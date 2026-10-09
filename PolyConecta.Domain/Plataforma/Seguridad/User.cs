using PolyConecta.Domain.Common;

namespace PolyConecta.Domain.Plataforma.Seguridad;

/// <summary>Asignación pedida para un usuario: grupo × planta, titular o suplente (D-38, D-148).</summary>
public sealed record AsignacionSolicitada(long GrupoId, long PlantaId, bool Suplente);

/// <summary>Usuario × grupo × planta, con la marca de suplente (<c>plt.group_assignment</c>).</summary>
public sealed class GroupAssignment
{
    public long GroupId { get; private set; }

    public long PlantId { get; private set; }

    /// <summary>Solo para leer los nombres en la lista de usuarios.</summary>
    public Group? Group { get; private set; }

    public Plant? Plant { get; private set; }

    /// <summary>Suplente designado del grupo en esa planta (D-38).</summary>
    public bool IsSubstitute { get; internal set; }

    private GroupAssignment() { }

    public GroupAssignment(long groupId, long plantId, bool isSubstitute)
    {
        GroupId = groupId;
        PlantId = plantId;
        IsSubstitute = isSubstitute;
    }
}

/// <summary>
/// Persona que entra a PolyConecta (D-32). Las credenciales viven aparte (Identity, R-01); el dominio
/// solo conoce quién es y qué grupos tiene por planta. Archivar impide entrar y no borra nada: sus
/// firmas y transiciones siguen atribuidas a él (US2, escenario 5).
/// </summary>
public sealed class User : ArchivableEntity
{
    private readonly List<GroupAssignment> _asignaciones = [];

    public string UserName { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    /// <summary>Su agente de CONTPAQi; lo liga el Administrador (D-153).</summary>
    public long? ErpAgentId { get; private set; }

    public IReadOnlyList<GroupAssignment> Assignments => _asignaciones;

    private User() { }

    public User(string userName, string displayName, string? email, IEnumerable<AsignacionSolicitada> asignaciones)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ReglaDeNegocioException("USUARIO_SIN_NOMBRE", "Falta el usuario con el que se entra.");
        var limpio = userName.Trim();
        if (limpio.Any(char.IsWhiteSpace))
            throw new ReglaDeNegocioException("USUARIO_CON_ESPACIOS", "El usuario no lleva espacios.");
        if (limpio.Length > 64)
            throw new ReglaDeNegocioException("USUARIO_LARGO", "El usuario tiene más de 64 caracteres.");
        UserName = limpio;
        Editar(displayName, email);
        AsignarGrupos(asignaciones);
    }

    public void Editar(string displayName, string? email)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ReglaDeNegocioException("USUARIO_SIN_NOMBRE_VISIBLE", "Falta el nombre de la persona.");
        DisplayName = displayName.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }

    /// <summary>Deja exactamente esas asignaciones. Un usuario activo tiene al menos una (FR-010).</summary>
    public void AsignarGrupos(IEnumerable<AsignacionSolicitada> asignaciones)
    {
        var pedidas = asignaciones
            .GroupBy(a => (a.GrupoId, a.PlantaId))
            .Select(g => g.Last())
            .ToList();
        if (IsActive && pedidas.Count == 0)
            throw new ReglaDeNegocioException("USUARIO_SIN_GRUPO", "Un usuario activo tiene al menos un grupo.");

        _asignaciones.RemoveAll(a => !pedidas.Any(p => p.GrupoId == a.GroupId && p.PlantaId == a.PlantId));
        foreach (var p in pedidas)
        {
            var actual = _asignaciones.FirstOrDefault(a => a.GroupId == p.GrupoId && a.PlantId == p.PlantaId);
            if (actual is null) _asignaciones.Add(new GroupAssignment(p.GrupoId, p.PlantaId, p.Suplente));
            else actual.IsSubstitute = p.Suplente;
        }
    }

    public void LigarAgente(long? erpAgentId) => ErpAgentId = erpAgentId;

    public void Archivar() => Archive();

    /// <summary>Restaurar exige al menos un grupo, como cualquier usuario activo.</summary>
    public void Restaurar()
    {
        if (_asignaciones.Count == 0)
            throw new ReglaDeNegocioException("USUARIO_SIN_GRUPO", "Un usuario activo tiene al menos un grupo.");
        Restore();
    }
}
