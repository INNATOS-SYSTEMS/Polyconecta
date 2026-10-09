using Microsoft.AspNetCore.Mvc;
using PolyConecta.Application.Common;
using PolyConecta.Application.Inventario;
using PolyConecta.Application.Ventas;
using PolyConecta.Domain.Inventario;

namespace PolyConecta.Api.Controllers.Catalogos;

/// <summary>Base: resuelve el caso de uso de la petición y lo ejecuta (CT-08).</summary>
public abstract class CasoDeUsoController(IServiceProvider servicios) : ControllerBase
{
    protected Task<TResult> Ejecutar<TRequest, TResult>(TRequest request, CancellationToken ct) =>
        servicios.GetRequiredService<IUseCase<TRequest, TResult>>().ExecuteAsync(request, ct);
}

/// <summary>Productos, clasificaciones y almacenes de Inventario (contracts/api-f1.md, D-155).</summary>
[ApiController]
[Route("api/v1/inventario")]
public sealed class InventarioCatalogosController(IServiceProvider servicios) : CasoDeUsoController(servicios)
{
    public sealed record ClasificarRequest(long? ClasificacionId);

    public sealed record FichaRequest(DatosRollo Rollo, DatosPt Pt, long? RolloLigadoProductoId);

    public sealed record ClasificacionRequest(string Codigo, string Nombre);

    [HttpGet("productos/buscar")]
    public Task<IReadOnlyList<ProductoBusqueda>> Buscar([FromQuery] string? texto, CancellationToken ct) =>
        Ejecutar<BuscarProductos, IReadOnlyList<ProductoBusqueda>>(new(texto), ct);

    [HttpGet("productos/{id:long}")]
    public Task<ProductoDetalle> Producto(long id, CancellationToken ct) => Ejecutar<ObtenerProducto, ProductoDetalle>(new(id), ct);

    [HttpPut("productos/{id:long}/clasificacion")]
    public Task<ProductoDetalle> Clasificar(long id, ClasificarRequest r, CancellationToken ct) =>
        Ejecutar<ClasificarProducto, ProductoDetalle>(new(id, r.ClasificacionId), ct);

    [HttpPut("productos/{id:long}/ficha-tecnica")]
    public Task<ProductoDetalle> Ficha(long id, FichaRequest r, CancellationToken ct) =>
        Ejecutar<GuardarFichaTecnica, ProductoDetalle>(new(id, r.Rollo, r.Pt, r.RolloLigadoProductoId), ct);

    [HttpGet("clasificaciones")]
    public Task<IReadOnlyList<ClasificacionDto>> Clasificaciones(CancellationToken ct) =>
        Ejecutar<ListarClasificaciones, IReadOnlyList<ClasificacionDto>>(new(), ct);

    [HttpPost("clasificaciones")]
    public Task<ClasificacionDto> CrearClasificacion(ClasificacionRequest r, CancellationToken ct) =>
        Ejecutar<GuardarClasificacion, ClasificacionDto>(new(null, r.Codigo, r.Nombre), ct);

    [HttpPut("clasificaciones/{id:long}")]
    public Task<ClasificacionDto> EditarClasificacion(long id, ClasificacionRequest r, CancellationToken ct) =>
        Ejecutar<GuardarClasificacion, ClasificacionDto>(new(id, r.Codigo, r.Nombre), ct);

    [HttpGet("almacenes")]
    public Task<IReadOnlyList<AlmacenDto>> Almacenes(CancellationToken ct) => Ejecutar<ListarAlmacenes, IReadOnlyList<AlmacenDto>>(new(), ct);
}

/// <summary>Clientes y agentes de Ventas (contracts/api-f1.md, D-155).</summary>
[ApiController]
[Route("api/v1/ventas")]
public sealed class VentasCatalogosController(IServiceProvider servicios) : CasoDeUsoController(servicios)
{
    [HttpGet("clientes/buscar")]
    public Task<IReadOnlyList<ClienteBusqueda>> Buscar([FromQuery] string? texto, CancellationToken ct) =>
        Ejecutar<BuscarClientes, IReadOnlyList<ClienteBusqueda>>(new(texto), ct);

    [HttpGet("clientes/{id:long}")]
    public Task<ClienteDetalle> Cliente(long id, CancellationToken ct) => Ejecutar<ObtenerCliente, ClienteDetalle>(new(id), ct);

    [HttpGet("agentes")]
    public Task<IReadOnlyList<AgenteDto>> Agentes(CancellationToken ct) => Ejecutar<ListarAgentes, IReadOnlyList<AgenteDto>>(new(), ct);
}
