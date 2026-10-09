using PolyConecta.Domain.Common;

namespace PolyConecta.Domain.Plataforma.Seguridad;

/// <summary>Un permiso asignado a un grupo (<c>plt.group_permission</c>).</summary>
public sealed class GroupPermission
{
    public long PermissionId { get; private set; }

    private GroupPermission() { }

    public GroupPermission(long permissionId) => PermissionId = permissionId;
}

/// <summary>
/// Grupo del catálogo (D-148): un conjunto de permisos que el Administrador modifica. Un área con
/// niveles tiene un grupo por nivel. Sustituye a <c>Role</c> de 04 §3.
/// </summary>
public sealed class Group : ArchivableEntity
{
    private readonly List<GroupPermission> _permisos = [];

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public IReadOnlyList<GroupPermission> Permissions => _permisos;

    private Group() { }

    public Group(string code, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ReglaDeNegocioException("GRUPO_SIN_CODIGO", "El grupo necesita un código.");
        Code = code.Trim().ToUpperInvariant().Replace(' ', '_');
        if (Code.Length > 40) throw new ReglaDeNegocioException("GRUPO_CODIGO_LARGO", "El código del grupo tiene más de 40 caracteres.");
        Editar(name, description);
    }

    /// <summary>Grupo nuevo con los mismos permisos que <paramref name="origen"/> (US2, escenario 7).</summary>
    public static Group CopiarDe(Group origen, string code, string name, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(origen);
        var grupo = new Group(code, name, description ?? origen.Description);
        grupo.AsignarPermisos(origen.Permissions.Select(p => p.PermissionId));
        return grupo;
    }

    public void Editar(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ReglaDeNegocioException("GRUPO_SIN_NOMBRE", "El grupo necesita un nombre.");
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>Deja exactamente esos permisos: los dos paneles mandan la lista final (contracts/api-f1.md).</summary>
    public void AsignarPermisos(IEnumerable<long> permisoIds)
    {
        var finales = permisoIds.Distinct().ToHashSet();
        _permisos.RemoveAll(p => !finales.Contains(p.PermissionId));
        foreach (var id in finales.Where(id => _permisos.All(p => p.PermissionId != id)))
            _permisos.Add(new GroupPermission(id));
    }

    public bool Tiene(long permisoId) => _permisos.Any(p => p.PermissionId == permisoId);

    /// <summary>Un grupo con miembros activos no se archiva: primero se quitan (data-model §1).</summary>
    public void Archivar(int miembrosActivos)
    {
        if (miembrosActivos > 0)
            throw new ReglaDeNegocioException("GRUPO_CON_MIEMBROS",
                $"El grupo {Name} tiene {miembrosActivos} miembro(s) activo(s); quítalos antes de archivarlo.");
        Archive();
    }
}
