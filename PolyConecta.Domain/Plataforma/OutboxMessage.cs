namespace PolyConecta.Domain.Plataforma;

public enum OutboxStatus
{
    Pendiente,
    Enviado,
    Confirmado,
    Error,
    Bloqueado,
}

/// <summary>
/// Comando para CONTPAQi en el outbox de PolyConecta. Se escribe en la misma transacción que el
/// cambio de negocio (CT-20) y lo envía el despachador en orden de <see cref="Sequence"/> (CT-41).
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }

    /// <summary>Orden de envío. Lo asigna la base (IDENTITY).</summary>
    public long Sequence { get; private set; }

    public string CommandType { get; private set; } = string.Empty;

    public string? Variant { get; private set; }

    /// <summary>La carga del contrato, ya traducida, en JSON.</summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>{tipo}:{id}:{transición} (CT-19).</summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    public string CorrelationId { get; private set; } = string.Empty;

    public string DocumentType { get; private set; } = string.Empty;

    public long DocumentId { get; private set; }

    /// <summary>Llaves de bloqueo (`producto:…`, `almacen:…`) para D-95.</summary>
    public IReadOnlyList<string> LockKeys { get; private set; } = [];

    public OutboxStatus Status { get; private set; } = OutboxStatus.Pendiente;

    public int Attempts { get; private set; }

    public DateTimeOffset? NextAttemptAt { get; private set; }

    public string? BridgeTransactionId { get; private set; }

    public string? LastErrorCode { get; private set; }

    public string? LastErrorMessage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(
        string commandType, string? variant, string payload, string idempotencyKey, string correlationId,
        string documentType, long documentId, IEnumerable<string> lockKeys, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        CommandType = commandType;
        Variant = variant;
        Payload = payload;
        IdempotencyKey = idempotencyKey;
        CorrelationId = correlationId;
        DocumentType = documentType;
        DocumentId = documentId;
        LockKeys = lockKeys.Distinct(StringComparer.Ordinal).ToList();
        CreatedAt = createdAt;
    }

    public bool ComparteLlaves(OutboxMessage otro) => LockKeys.Intersect(otro.LockKeys, StringComparer.Ordinal).Any();

    public void MarcarEnviado(string bridgeTransactionId, DateTimeOffset cuando)
    {
        Status = OutboxStatus.Enviado;
        BridgeTransactionId = bridgeTransactionId;
        SentAt = cuando;
        NextAttemptAt = null;
    }

    /// <summary>Falla de red o 5xx: se reintenta con espera de 2ⁿ segundos hasta el máximo.</summary>
    /// <returns>True si se agotaron los reintentos y el mensaje quedó en Error.</returns>
    public bool RegistrarFallaDeEnvio(string mensaje, DateTimeOffset ahora, int maxIntentos)
    {
        Attempts++;
        LastErrorCode = "ENVIO_FALLIDO";
        LastErrorMessage = mensaje;
        if (Attempts >= maxIntentos)
        {
            Status = OutboxStatus.Error;
            NextAttemptAt = null;
            return true;
        }
        NextAttemptAt = ahora.AddSeconds(Math.Pow(2, Attempts));
        return false;
    }

    public void Confirmar(DateTimeOffset cuando)
    {
        Status = OutboxStatus.Confirmado;
        CompletedAt = cuando;
        LastErrorCode = null;
        LastErrorMessage = null;
    }

    public void MarcarError(string codigo, string mensaje, DateTimeOffset cuando)
    {
        Status = OutboxStatus.Error;
        LastErrorCode = codigo;
        LastErrorMessage = mensaje;
        CompletedAt = cuando;
    }

    public void Bloquear() => Status = OutboxStatus.Bloqueado;

    public void Desbloquear()
    {
        if (Status == OutboxStatus.Bloqueado) Status = OutboxStatus.Pendiente;
    }

    /// <summary>Reintento manual de un Error (CT-20): misma idempotency_key, contadores en cero.</summary>
    public void Reintentar()
    {
        Status = OutboxStatus.Pendiente;
        Attempts = 0;
        NextAttemptAt = null;
        BridgeTransactionId = null;
        CompletedAt = null;
    }
}
