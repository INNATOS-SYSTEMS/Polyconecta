using PolyConecta.Domain.Plataforma.Seguridad;

namespace PolyConecta.Application.Common;

/// <summary>La petición de un caso de uso exige un permiso del catálogo (R-02, CT-11).</summary>
public interface IRequierePermiso
{
    string Permiso { get; }
}

/// <summary>La petición exige cualquiera de varios permisos (por ejemplo, firmar como Comercial o como Cobranza).</summary>
public interface IRequiereAlgunPermiso
{
    IReadOnlyList<string> Permisos { get; }
}

/// <summary>El documento de la petición tiene planta: solo cuentan las asignaciones de esa planta (R-02).</summary>
public interface IConPlanta
{
    long? PlantaId { get; }
}

/// <summary>Una asignación activa del usuario con los permisos de su grupo.</summary>
public sealed record AsignacionEfectiva(
    long GrupoId, string GrupoCodigo, string GrupoNombre, long PlantaId, string PlantaCodigo, bool EsSuplente,
    IReadOnlySet<string> Permisos);

/// <summary>El grupo con el que actúa el usuario para un permiso (el "rol ejercido" de CT-32).</summary>
public sealed record GrupoEjercido(long GrupoId, string Codigo, string Nombre, bool EsSuplente);

/// <summary>Asignaciones activas del usuario de la sesión, con los permisos de cada grupo. Lo implementa Infrastructure.</summary>
public interface IPermisosDelUsuario
{
    Task<IReadOnlyList<AsignacionEfectiva>> AsignacionesAsync(CancellationToken cancellationToken = default);
}

/// <summary>El usuario no tiene el permiso. La API la traduce a 403 con <see cref="Razon"/> (CT-26).</summary>
public sealed class PermisoDenegadoException(string permiso, string razon) : Exception(razon)
{
    public string Permiso { get; } = permiso;

    public string Razon { get; } = razon;

    public static PermisoDenegadoException Para(string permiso)
    {
        var d = Domain.Plataforma.Seguridad.Permisos.Buscar(permiso);
        var nombre = d is null ? permiso : $"{d.EtiquetaModulo} › {d.EtiquetaObjeto} › {d.EtiquetaAccion}";
        return new PermisoDenegadoException(permiso, $"Tu grupo no tiene el permiso {nombre}.");
    }
}

/// <summary>
/// Decide si el usuario puede y con qué grupo (R-02): la primera asignación activa con el permiso, las de
/// titular antes que las de suplente; si el documento tiene planta, solo las de esa planta.
/// </summary>
public sealed class Autorizacion(IPermisosDelUsuario permisos)
{
    public async Task<GrupoEjercido?> ResolverAsync(string permiso, long? plantaId = null, CancellationToken cancellationToken = default)
    {
        var asignaciones = await permisos.AsignacionesAsync(cancellationToken);
        var a = asignaciones
            .Where(x => x.Permisos.Contains(permiso) && (plantaId is null || x.PlantaId == plantaId))
            .OrderBy(x => x.EsSuplente)
            .ThenBy(x => x.GrupoId)
            .FirstOrDefault();
        return a is null ? null : new GrupoEjercido(a.GrupoId, a.GrupoCodigo, a.GrupoNombre, a.EsSuplente);
    }

    public async Task<bool> PuedeAsync(string permiso, long? plantaId = null, CancellationToken cancellationToken = default) =>
        await ResolverAsync(permiso, plantaId, cancellationToken) is not null;

    /// <summary>Permisos efectivos del usuario, para la sesión y los menús de la web.</summary>
    public async Task<IReadOnlySet<string>> PermisosAsync(CancellationToken cancellationToken = default) =>
        (await permisos.AsignacionesAsync(cancellationToken)).SelectMany(a => a.Permisos).ToHashSet();
}

/// <summary>
/// Verifica el permiso de la petición antes de validarla y de abrir la transacción (R-02), y deja el
/// grupo ejercido en <see cref="ICurrentUser"/> para la bitácora y el chatter. Lo que corre como
/// "sistema" (sin usuario: despachador, sincronización periódica) no pasa por permisos.
/// </summary>
public sealed class AuthorizationDecorator<TRequest, TResult>(
    IUseCase<TRequest, TResult> inner,
    Autorizacion autorizacion,
    ICurrentUser usuario) : IUseCase<TRequest, TResult>
{
    public async Task<TResult> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> exigidos = request switch
        {
            IRequierePermiso p => [p.Permiso],
            IRequiereAlgunPermiso a => a.Permisos,
            _ => [],
        };
        if (exigidos.Count > 0 && usuario.UserId is not null)
        {
            var planta = (request as IConPlanta)?.PlantaId;
            GrupoEjercido? grupo = null;
            foreach (var permiso in exigidos)
            {
                grupo = await autorizacion.ResolverAsync(permiso, planta, cancellationToken);
                if (grupo is not null) break;
            }
            if (grupo is null) throw PermisoDenegadoException.Para(exigidos[0]);
            usuario.EjercerGrupo(grupo.Nombre, grupo.EsSuplente);
        }
        return await inner.ExecuteAsync(request, cancellationToken);
    }
}
