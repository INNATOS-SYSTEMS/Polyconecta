using PolyConecta.Application.Common;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Plataforma.Seguridad;

namespace PolyConecta.Application.Plataforma.Seguridad;

/// <summary>Credenciales de un usuario (Identity, R-01). Lo implementa Infrastructure; un error de la política es 400.</summary>
public interface ICredenciales
{
    Task CrearAsync(long userId, string usuario, string contrasena, CancellationToken cancellationToken = default);

    Task RestablecerAsync(long userId, string contrasena, CancellationToken cancellationToken = default);
}

public sealed record AsignacionDto(long GrupoId, long PlantaId, bool Suplente);

public sealed record AsignacionDetalle(long GrupoId, string Grupo, string GrupoCodigo, long PlantaId, string Planta, bool Suplente);

public sealed record UsuarioDetalle(
    long Id, string Usuario, string Nombre, string? Email, bool Activo, long? AgenteId, byte[] RowVersion,
    IReadOnlyList<AsignacionDetalle> Asignaciones);

public sealed record ObtenerUsuario(long Id) : IRequierePermiso
{
    public string Permiso => Permisos.UsuariosLeer;
}

public sealed record CrearUsuario(string Usuario, string Nombre, string? Email, string Contrasena, IReadOnlyList<AsignacionDto> Asignaciones)
    : IRequierePermiso
{
    public string Permiso => Permisos.UsuariosAdministrar;
}

public sealed record EditarUsuario(long Id, byte[] RowVersion, string Nombre, string? Email, IReadOnlyList<AsignacionDto> Asignaciones)
    : IRequierePermiso
{
    public string Permiso => Permisos.UsuariosAdministrar;
}

public sealed record ArchivarUsuario(long Id, bool Archivar) : IRequierePermiso
{
    public string Permiso => Permisos.UsuariosAdministrar;
}

public sealed record RestablecerContrasena(long Id, string Contrasena) : IRequierePermiso
{
    public string Permiso => Permisos.UsuariosAdministrar;
}

public sealed class ValidarCrearUsuario : IValidator<CrearUsuario>
{
    public IEnumerable<ErrorValidacion> Validate(CrearUsuario request)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario)) yield return new("usuario", "Falta el usuario.");
        if (string.IsNullOrWhiteSpace(request.Nombre)) yield return new("nombre", "Falta el nombre.");
        if (string.IsNullOrWhiteSpace(request.Contrasena)) yield return new("contrasena", "Falta la contraseña.");
        if (request.Asignaciones is not { Count: > 0 }) yield return new("asignaciones", "Un usuario activo tiene al menos un grupo.");
    }
}

public sealed class ValidarEditarUsuario : IValidator<EditarUsuario>
{
    public IEnumerable<ErrorValidacion> Validate(EditarUsuario request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre)) yield return new("nombre", "Falta el nombre.");
        if (request.RowVersion is not { Length: > 0 }) yield return new("rowVersion", "Falta la versión del usuario.");
    }
}

/// <summary>Lo común de los casos de uso de usuarios: arma el detalle y verifica grupos y plantas.</summary>
public sealed class CatalogoDeSeguridad(IAlmacen<Group> grupos, IAlmacen<Plant> plantas)
{
    public async Task<UsuarioDetalle> DetalleAsync(User u, CancellationToken ct)
    {
        var ids = u.Assignments.Select(a => a.GroupId).ToList();
        var gs = await grupos.ListarAsync(g => ids.Contains(g.Id), incluirArchivados: true, ct);
        var ps = await plantas.ListarAsync(cancellationToken: ct);
        return new UsuarioDetalle(u.Id, u.UserName, u.DisplayName, u.Email, u.IsActive, u.ErpAgentId, u.RowVersion,
            u.Assignments
                .Select(a => (a, g: gs.First(g => g.Id == a.GroupId), p: ps.First(p => p.Id == a.PlantId)))
                .OrderBy(x => x.g.Name).ThenBy(x => x.p.Code)
                .Select(x => new AsignacionDetalle(x.g.Id, x.g.Name, x.g.Code, x.p.Id, x.p.Code, x.a.IsSubstitute))
                .ToList());
    }

    public async Task<IReadOnlyList<AsignacionSolicitada>> VerificarAsync(IReadOnlyList<AsignacionDto> asignaciones, CancellationToken ct)
    {
        var gs = (await grupos.ListarAsync(cancellationToken: ct)).Select(g => g.Id).ToHashSet();
        var ps = (await plantas.ListarAsync(cancellationToken: ct)).Select(p => p.Id).ToHashSet();
        var errores = new List<ErrorValidacion>();
        foreach (var a in asignaciones)
        {
            if (!gs.Contains(a.GrupoId)) errores.Add(new("asignaciones", $"El grupo {a.GrupoId} no existe o está archivado."));
            if (!ps.Contains(a.PlantaId)) errores.Add(new("asignaciones", $"La planta {a.PlantaId} no existe."));
        }
        if (errores.Count > 0) throw new ValidacionException(errores);
        return asignaciones.Select(a => new AsignacionSolicitada(a.GrupoId, a.PlantaId, a.Suplente)).ToList();
    }
}

