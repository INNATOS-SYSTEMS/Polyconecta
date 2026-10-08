using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Seguridad;
using PolyConecta.Domain.Common;

namespace PolyConecta.Infrastructure.Persistence.Almacenes;

/// <summary>
/// <see cref="IAlmacen{T}"/> sobre el PolyDbContext del ámbito. Aplica las reglas de fila del usuario
/// antes de cualquier filtro (02 §7). Las colecciones propias del agregado (OwnsMany) se cargan solas;
/// las navegaciones a otros agregados se cargan con <see cref="IIncluirEnAlmacen{T}"/>.
/// </summary>
public sealed class AlmacenEf<T>(PolyDbContext db, ReglasDeFila<T> reglas, IEnumerable<IIncluirEnAlmacen<T>> inclusiones)
    : IAlmacen<T> where T : AuditableEntity
{
    private async Task<IQueryable<T>> ConsultaAsync(bool incluirArchivados, CancellationToken ct)
    {
        IQueryable<T> q = db.Set<T>();
        if (incluirArchivados) q = q.IgnoreQueryFilters();
        foreach (var inclusion in inclusiones) q = inclusion.Incluir(q);
        var regla = await reglas.FiltroAsync(ct);
        return regla is null ? q : q.Where(regla);
    }

    public async Task<T?> PorIdAsync(long id, bool incluirArchivados = false, CancellationToken cancellationToken = default) =>
        await (await ConsultaAsync(incluirArchivados, cancellationToken)).SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<T>> ListarAsync(
        Expression<Func<T, bool>>? filtro = null, bool incluirArchivados = false, CancellationToken cancellationToken = default)
    {
        var q = await ConsultaAsync(incluirArchivados, cancellationToken);
        if (filtro is not null) q = q.Where(filtro);
        return await q.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExisteAsync(Expression<Func<T, bool>> filtro, bool incluirArchivados = false, CancellationToken cancellationToken = default) =>
        await (await ConsultaAsync(incluirArchivados, cancellationToken)).AnyAsync(filtro, cancellationToken);

    public async Task<int> ContarAsync(Expression<Func<T, bool>> filtro, bool incluirArchivados = false, CancellationToken cancellationToken = default) =>
        await (await ConsultaAsync(incluirArchivados, cancellationToken)).CountAsync(filtro, cancellationToken);

    public void Agregar(T entidad) => db.Set<T>().Add(entidad);

    public void ExigirVersion(T entidad, byte[] rowVersion)
    {
        var entry = db.Entry(entidad);
        entry.Property(e => e.RowVersion).OriginalValue = rowVersion;
        // El maestro siempre se actualiza: así la versión se compara aunque solo cambien líneas o firmas.
        if (entry.State == EntityState.Unchanged) entry.State = EntityState.Modified;
    }
}

/// <summary>Navegaciones a otros agregados que un almacén carga con la entidad (por ejemplo, el cliente del pedido).</summary>
public interface IIncluirEnAlmacen<T> where T : class
{
    IQueryable<T> Incluir(IQueryable<T> consulta);
}
