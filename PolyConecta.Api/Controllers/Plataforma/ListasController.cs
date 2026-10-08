using Microsoft.AspNetCore.Mvc;
using PolyConecta.Application.Common;
using PolyConecta.Application.Common.Listas;

namespace PolyConecta.Api.Controllers.Plataforma;

/// <summary>
/// Controlador único de consulta y conjuntos de listas (contracts/api-listas.md, D-151, D-155).
/// </summary>
[ApiController]
[Route("api/v1/{modulo}/{lista}")]
public sealed class ListasController(
    IEnumerable<IConsultaDeLista> listas,
    ICurrentUser usuario,
    Autorizacion autorizacion) : ControllerBase
{
    private async Task<IConsultaDeLista> ResolverListaAsync(string modulo, string lista, CancellationToken ct)
    {
        if (usuario.UserId is null)
        {
            throw new UnauthorizedAccessException("No hay una sesión activa.");
        }

        var servicio = listas.FirstOrDefault(l =>
            l.Modulo.Equals(modulo, StringComparison.OrdinalIgnoreCase) &&
            l.Lista.Equals(lista, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"No existe la lista {modulo}/{lista}.");

        var grupo = await autorizacion.ResolverAsync(servicio.PermisoLectura, cancellationToken: ct);
        if (grupo is null)
        {
            throw PermisoDenegadoException.Para(servicio.PermisoLectura);
        }

        return servicio;
    }

    [HttpGet("vista")]
    public async Task<IActionResult> Vista(string modulo, string lista, CancellationToken ct)
    {
        var servicio = await ResolverListaAsync(modulo, lista, ct);
        return Ok(servicio.DescribirVista());
    }

    [HttpPost("conjunto")]
    public async Task<IActionResult> Conjunto(string modulo, string lista, CancellationToken ct)
    {
        var servicio = await ResolverListaAsync(modulo, lista, ct);
        return Ok(await servicio.ConjuntoAsync(ct));
    }

    [HttpPost("consulta")]
    public async Task<IActionResult> Consulta(string modulo, string lista, [FromBody] ConsultaLista? consulta, CancellationToken ct)
    {
        var servicio = await ResolverListaAsync(modulo, lista, ct);
        return Ok(await servicio.ConsultarAsync(consulta ?? new(), ct));
    }
}
