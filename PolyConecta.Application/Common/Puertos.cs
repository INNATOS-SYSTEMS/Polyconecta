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

/// <summary>
/// Usuario de la sesión y el grupo con el que actúa (el "rol ejercido" de CT-32). Fuera de una petición
/// (despachador, sincronización periódica) es "sistema". El grupo ejercido lo fija el decorador de
/// autorización antes de correr el caso de uso (R-02); la auditoría, la bitácora y el chatter lo guardan.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Con lo que entra la persona (`cvillarreal`), o "sistema".</summary>
    string UserName { get; }

    /// <summary>Id del usuario de dominio; null para "sistema".</summary>
    long? UserId { get; }

    /// <summary>Lo que muestran la barra superior, el chatter y las firmas.</summary>
    string NombreVisible { get; }

    /// <summary>Nombre del grupo con el que actúa en la acción en curso (CT-32).</summary>
    string? GrupoEjercido { get; }

    /// <summary>Si actúa como suplente del grupo ejercido (D-38).</summary>
    bool EsSuplente { get; }

    /// <summary>Rol ejercido, como lo guarda StateTransitionLog. Es el grupo ejercido.</summary>
    string? Role => GrupoEjercido;

    /// <summary>Lo llama el decorador de autorización (o el caso de uso que elige entre varios grupos).</summary>
    void EjercerGrupo(string? grupo, bool esSuplente);
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
