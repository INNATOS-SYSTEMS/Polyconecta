using System.Text.Json;
using System.Text.Json.Nodes;
using Contpaq.Bridge.Core.Models;

namespace Contpaq.Bridge.Core.Contract
{
    /// <summary>
    /// Cuerpo del callback (§3). Es el mismo que devuelve GET /api/v1/transactions/{id}, para que
    /// PolyConecta recupere el estado si el callback no llega.
    /// </summary>
    public static class CuerpoCallback
    {
        public static JsonObject Desde(BridgeTransaction tx) => new()
        {
            ["event_type"] = "transaction.status_changed",
            ["contract_version"] = string.IsNullOrEmpty(tx.ContractVersion) ? Contrato.VersionActual : tx.ContractVersion,
            ["transaction_id"] = tx.TransactionId,
            ["correlation_id"] = tx.CorrelationId,
            ["idempotency_key"] = tx.IdempotencyKey,
            ["command_type"] = tx.CommandType,
            ["status"] = tx.Status,
            ["result"] = tx.Status == Estados.Confirmed && tx.ResultJson is not null ? JsonNode.Parse(tx.ResultJson) : null,
            ["error"] = Estados.EsTerminal(tx.Status) && tx.Status != Estados.Confirmed && tx.ErrorJson is not null
                ? JsonNode.Parse(tx.ErrorJson)
                : null,
            ["timestamp"] = tx.UpdatedAt,
        };

        public static string Serializar(BridgeTransaction tx) => Desde(tx).ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }
}
