using AwesomeAssertions;
using PolyConecta.Application.Plataforma.Erp;
using PolyConecta.Domain.Ventas;
using PolyConecta.Infrastructure.Common;
using PolyConecta.Infrastructure.Erp;
using Xunit;

namespace PolyConecta.IntegrationTests.Erp;

/// <summary>
/// Lecturas de F1 contra el bridge simulado (L2-T014, R-05): páginas con cursor hasta agotar y campos de
/// `1.1`. Necesitan el simulador en BRIDGE_URL; sin él se omiten, como el ciclo completo.
/// </summary>
public class BridgeLecturasTests
{
    private static readonly string? BridgeUrl = Environment.GetEnvironmentVariable("BRIDGE_URL");

    public static bool HayBridge => !string.IsNullOrWhiteSpace(BridgeUrl);

    private static BridgeLecturasHttp Lecturas() =>
        new(new HttpClient { BaseAddress = new Uri(BridgeUrl!) }, new CorrelationContext { CorrelationId = "lecturas-prueba" });

    private static async Task<List<T>> TodasAsync<T>(Func<string?, Task<PaginaLectura<T>>> pagina)
    {
        var todas = new List<T>();
        string? cursor = null;
        var vueltas = 0;
        do
        {
            var p = await pagina(cursor);
            todas.AddRange(p.Items);
            cursor = p.Siguiente;
            (++vueltas).Should().BeLessThan(1000, "el cursor debe agotarse");
        }
        while (cursor is not null);
        return todas;
    }

    [Fact(SkipUnless = nameof(HayBridge), Skip = "Lecturas: define BRIDGE_URL con el bridge simulado.")]
    public async Task Productos_paginados_con_unidad_base_lote_y_clasificacion()
    {
        var lecturas = Lecturas();

        var productos = await TodasAsync(c => lecturas.ProductosAsync(3, c));

        productos.Should().NotBeEmpty();
        productos.Select(p => p.Datos.IdErp).Should().OnlyHaveUniqueItems();
        productos.Should().AllSatisfy(p => p.Datos.UnidadBase.Should().NotBeNullOrWhiteSpace());
        productos.Should().Contain(p => p.Datos.LlevaLote).And.Contain(p => !p.Datos.LlevaLote);
        productos.Should().Contain(p => p.Clasificacion != null);
        productos.Should().Contain(p => !p.Datos.Activo, "la semilla 1.1 trae productos inactivos");
    }

    [Fact(SkipUnless = nameof(HayBridge), Skip = "Lecturas: define BRIDGE_URL con el bridge simulado.")]
    public async Task Clientes_con_moneda_y_un_domicilio_fiscal_y_los_de_envio()
    {
        var lecturas = Lecturas();

        var clientes = await TodasAsync(c => lecturas.ClientesAsync(2, c));

        clientes.Should().NotBeEmpty();
        clientes.Select(c => c.Moneda).Should().Contain("USD").And.Contain("MXN");
        clientes.Where(c => c.Domicilios is { Count: > 0 })
            .Should().NotBeEmpty()
            .And.AllSatisfy(c => c.Domicilios!.Count(d => d.Tipo == TipoDomicilio.Fiscal).Should().Be(1));
        clientes.Should().Contain(c => c.Domicilios!.Count(d => d.Tipo == TipoDomicilio.Envio) > 1);
    }

    [Fact(SkipUnless = nameof(HayBridge), Skip = "Lecturas: define BRIDGE_URL con el bridge simulado.")]
    public async Task Agentes_y_almacenes()
    {
        var lecturas = Lecturas();

        (await TodasAsync(c => lecturas.AgentesAsync(100, c))).Should().NotBeEmpty();
        (await lecturas.AlmacenesAsync()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task Sin_bridge_la_lectura_falla_con_su_motivo()
    {
        var lecturas = new BridgeLecturasHttp(new HttpClient { BaseAddress = new Uri("http://localhost:1") }, new CorrelationContext());

        var leer = () => lecturas.ProductosAsync(10, null);

        await leer.Should().ThrowAsync<LecturaBridgeException>();
    }
}
