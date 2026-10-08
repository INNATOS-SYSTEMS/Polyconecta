using System;
using System.Linq;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Simulated;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Contpaq.Bridge.Api.Controllers
{
    /// <summary>
    /// Cambia el catálogo simulado en caliente (spec 003, C-T006; fuera del contrato, §7): agrega o
    /// reemplaza un producto o un cliente, o lo quita. Sirve para probar la sincronización, el
    /// archivado y los cambios de moneda sin reiniciar. En modo real no existe y responde 404.
    /// </summary>
    [ApiController]
    [Route("admin/simulated/catalog")]
    public class CatalogoSimuladoController(IServiceProvider servicios) : ControllerBase
    {
        private SimulatedCatalog? Catalogo => servicios.GetService<SimulatedCatalog>();

        [HttpPut("products/{codigo}")]
        public IActionResult GuardarProducto(string codigo, [FromBody] ProductoContrato producto)
        {
            if (Catalogo is not { } catalogo) return NotFound();
            if (string.IsNullOrWhiteSpace(producto.Nombre) || string.IsNullOrWhiteSpace(producto.UnidadBase))
                return BadRequest(ErrorContrato.De(CodigosError.CargaInvalida, "nombre y unidad_base son obligatorios.", new() { ["campo"] = "nombre" }));
            return Ok(catalogo.GuardarProducto(codigo, producto));
        }

        [HttpDelete("products/{codigo}")]
        public IActionResult QuitarProducto(string codigo) =>
            Catalogo is { } catalogo && catalogo.QuitarProducto(codigo) ? NoContent() : NotFound();

        [HttpPut("clients/{codigo}")]
        public IActionResult GuardarCliente(string codigo, [FromBody] ClienteContrato cliente)
        {
            if (Catalogo is not { } catalogo) return NotFound();
            if (string.IsNullOrWhiteSpace(cliente.RazonSocial))
                return BadRequest(ErrorContrato.De(CodigosError.CargaInvalida, "razon_social es obligatoria.", new() { ["campo"] = "razon_social" }));
            if (cliente.Domicilios.Count > 0 && cliente.Domicilios.Count(d => d.Tipo == "fiscal") != 1)
                return BadRequest(ErrorContrato.De(CodigosError.CargaInvalida, "domicilios: debe haber exactamente un domicilio fiscal.", new() { ["campo"] = "domicilios" }));
            if (cliente.Domicilios.Any(d => d.Tipo is not ("fiscal" or "envio")))
                return BadRequest(ErrorContrato.De(CodigosError.CargaInvalida, "domicilios: el tipo es fiscal o envio.", new() { ["campo"] = "domicilios" }));
            return Ok(catalogo.GuardarCliente(codigo, cliente));
        }

        [HttpDelete("clients/{codigo}")]
        public IActionResult QuitarCliente(string codigo) =>
            Catalogo is { } catalogo && catalogo.QuitarCliente(codigo) ? NoContent() : NotFound();
    }
}
