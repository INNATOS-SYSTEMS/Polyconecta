using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolyConecta.Api.Seguridad;
using PolyConecta.Application.Common;
using PolyConecta.Infrastructure.Plataforma.Identidad;

namespace PolyConecta.Api.Controllers.Plataforma;

/// <summary>Entrar, leer la sesión y salir (contracts/api-f1.md, Sesión; R-01).</summary>
[ApiController]
[Route("api/v1/plataforma/sesion")]
public sealed class SesionController(ServicioDeSesion sesion, ICurrentUser usuario) : ControllerBase
{
    public sealed record EntrarRequest(string Usuario, string Contrasena);

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Entrar(EntrarRequest request, CancellationToken cancellationToken)
    {
        var (resultado, userId) = await sesion.EntrarAsync(request.Usuario ?? string.Empty, request.Contrasena ?? string.Empty, cancellationToken);
        switch (resultado)
        {
            case ResultadoEntrada.Bloqueado:
                await Autenticacion.EscribirAsync(Response, 401, "USUARIO_BLOQUEADO",
                    "Demasiados intentos fallidos: el usuario queda bloqueado 15 minutos.");
                return new EmptyResult();
            case ResultadoEntrada.CredencialesInvalidas:
                await Autenticacion.EscribirAsync(Response, 401, "CREDENCIALES_INVALIDAS", "Usuario o contraseña incorrectos.");
                return new EmptyResult();
            default:
                return Ok(await sesion.SesionAsync(userId!.Value, cancellationToken));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Leer(CancellationToken cancellationToken) =>
        usuario.UserId is { } id && await sesion.SesionAsync(id, cancellationToken) is { } s ? Ok(s) : Unauthorized();

    [HttpDelete]
    [AllowAnonymous]
    public async Task<IActionResult> Salir()
    {
        await sesion.SalirAsync();
        return NoContent();
    }
}
