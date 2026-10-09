using System;

namespace Contpaq.Bridge.Core.Models
{
    public class BridgeTransaction
    {
        public string TransactionId { get; set; } = Guid.NewGuid().ToString();
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public string ClientAppId { get; set; } = string.Empty;
        public string? IdempotencyKey { get; set; }
        public string CommandType { get; set; } = string.Empty;
        public string ContractVersion { get; set; } = string.Empty;
        public string? Variant { get; set; }
        public string PayloadJson { get; set; } = string.Empty;
        public string? CallbackUrl { get; set; }
        public string Status { get; set; } = Contpaq.Bridge.Core.Contract.Estados.Pending;
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; } = 5;
        public string NextAttemptAt { get; set; } = DateTime.UtcNow.ToString("o");
        public int? ContpaqiDocId { get; set; }
        public string? ContpaqiFolio { get; set; }
        public int? LastErrorCode { get; set; }
        public string? LastErrorMessage { get; set; }
        /// <summary>Resultado del contrato (§5) en JSON, cuando la transacción está CONFIRMED.</summary>
        public string? ResultJson { get; set; }
        /// <summary>Error del contrato (§4) en JSON, cuando falló o se está reintentando.</summary>
        public string? ErrorJson { get; set; }
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
        public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class TransactionLog
    {
        public string LogId { get; set; } = Guid.NewGuid().ToString();
        public string TransactionId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public int AttemptNumber { get; set; }
        public string SdkFunctionName { get; set; } = string.Empty;
        public int SdkErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public long DurationMs { get; set; }
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }
}
