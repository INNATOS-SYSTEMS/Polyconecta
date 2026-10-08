using System.Linq.Expressions;
using PolyConecta.Domain.Common;

namespace PolyConecta.Application.Common;

/// <summary>
/// Acceso a un agregado desde los casos de uso, sin EF Core en Application (CT-07). Lo implementa
/// Infrastructure. Toda lectura aplica las reglas de fila del usuario (R-02) y oculta lo archivado salvo
/// que se pida; las colecciones del agregado vienen cargadas.
/// </summary>
public interface IAlmacen<T> where T : AuditableEntity
{
    Task<T?> PorIdAsync(long id, bool incluirArchivados = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListarAsync(
        Expression<Func<T, bool>>? filtro = null, bool incluirArchivados = false, CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(Expression<Func<T, bool>> filtro, bool incluirArchivados = false, CancellationToken cancellationToken = default);

    Task<int> ContarAsync(Expression<Func<T, bool>> filtro, bool incluirArchivados = false, CancellationToken cancellationToken = default);

    void Agregar(T entidad);

    /// <summary>
    /// Concurrencia optimista (R-09): el guardado falla con 409 DOCUMENTO_MODIFICADO si la versión que
    /// tenía el usuario ya no es la de la base, aunque solo cambien las colecciones del agregado.
    /// </summary>
    void ExigirVersion(T entidad, byte[] rowVersion);
}

/// <summary>Busca un agregado o lanza 404 (D-154: un id inexistente o fuera de las reglas de fila no se ve).</summary>
public static class AlmacenExtensiones
{
    public static async Task<T> ObtenerAsync<T>(this IAlmacen<T> almacen, long id, string nombre, CancellationToken cancellationToken = default)
        where T : AuditableEntity =>
        await almacen.PorIdAsync(id, incluirArchivados: true, cancellationToken)
        ?? throw new KeyNotFoundException($"No existe {nombre} {id}.");
}
