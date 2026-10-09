using System.Linq.Expressions;
using PolyConecta.Application.Common;

namespace PolyConecta.Application.Plataforma.Seguridad;

/// <summary>Lo que una regla de fila sabe del usuario: quién es y sus asignaciones por planta.</summary>
public sealed record ContextoDeReglas(long? UserId, IReadOnlyList<AsignacionEfectiva> Asignaciones)
{
    /// <summary>Códigos de planta del usuario, opcionalmente solo de los grupos que tienen un permiso.</summary>
    public IReadOnlyList<string> Plantas(string? conPermiso = null) =>
        Asignaciones.Where(a => conPermiso is null || a.Permisos.Contains(conPermiso)).Select(a => a.PlantaCodigo).Distinct().ToList();
}

/// <summary>
/// Regla de fila (capa 2 de 01 §3, R-02): sobre qué registros de <typeparamref name="T"/> puede actuar
/// el usuario. Se aplica antes de cualquier filtro, así que ningún filtro la amplía (02 §7). Es código
/// registrado por tipo; pasa a tabla si un día se configura desde la interfaz (data-model §1).
/// </summary>
public interface IReglaDeFila<T>
{
    Expression<Func<T, bool>> Filtro(ContextoDeReglas contexto);
}

/// <summary>Todas las reglas de <typeparamref name="T"/> unidas con Y, para el usuario de la sesión.</summary>
public sealed class ReglasDeFila<T>(IEnumerable<IReglaDeFila<T>> reglas, IPermisosDelUsuario permisos, ICurrentUser usuario)
{
    /// <summary>Null si no hay reglas o si corre como "sistema" (sincronización, despachador).</summary>
    public async Task<Expression<Func<T, bool>>?> FiltroAsync(CancellationToken cancellationToken = default)
    {
        var lista = reglas.ToList();
        if (lista.Count == 0 || usuario.UserId is null) return null;
        var contexto = new ContextoDeReglas(usuario.UserId, await permisos.AsignacionesAsync(cancellationToken));
        return Expresiones.Todos(lista.Select(r => r.Filtro(contexto)));
    }
}
