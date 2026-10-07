namespace PolyConecta.Domain.Plataforma;

/// <summary>Bitácora de transiciones de estado (CT-32). Solo se inserta.</summary>
public sealed class StateTransitionLog
{
    public long Id { get; private set; }

    public string EntityType { get; private set; } = string.Empty;

    public long EntityId { get; private set; }

    public string FromState { get; private set; } = string.Empty;

    public string ToState { get; private set; } = string.Empty;

    public string UserName { get; private set; } = string.Empty;

    /// <summary>Rol ejercido. Se llena desde F1, con identidad.</summary>
    public string? Role { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string? Note { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    private StateTransitionLog() { }

    public StateTransitionLog(
        string entityType, long entityId, string fromState, string toState,
        string userName, string? role, DateTimeOffset occurredAt, string? note, string correlationId)
    {
        EntityType = entityType;
        EntityId = entityId;
        FromState = fromState;
        ToState = toState;
        UserName = userName;
        Role = role;
        OccurredAt = occurredAt;
        Note = note;
        CorrelationId = correlationId;
    }
}
