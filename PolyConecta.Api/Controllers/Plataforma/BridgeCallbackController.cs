using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Erp;
using PolyConecta.Infrastructure.Erp;

namespace PolyConecta.Api.Controllers.Plataforma;

/// <summary>
/// Recibe los callbacks del bridge (contrato §3; contrato del endpoint en
/// .specify/features/002-construccion-tecnica/contracts/callback-api.md). Solo traduce HTTP al
/// caso de uso ConfirmarSincronizacion (CT-08).
/// </summary>
[ApiController]
[Route("api/v1/plataforma/bridge/callbacks")]
public sealed class BridgeCallbackController(
    IUseCase<ResultadoBridge, EfectoCallback> confirmar, IOptions<ErpOptions> opciones, IClock clock) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Recibir(CancellationToken cancellationToken)
    {
        // Se firma el cuerpo exacto que llegó: se lee crudo antes de deserializar.
        using var lector = new StreamReader(Request.Body, Encoding.UTF8);
        var cuerpo = await lector.ReadToEndAsync(cancellationToken);
        if (!BridgeSignature.Verificar(Request.Headers[BridgeSignature.Cabecera], cuerpo, opciones.Value.CallbackSecret, clock.Now))
            return Unauthorized();

        ResultadoBridge resultado;
        try
        {
            using var json = JsonDocument.Parse(cuerpo);
            resultado = ConfirmarSincronizacion.Leer(json.RootElement);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(new { error = "El cuerpo no cumple el esquema del callback." });
        }

        return await confirmar.ExecuteAsync(resultado, cancellationToken) == EfectoCallback.NoEncontrado
            ? NotFound()
            : Ok();
    }
}

/// <summary>Reintento manual de un comando en Error (CT-20). Lo usará el tablero de sincronización (2.7).</summary>
[ApiController]
[Route("api/v1/plataforma/outbox")]
public sealed class OutboxController(IUseCase<ReintentarSincronizacionRequest, Unit> reintentar) : ControllerBase
{
    [HttpPost("{id:guid}/reintentar")]
    public async Task<IActionResult> Reintentar(Guid id, CancellationToken cancellationToken)
    {
        await reintentar.ExecuteAsync(new ReintentarSincronizacionRequest(id), cancellationToken);
        return NoContent();
    }
}
