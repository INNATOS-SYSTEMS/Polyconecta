using Microsoft.EntityFrameworkCore.Storage;
using PolyConecta.Application.Common;
using PolyConecta.Infrastructure.Persistence;

namespace PolyConecta.Infrastructure.Common;

/// <summary>Transacción sobre el PolyDbContext del ámbito actual.</summary>
public sealed class EfUnitOfWork(PolyDbContext db) : IUnitOfWork
{
    private IDbContextTransaction? _transaccion;

    public bool HasActiveTransaction => _transaccion is not null;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        _transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaccion is null) return;
        await _transaccion.CommitAsync(cancellationToken);
        await _transaccion.DisposeAsync();
        _transaccion = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaccion is null) return;
        await _transaccion.RollbackAsync(cancellationToken);
        await _transaccion.DisposeAsync();
        _transaccion = null;
        db.ChangeTracker.Clear();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}

/// <summary>
/// Usuario "sistema": lo que corre fuera de una petición (despachador, sincronización periódica,
/// sembradores). Es por ámbito porque el grupo ejercido cambia por caso de uso.
/// </summary>
public class SistemaCurrentUser : ICurrentUser
{
    public const string Nombre = "sistema";

    public virtual string UserName => Nombre;

    public virtual long? UserId => null;

    public virtual string NombreVisible => "Sistema";

    public string? GrupoEjercido { get; private set; }

    public bool EsSuplente { get; private set; }

    public void EjercerGrupo(string? grupo, bool esSuplente)
    {
        GrupoEjercido = grupo;
        EsSuplente = esSuplente;
    }
}

/// <summary>correlation_id del ámbito; lo fija el middleware de la API o el despachador (CT-31).</summary>
public sealed class CorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
}
