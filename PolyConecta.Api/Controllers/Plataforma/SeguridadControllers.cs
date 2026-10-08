using Microsoft.AspNetCore.Mvc;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Seguridad;

namespace PolyConecta.Api.Controllers.Plataforma;

/// <summary>Usuarios (contracts/api-f1.md). Solo traduce HTTP ↔ caso de uso (CT-08); el permiso lo decide Application.</summary>
[ApiController]
[Route("api/v1/plataforma/usuarios")]
public sealed class UsuariosController(IServiceProvider servicios) : ControllerBase
{
    public sealed record CrearRequest(string Usuario, string Nombre, string? Email, string Contrasena, IReadOnlyList<AsignacionDto> Asignaciones);

    public sealed record EditarRequest(byte[] RowVersion, string Nombre, string? Email, IReadOnlyList<AsignacionDto> Asignaciones);

    public sealed record ContrasenaRequest(string Contrasena);

    private Task<TResult> Ejecutar<TRequest, TResult>(TRequest request, CancellationToken ct) =>
        servicios.GetRequiredService<IUseCase<TRequest, TResult>>().ExecuteAsync(request, ct);

    [HttpGet("{id:long}")]
    public Task<UsuarioDetalle> Obtener(long id, CancellationToken ct) => Ejecutar<ObtenerUsuario, UsuarioDetalle>(new(id), ct);

    [HttpPost]
    public async Task<IActionResult> Crear(CrearRequest r, CancellationToken ct)
    {
        var u = await Ejecutar<CrearUsuario, UsuarioDetalle>(new(r.Usuario, r.Nombre, r.Email, r.Contrasena, r.Asignaciones ?? []), ct);
        return CreatedAtAction(nameof(Obtener), new { id = u.Id }, u);
    }

    [HttpPut("{id:long}")]
    public Task<UsuarioDetalle> Editar(long id, EditarRequest r, CancellationToken ct) =>
        Ejecutar<EditarUsuario, UsuarioDetalle>(new(id, r.RowVersion, r.Nombre, r.Email, r.Asignaciones ?? []), ct);

    [HttpPost("{id:long}/archivar")]
    public Task<UsuarioDetalle> Archivar(long id, CancellationToken ct) => Ejecutar<ArchivarUsuario, UsuarioDetalle>(new(id, true), ct);

    [HttpPost("{id:long}/restaurar")]
    public Task<UsuarioDetalle> Restaurar(long id, CancellationToken ct) => Ejecutar<ArchivarUsuario, UsuarioDetalle>(new(id, false), ct);

    [HttpPost("{id:long}/contrasena")]
    public async Task<IActionResult> Contrasena(long id, ContrasenaRequest r, CancellationToken ct)
    {
        await Ejecutar<RestablecerContrasena, Unit>(new(id, r.Contrasena), ct);
        return NoContent();
    }
}

/// <summary>Grupos y su catálogo de permisos (contracts/api-f1.md, D-148).</summary>
[ApiController]
[Route("api/v1/plataforma")]
public sealed class GruposController(IServiceProvider servicios) : ControllerBase
{
    public sealed record CrearRequest(string Codigo, string Nombre, string? Descripcion, long? CopiarDe);

    public sealed record EditarRequest(byte[] RowVersion, string Nombre, string? Descripcion, IReadOnlyList<string> Permisos);

    private Task<TResult> Ejecutar<TRequest, TResult>(TRequest request, CancellationToken ct) =>
        servicios.GetRequiredService<IUseCase<TRequest, TResult>>().ExecuteAsync(request, ct);

    [HttpGet("permisos")]
    public Task<IReadOnlyList<ModuloDePermiso>> Permisos(CancellationToken ct) =>
        Ejecutar<ArbolDePermisos, IReadOnlyList<ModuloDePermiso>>(new(), ct);

    [HttpGet("grupos/{id:long}")]
    public Task<GrupoDetalle> Obtener(long id, CancellationToken ct) => Ejecutar<ObtenerGrupo, GrupoDetalle>(new(id), ct);

    [HttpPost("grupos")]
    public async Task<IActionResult> Crear(CrearRequest r, CancellationToken ct)
    {
        var g = await Ejecutar<CrearGrupo, GrupoDetalle>(new(r.Codigo, r.Nombre, r.Descripcion, r.CopiarDe), ct);
        return CreatedAtAction(nameof(Obtener), new { id = g.Id }, g);
    }

    [HttpPut("grupos/{id:long}")]
    public Task<GrupoDetalle> Editar(long id, EditarRequest r, CancellationToken ct) =>
        Ejecutar<EditarGrupo, GrupoDetalle>(new(id, r.RowVersion, r.Nombre, r.Descripcion, r.Permisos ?? []), ct);

    [HttpPost("grupos/{id:long}/archivar")]
    public Task<GrupoDetalle> Archivar(long id, CancellationToken ct) => Ejecutar<ArchivarGrupo, GrupoDetalle>(new(id, true), ct);

    [HttpPost("grupos/{id:long}/restaurar")]
    public Task<GrupoDetalle> Restaurar(long id, CancellationToken ct) => Ejecutar<ArchivarGrupo, GrupoDetalle>(new(id, false), ct);
}
