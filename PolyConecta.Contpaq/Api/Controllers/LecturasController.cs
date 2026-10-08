using System;
using System.Linq;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Contpaq.Bridge.Api.Controllers
{
    /// <summary>Lecturas del contrato (§6). Lo que un modo todavía no implementa responde 501.</summary>
    [ApiController]
    [Route("api/v1")]
    public class LecturasController(IReadRepository lecturas) : ControllerBase
    {
        private const int LimiteMaximo = 500;

        [HttpGet("catalogs/products")]
        public Task<IActionResult> Productos(string? search, DateTimeOffset? modified_since, int limit = 100, string? cursor = null) =>
            Leer(limit, () => lecturas.ProductosAsync(search, modified_since, limit, cursor));

        [HttpGet("catalogs/clients")]
        public Task<IActionResult> Clientes(string? search, DateTimeOffset? modified_since, int limit = 100, string? cursor = null) =>
            Leer(limit, () => lecturas.ClientesAsync(search, modified_since, limit, cursor));

        [HttpGet("catalogs/agents")]
        public Task<IActionResult> Agentes(int limit = 100, string? cursor = null) =>
            Leer(limit, () => lecturas.AgentesAsync(limit, cursor));

        [HttpGet("catalogs/warehouses")]
        public Task<IActionResult> Almacenes() => Leer(1, () => lecturas.AlmacenesAsync());

        [HttpGet("inventory/stocks")]
        public Task<IActionResult> Existencias(string? productos, string? almacen)
        {
            if (string.IsNullOrWhiteSpace(productos))
                return Task.FromResult<IActionResult>(BadRequest(ErrorContrato.De(CodigosError.CargaInvalida,
                    "productos: Es obligatorio.", new() { ["campo"] = "productos" })));
            var codigos = productos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            return Leer(1, () => lecturas.ExistenciasAsync(codigos, almacen));
        }

        [HttpGet("inventory/purchases")]
        public Task<IActionResult> RecepcionesCompra(DateTimeOffset? modified_since, int limit = 100, string? cursor = null) =>
            Leer(limit, () => lecturas.RecepcionesCompraAsync(modified_since, limit, cursor));

        private async Task<IActionResult> Leer<T>(int limit, Func<Task<T>> lectura)
        {
            if (limit is < 1 or > LimiteMaximo)
                return BadRequest(ErrorContrato.De(CodigosError.CargaInvalida, $"limit: Debe estar entre 1 y {LimiteMaximo}.", new() { ["campo"] = "limit" }));
            try
            {
                return Ok(await lectura());
            }
            catch (LecturaNoDisponibleException ex)
            {
                return StatusCode(StatusCodes.Status501NotImplemented, ErrorContrato.De(CodigosError.SdkError, ex.Message));
            }
        }
    }
}
