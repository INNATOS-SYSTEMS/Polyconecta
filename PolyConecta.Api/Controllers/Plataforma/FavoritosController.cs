using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Listas;

namespace PolyConecta.Api.Controllers.Plataforma;

/// <summary>Favoritos del usuario por lista (contracts/api-listas.md, Favoritos). Solo el dueño los ve y los cambia.</summary>
[ApiController]
[Route("api/v1/plataforma/favoritos")]
public sealed class FavoritosController(
    IUseCase<ListarFavoritos, IReadOnlyList<FavoritoDto>> listar,
    IUseCase<GuardarFavorito, FavoritoDto> guardar,
    IUseCase<BorrarFavorito, Unit> borrar) : ControllerBase
{
    public sealed record GuardarFavoritoRequest(JsonElement Definicion, bool PorOmision);

    [HttpGet("{llaveLista}")]
    public Task<IReadOnlyList<FavoritoDto>> Listar(string llaveLista, CancellationToken ct) =>
        listar.ExecuteAsync(new ListarFavoritos(llaveLista), ct);

    [HttpPut("{llaveLista}/{nombre}")]
    public Task<FavoritoDto> Guardar(string llaveLista, string nombre, GuardarFavoritoRequest request, CancellationToken ct) =>
        guardar.ExecuteAsync(new GuardarFavorito(llaveLista, nombre, request.Definicion, request.PorOmision), ct);

    [HttpDelete("{llaveLista}/{nombre}")]
    public async Task<IActionResult> Borrar(string llaveLista, string nombre, CancellationToken ct)
    {
        await borrar.ExecuteAsync(new BorrarFavorito(llaveLista, nombre), ct);
        return NoContent();
    }
}
