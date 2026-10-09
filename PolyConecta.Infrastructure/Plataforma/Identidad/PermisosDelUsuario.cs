using Microsoft.EntityFrameworkCore;
using PolyConecta.Application.Common;
using PolyConecta.Infrastructure.Persistence;

namespace PolyConecta.Infrastructure.Plataforma.Identidad;

/// <summary>
/// Asignaciones activas del usuario de la sesión con los permisos de sus grupos activos. Se consulta una
/// vez por petición: así, quitar un permiso a un grupo aplica a sus miembros en la siguiente acción, sin
/// tocar código (US2, escenario 6).
/// </summary>
public sealed class PermisosDelUsuario(PolyDbContext db, ICurrentUser usuario) : IPermisosDelUsuario
{
    private IReadOnlyList<AsignacionEfectiva>? _cache;

    public async Task<IReadOnlyList<AsignacionEfectiva>> AsignacionesAsync(CancellationToken cancellationToken = default)
    {
        if (_cache is not null) return _cache;
        if (usuario.UserId is not { } id) return _cache = [];

        var user = await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return _cache = [];

        var gruposIds = user.Assignments.Select(a => a.GroupId).Distinct().ToList();
        var grupos = await db.Groups.AsNoTracking().Where(g => gruposIds.Contains(g.Id)).ToListAsync(cancellationToken);
        var permisosIds = grupos.SelectMany(g => g.Permissions.Select(p => p.PermissionId)).Distinct().ToList();
        var claves = await db.Permissions.AsNoTracking().Where(p => permisosIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Key, cancellationToken);
        var plantas = await db.Plants.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Code, cancellationToken);

        return _cache = user.Assignments
            .Select(a => (a, g: grupos.FirstOrDefault(g => g.Id == a.GroupId)))
            .Where(x => x.g is not null && plantas.ContainsKey(x.a.PlantId))
            .Select(x => new AsignacionEfectiva(
                x.g!.Id, x.g.Code, x.g.Name, x.a.PlantId, plantas[x.a.PlantId], x.a.IsSubstitute,
                x.g.Permissions.Where(p => claves.ContainsKey(p.PermissionId)).Select(p => claves[p.PermissionId]).ToHashSet()))
            .ToList();
    }
}