public sealed class ObtenerUsuarioCaso(IAlmacen<User> usuarios, CatalogoDeSeguridad catalogo) : IUseCase<ObtenerUsuario, UsuarioDetalle>
{
    public async Task<UsuarioDetalle> ExecuteAsync(ObtenerUsuario request, CancellationToken cancellationToken = default) =>
        await catalogo.DetalleAsync(await usuarios.ObtenerAsync(request.Id, "el usuario", cancellationToken), cancellationToken);
}

/// <summary>El Administrador da de alta un usuario con sus grupos por planta y su contraseña (FR-013).</summary>
public sealed class CrearUsuarioCaso(IAlmacen<User> usuarios, CatalogoDeSeguridad catalogo, ICredenciales credenciales, IUnitOfWork uow)
    : IUseCase<CrearUsuario, UsuarioDetalle>
{
    public async Task<UsuarioDetalle> ExecuteAsync(CrearUsuario request, CancellationToken cancellationToken = default)
    {
        var nombre = request.Usuario.Trim();
        if (await usuarios.ExisteAsync(u => u.UserName == nombre, incluirArchivados: true, cancellationToken))
            throw new ReglaDeNegocioException("USUARIO_DUPLICADO", $"Ya existe el usuario {nombre}.");
        var usuario = new User(nombre, request.Nombre, request.Email, await catalogo.VerificarAsync(request.Asignaciones, cancellationToken));
        usuarios.Agregar(usuario);
        await uow.SaveChangesAsync(cancellationToken);
        await credenciales.CrearAsync(usuario.Id, usuario.UserName, request.Contrasena, cancellationToken);
        return await catalogo.DetalleAsync(usuario, cancellationToken);
    }
}

public sealed class EditarUsuarioCaso(IAlmacen<User> usuarios, CatalogoDeSeguridad catalogo, IUnitOfWork uow)
    : IUseCase<EditarUsuario, UsuarioDetalle>
{
    public async Task<UsuarioDetalle> ExecuteAsync(EditarUsuario request, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarios.ObtenerAsync(request.Id, "el usuario", cancellationToken);
        usuarios.ExigirVersion(usuario, request.RowVersion);
        usuario.Editar(request.Nombre, request.Email);
        usuario.AsignarGrupos(await catalogo.VerificarAsync(request.Asignaciones ?? [], cancellationToken));
        await uow.SaveChangesAsync(cancellationToken);
        return await catalogo.DetalleAsync(usuario, cancellationToken);
    }
}

public sealed class ArchivarUsuarioCaso(IAlmacen<User> usuarios, CatalogoDeSeguridad catalogo, IUnitOfWork uow)
    : IUseCase<ArchivarUsuario, UsuarioDetalle>
{
    public async Task<UsuarioDetalle> ExecuteAsync(ArchivarUsuario request, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarios.ObtenerAsync(request.Id, "el usuario", cancellationToken);
        if (request.Archivar) usuario.Archivar();
        else usuario.Restaurar();
        await uow.SaveChangesAsync(cancellationToken);
        return await catalogo.DetalleAsync(usuario, cancellationToken);
    }
}

public sealed class RestablecerContrasenaCaso(IAlmacen<User> usuarios, ICredenciales credenciales) : IUseCase<RestablecerContrasena, Unit>
{
    public async Task<Unit> ExecuteAsync(RestablecerContrasena request, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarios.ObtenerAsync(request.Id, "el usuario", cancellationToken);
        await credenciales.RestablecerAsync(usuario.Id, request.Contrasena ?? string.Empty, cancellationToken);
        return Unit.Value;
    }
}

/// <summary>Liga el usuario a su agente de CONTPAQi; el pedido lo propone (D-153). Null lo desliga.</summary>
public sealed record LigarAgente(long Id, long? AgenteId) : IRequierePermiso
{
    public string Permiso => Permisos.UsuariosLigarAgente;
}

public sealed class LigarAgenteCaso(IAlmacen<User> usuarios, IAlmacen<Domain.Ventas.ErpAgent> agentes, CatalogoDeSeguridad catalogo, IUnitOfWork uow)
    : IUseCase<LigarAgente, UsuarioDetalle>
{
    public async Task<UsuarioDetalle> ExecuteAsync(LigarAgente request, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarios.ObtenerAsync(request.Id, "el usuario", cancellationToken);
        if (request.AgenteId is { } id && !await agentes.ExisteAsync(a => a.Id == id, cancellationToken: cancellationToken))
            throw new ValidacionException([new ErrorValidacion("agenteId", "El agente no existe o ya no está en CONTPAQi.")]);
        usuario.LigarAgente(request.AgenteId);
        await uow.SaveChangesAsync(cancellationToken);
        return await catalogo.DetalleAsync(usuario, cancellationToken);
    }
}
