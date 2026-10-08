using Microsoft.AspNetCore.Mvc;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Sincronizacion;

namespace PolyConecta.Api.Controllers.Plataforma;

/// <summary>Estado y "Sincronizar ahora" por catálogo y en general (contracts/api-f1.md, US3 escenario 7).</summary>
[ApiController]
[Route("api/v1/plataforma/sincronizacion")]
public sealed class SincronizacionController(
    IUseCase<EstadoDeSincronizacion, IReadOnlyList<EstadoCatalogo>> estado,
    IUseCase<SincronizarTodo, IReadOnlyList<EstadoCatalogo>> todo,
    IUseCase<SincronizarCatalogo, EstadoCatalogo> catalogo) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<EstadoCatalogo>> Estado(CancellationToken ct) => estado.ExecuteAsync(new EstadoDeSincronizacion(), ct);

    [HttpPost]
    public Task<IReadOnlyList<EstadoCatalogo>> Todo(CancellationToken ct) => todo.ExecuteAsync(new SincronizarTodo(), ct);

    [HttpPost("{nombre}")]
    public Task<EstadoCatalogo> Catalogo(string nombre, CancellationToken ct) => catalogo.ExecuteAsync(new SincronizarCatalogo(nombre), ct);
}
