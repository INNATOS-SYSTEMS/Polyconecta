using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PolyConecta.Application.Ventas;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Ventas;

namespace PolyConecta.Infrastructure.Persistence.Sembradores;

/// <summary>
/// El caso representativo de la revisión R1: el pedido IV310-26 del prototipo (spec 001, semilla de
/// <c>OperationalFlowState</c>), con su folio tal cual, en Borrador y capturado por <c>ac1</c>. Es el único pedido
/// de ejemplo. Corre con los datos de R1 (desarrollo o <c>Seguridad__SembrarDatosR1=true</c>, con su contraseña) y
/// solo cuando ya están sincronizados su cliente y su producto: al arrancar y después de cada sincronización. No
/// lo vuelve a crear si existe.
/// </summary>
public sealed partial class PedidoRepresentativo(
    PolyDbContext db,
    IConfiguracionDeMonedas monedas,
    IConfiguration configuracion,
    IHostEnvironment entorno,
    ILogger<PedidoRepresentativo> logger) : IDataSeeder
{
    public const string Folio = "IV310-26";
    public const string Cliente = "EMM-001";
    public const string Producto = "PT1113 C567";
    public const string Agente = "AG-01";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!entorno.IsDevelopment() && !configuracion.GetValue<bool>("Seguridad:SembrarDatosR1")) return;
        if (string.IsNullOrWhiteSpace(configuracion["Seguridad:DatosR1:Contrasena"])) return;
        if (await db.Set<SalesOrder>().IgnoreQueryFilters().AnyAsync(o => o.Name == Folio, cancellationToken)) return;

        var cliente = await db.Clientes.Include(c => c.Addresses).FirstOrDefaultAsync(c => c.ErpCode == Cliente, cancellationToken);
        var producto = await db.Set<PolyConecta.Domain.Inventario.Product>().Include(p => p.PackagingUnits).FirstOrDefaultAsync(p => p.ErpCode == Producto, cancellationToken);
        if (cliente is null || producto is null)
        {
            LogSinCatalogos(logger, Folio);
            return;
        }
        var agente = await db.Set<ErpAgent>().FirstOrDefaultAsync(a => a.ErpCode == Agente, cancellationToken);

        var pedido = SalesOrder.Crear(Folio, new DatosPedido(
            cliente, "3893", agente, new DateOnly(2026, 10, 9), new DateOnly(2026, 10, 30), null, "MXN", 1m,
            [new LineaSolicitada(null, producto, 5500m, 5.70m, null, null)]), monedas.Monedas);
        pedido.MarcarCreado(DateTimeOffset.UtcNow, "ac1");
        db.Add(pedido);
        await db.SaveChangesAsync(cancellationToken);
        LogCreado(logger, Folio);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Pedido representativo {Folio} sembrado.")]
    private static partial void LogCreado(ILogger logger, string folio);

    [LoggerMessage(Level = LogLevel.Information, Message = "El pedido representativo {Folio} espera a que se sincronicen su cliente y su producto.")]
    private static partial void LogSinCatalogos(ILogger logger, string folio);
}
