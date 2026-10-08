using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PolyConecta.Application.Common;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Plataforma;

namespace PolyConecta.Infrastructure.Persistence;

/// <summary>
/// Llena la auditoría de los mixins (04 §1) y convierte las transiciones pendientes de cada
/// documento en filas de StateTransitionLog (CT-32). Las filas de la bitácora se insertan después
/// del guardado principal, cuando ya se conoce el id de los documentos nuevos; dentro de un caso
/// de uso las dos escrituras van en la misma transacción.
/// </summary>
public sealed class AuditoriaInterceptor(IClock clock, ICurrentUser user, ICorrelationContext correlation) : SaveChangesInterceptor
{
    private readonly List<AuditableEntity> _conTransiciones = [];
    private bool _guardandoBitacora;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Preparar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Preparar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } db && EscribirBitacora(db))
        {
            try { await db.SaveChangesAsync(cancellationToken); }
            finally { _guardandoBitacora = false; }
        }
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is { } db && EscribirBitacora(db))
        {
            try { db.SaveChanges(); }
            finally { _guardandoBitacora = false; }
        }
        return base.SavedChanges(eventData, result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _conTransiciones.Clear();
        base.SaveChangesFailed(eventData);
    }

    private void Preparar(DbContext? db)
    {
        if (db is null || _guardandoBitacora) return;
        var ahora = clock.Now;
        foreach (var entry in db.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.MarcarCreado(ahora, user.UserName);
            else if (entry.State == EntityState.Modified)
                entry.Entity.MarcarModificado(ahora, user.UserName);

            if (entry.Entity.TransicionesPendientes.Count > 0 && !_conTransiciones.Contains(entry.Entity))
                _conTransiciones.Add(entry.Entity);
        }
    }

    private bool EscribirBitacora(DbContext db)
    {
        if (_guardandoBitacora || _conTransiciones.Count == 0) return false;
        var ahora = clock.Now;
        foreach (var entidad in _conTransiciones)
        {
            foreach (var t in entidad.TransicionesPendientes)
            {
                db.Set<StateTransitionLog>().Add(new StateTransitionLog(
                    entidad.GetType().Name, entidad.Id, t.Desde, t.Hacia, user.UserName, user.GrupoEjercido, ahora, t.Nota,
                    correlation.CorrelationId));
            }
            entidad.LimpiarTransicionesPendientes();
        }
        _conTransiciones.Clear();
        _guardandoBitacora = true;
        return true;
    }
}
