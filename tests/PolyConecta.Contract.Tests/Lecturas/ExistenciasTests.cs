using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using PolyConecta.Contract.Tests.Infraestructura;
using Xunit;

namespace PolyConecta.Contract.Tests.Lecturas;

/// <summary>`GET /api/v1/inventory/stocks` (§6): varios productos por consulta, por almacén y por lote.</summary>
public class ExistenciasTests(Bridge bridge)
{
    private const string Ruta = "/api/v1/inventory/stocks";

    private static string Lista(IEnumerable<string> codigos) => Uri.EscapeDataString(string.Join(",", codigos));

    private async Task<IReadOnlyList<JsonObject>> ProductosAsync() =>
        await Lectura.RecorrerAsync(bridge, "/api/v1/catalogs/products", 500);

    [ContratoFact]
    public async Task Sin_productos_se_rechaza()
    {
        var r = await bridge.Http.GetAsync(Ruta);
        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await r.Content.ReadFromJsonAsync<JsonObject>())!["code"]!.GetValue<string>().Should().Be("CARGA_INVALIDA");
    }

    [ContratoFact]
    public async Task Un_producto_desconocido_no_da_existencias()
    {
        var existencias = (await bridge.Http.GetFromJsonAsync<JsonArray>($"{Ruta}?productos=NO-EXISTE-XYZ"))!;
        existencias.Should().BeEmpty();
    }

    [ContratoFact]
    public async Task Las_existencias_vienen_en_la_unidad_base_y_con_su_forma()
    {
        var productos = (await ProductosAsync()).Take(250).ToList();
        var unidades = productos.ToDictionary(p => p["codigo"]!.GetValue<string>(), p => p["unidad_base"]!.GetValue<string>());
        // Más de 100 productos en la consulta ejercita los lotes de 100 del bridge real (L1-T007).
        var existencias = (await bridge.Http.GetFromJsonAsync<JsonArray>($"{Ruta}?productos={Lista(unidades.Keys)}"))!;
        foreach (var e in existencias)
        {
            Lectura.EsSnakeCase(e);
            var producto = e!["producto"]!.GetValue<string>();
            unidades.Should().ContainKey(producto, "solo se devuelven los productos pedidos");
            e["unidad"]!.GetValue<string>().Should().Be(unidades[producto]);
            e["almacen"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
            e["cantidad"]!.GetValue<decimal>();
            e.AsObject().ContainsKey("lote").Should().BeTrue("lote va null si el producto no lleva lote");
        }
    }

    [ContratoFact]
    public async Task Varios_productos_en_una_consulta_traen_lotes_y_el_filtro_de_almacen_los_acota()
    {
        Assert.SkipUnless(bridge.EsSimulado, "Los productos y lotes de la prueba son los del simulador.");
        var pedido = new[] { "R-IV310", "PEBD-001", "PT1113 C567" };
        var todas = (await bridge.Http.GetFromJsonAsync<JsonArray>($"{Ruta}?productos={Lista(pedido)}"))!;

        todas.Select(e => e!["producto"]!.GetValue<string>()).Distinct().Should().BeEquivalentTo(pedido);
        var lotes = todas.Where(e => e!["producto"]!.GetValue<string>() == "R-IV310").Select(e => e!["lote"]!.GetValue<string>());
        lotes.Should().BeEquivalentTo(["R001-IV310-26", "R002-IV310-26"]);
        todas.Where(e => e!["producto"]!.GetValue<string>() == "PEBD-001")
            .Should().OnlyContain(e => e!["lote"] == null, "un producto sin lote no trae lote");

        var enMp = (await bridge.Http.GetFromJsonAsync<JsonArray>($"{Ruta}?productos={Lista(pedido)}&almacen=MP-PIM"))!;
        enMp.Should().NotBeEmpty().And.OnlyContain(e => e!["almacen"]!.GetValue<string>() == "MP-PIM");
        enMp.Count.Should().BeLessThan(todas.Count);
    }
}
