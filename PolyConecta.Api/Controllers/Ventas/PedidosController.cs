using Microsoft.AspNetCore.Mvc;
using PolyConecta.Api.Controllers.Catalogos;
using PolyConecta.Application.Ventas;
using PolyConecta.Domain.Ventas;

namespace PolyConecta.Api.Controllers.Ventas;

/// <summary>Pedidos de venta por id (contracts/api-f1.md, D-154). Solo traduce HTTP ↔ caso de uso (CT-08).</summary>
[ApiController]
[Route("api/v1/ventas/pedidos")]
public sealed class PedidosController(IServiceProvider servicios) : CasoDeUsoController(servicios)
{
    public sealed record EditarRequest(
        byte[] RowVersion, bool RevocarAutorizacion, long ClienteId, string? OrdenCompraCliente, long? AgenteId, DateOnly? FechaPedido,
        DateOnly? FechaPromesa, long? DomicilioEntregaId, string? Moneda, decimal? TipoCambio, IReadOnlyList<LineaPedidoDto>? Lineas)
    {
        public DatosPedidoDto Datos() => new(ClienteId, OrdenCompraCliente, AgenteId, FechaPedido, FechaPromesa, DomicilioEntregaId, Moneda, TipoCambio, Lineas);
    }

    public sealed record VersionRequest(byte[] RowVersion);

    public sealed record AutorizarRequest(byte[] RowVersion, RolFirma? Rol);

    public sealed record MotivoRequest(byte[] RowVersion, string? Motivo);

    [HttpGet("{id:long}")]
    public Task<PedidoDetalle> Obtener(long id, CancellationToken ct) => Ejecutar<ObtenerPedido, PedidoDetalle>(new(id), ct);

    [HttpPost]
    public async Task<IActionResult> Crear(DatosPedidoDto datos, CancellationToken ct)
    {
        var p = await Ejecutar<CrearPedido, PedidoDetalle>(new(datos), ct);
        return CreatedAtAction(nameof(Obtener), new { id = p.Id }, p);
    }

    [HttpPut("{id:long}")]
    public Task<PedidoDetalle> Editar(long id, EditarRequest r, CancellationToken ct) =>
        Ejecutar<EditarPedido, PedidoDetalle>(new(id, r.RowVersion, r.RevocarAutorizacion, r.Datos()), ct);

    [HttpPost("{id:long}/confirmar")]
    public Task<PedidoDetalle> Confirmar(long id, VersionRequest r, CancellationToken ct) =>
        Ejecutar<ConfirmarPedido, PedidoDetalle>(new(id, r.RowVersion), ct);

    [HttpPost("{id:long}/autorizar")]
    public Task<PedidoDetalle> Autorizar(long id, AutorizarRequest r, CancellationToken ct) =>
        Ejecutar<AutorizarPedido, PedidoDetalle>(new(id, r.RowVersion, r.Rol), ct);

    [HttpPost("{id:long}/revocar")]
    public Task<PedidoDetalle> Revocar(long id, MotivoRequest r, CancellationToken ct) =>
        Ejecutar<RevocarAutorizacion, PedidoDetalle>(new(id, r.RowVersion, r.Motivo), ct);

    [HttpPost("{id:long}/cancelar")]
    public Task<PedidoDetalle> Cancelar(long id, MotivoRequest r, CancellationToken ct) =>
        Ejecutar<CancelarPedido, PedidoDetalle>(new(id, r.RowVersion, r.Motivo), ct);
}
