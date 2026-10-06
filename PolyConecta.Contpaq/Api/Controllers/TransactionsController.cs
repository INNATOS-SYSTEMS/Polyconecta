using System;
using System.Text.Json;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Core.Models;
using Contpaq.Bridge.Core.Services;
using Contpaq.Bridge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Contpaq.Bridge.Api.Controllers
{
    /// <summary>Comandos del contrato bridge-v1 (§2 a §5).</summary>
    [ApiController]
    [Route("api/v1/transactions")]
    public class TransactionsController(IOutboxRepository outbox) : ControllerBase
    {
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> CrearTransaccion()
        {
            ComandoRequest? request;
            try
            {
                request = await JsonSerializer.DeserializeAsync<ComandoRequest>(Request.Body);
            }
            catch (JsonException ex)
            {
                return BadRequest(ErrorContrato.De(CodigosError.CargaInvalida, $"JSON inválido: {ex.Message}"));
            }
            if (request is null)
                return BadRequest(ErrorContrato.De(CodigosError.CargaInvalida, "Falta el cuerpo."));

            // Un reenvío con la misma idempotency_key nunca duplica (CT-19): devuelve el original.
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey) &&
                await outbox.GetByIdempotencyKeyAsync(request.IdempotencyKey) is { } existente)
            {
                return Accepted(Acuse(existente, esDuplicado: true));
            }

            var (_, error) = LectorComandos.Leer(request);
            if (error is not null)
                return BadRequest(error);

            var ahora = DateTime.UtcNow.ToString("o");
            var tx = new BridgeTransaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                CorrelationId = request.CorrelationId!,
                ClientAppId = request.ClientAppId!,
                IdempotencyKey = request.IdempotencyKey,
                CommandType = request.CommandType!,
                ContractVersion = request.ContractVersion!,
                Variant = request.Variant,
                PayloadJson = request.Payload.GetRawText(),
                CallbackUrl = request.CallbackUrl,
                Status = Estados.Pending,
                NextAttemptAt = ahora,
                CreatedAt = ahora,
                UpdatedAt = ahora,
            };

            if (!await outbox.AddTransactionAsync(tx))
            {
                // Carrera entre dos reenvíos simultáneos: gana el primero.
                var original = await outbox.GetByIdempotencyKeyAsync(tx.IdempotencyKey!);
                return Accepted(Acuse(original!, esDuplicado: true));
            }

            PerformanceMetrics.RecordWriteOp();
            return Accepted(Acuse(tx, esDuplicado: false));
        }

        /// <summary>Lo mismo que el callback (§3), para cuando el callback no llega.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Consultar(string id)
        {
            var tx = await outbox.GetByIdAsync(id);
            return tx is null ? NotFound() : Content(CuerpoCallback.Serializar(tx), "application/json");
        }

        // Rutas de operación (§7): las usa el tablero del bridge, no PolyConecta.

        [HttpDelete("pending")]
        public async Task<IActionResult> DeletePendingTransactions()
        {
            var deletedCount = await outbox.DeletePendingTransactionsAsync();
            return Ok(new { message = $"Purged {deletedCount} pending transactions from Outbox", deleted_count = deletedCount });
        }

        [HttpDelete("purge-all")]
        public async Task<IActionResult> PurgeAllTransactions()
        {
            var deletedCount = await outbox.PurgeAllTransactionsAsync();
            return Ok(new { message = $"Purged all {deletedCount} transactions from Outbox", deleted_count = deletedCount });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(string id)
        {
            if (!await outbox.DeleteTransactionAsync(id)) return NotFound(new { error = $"Transaction {id} not found" });
            return Ok(new { message = $"Transaction {id} deleted successfully" });
        }

        private static object Acuse(BridgeTransaction tx, bool esDuplicado) => esDuplicado
            ? new { transaction_id = tx.TransactionId, correlation_id = tx.CorrelationId, status = tx.Status, created_at = tx.CreatedAt, is_duplicate = true }
            : new { transaction_id = tx.TransactionId, correlation_id = tx.CorrelationId, status = tx.Status, created_at = tx.CreatedAt };
    }
}
