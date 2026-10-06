using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Contpaq.Bridge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Contpaq.Bridge.Api.Controllers
{
    /// <summary>
    /// Rutas de operación fuera del contrato (§7): conceptos y facturas. Solo existen en modo real,
    /// porque leen CONTPAQi directamente; en modo simulado responden 501.
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    public class CatalogsController(IServiceProvider servicios) : ControllerBase
    {
        private ISqlReadRepository? Sql => servicios.GetService<ISqlReadRepository>();

        [HttpGet("catalogs/concepts")]
        public async Task<IActionResult> GetConcepts()
        {
            if (Sql is null) return StatusCode(StatusCodes.Status501NotImplemented, new { error = "Solo en modo real." });
            try
            {
                return Ok(await Sql.GetConceptsAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database read error", details = ex.Message });
            }
        }

        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices([FromQuery] string? search, [FromQuery] int limit = 100)
        {
            if (Sql is null) return StatusCode(StatusCodes.Status501NotImplemented, new { error = "Solo en modo real." });
            var sw = Stopwatch.StartNew();
            try
            {
                var invoices = await Sql.GetInvoicesAsync(search, limit);
                return Ok(new { invoices, query_time_ms = sw.ElapsedMilliseconds });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database read error", details = ex.Message });
            }
        }
    }
}
