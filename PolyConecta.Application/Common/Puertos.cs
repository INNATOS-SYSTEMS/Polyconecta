namespace PolyConecta.Application.Common;

/// <summary>Transacción del caso de uso. La implementa Infrastructure sobre el DbContext.</summary>
public interface IUnitOfWork
{
    bool HasActiveTransaction { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Usuario y rol que ejecutan el caso de uso. Hasta F1 (1.2) es "sistema".</summary>
public interface ICurrentUser
{
    string UserName { get; }

    string? Role { get; }
}

/// <summary>Reloj inyectable, para que las pruebas controlen la fecha.</summary>
public interface IClock
{
    DateTimeOffset Now { get; }
}

/// <summary>correlation_id del flujo actual (CT-31).</summary>
public interface ICorrelationContext
{
    string CorrelationId { get; }
}
