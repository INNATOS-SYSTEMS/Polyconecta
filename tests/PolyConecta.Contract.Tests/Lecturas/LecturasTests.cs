using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using PolyConecta.Contract.Tests.Infraestructura;
using Xunit;

namespace PolyConecta.Contract.Tests.Lecturas;

/// <summary>Lecturas del contrato (§6): forma, paginación y filtros.</summary>
public class LecturasTests(Bridge bridge)
{
    [ContratoFact]
    public async Task Los_productos_traen_unidad_base_lote_y_estado_y_se_paginan_con_cursor()
    {
        var primera = (await bridge.Http.GetFromJsonAsync<JsonObject>("/api/v1/catalogs/products?limit=2"))!;
        var items = primera["items"]!.AsArray();
        items.Should().HaveCount(2);
        foreach (var p in items)
        {
            p!["codigo"]!.GetValue<string>().Should().NotBeEmpty();
            p["unidad_base"]!.GetValue<string>().Should().NotBeEmpty();
            p["lleva_lote"].Should().NotBeNull();
            p["activo"].Should().NotBeNull();
        }

        var cursor = primera["next_cursor"]!.GetValue<string>();
        var segunda = (await bridge.Http.GetFromJsonAsync<JsonObject>($"/api/v1/catalogs/products?limit=2&cursor={Uri.EscapeDataString(cursor)}"))!;
        segunda["items"]!.AsArray().Select(p => p!["codigo"]!.GetValue<string>())
            .Should().NotIntersectWith(items.Select(p => p!["codigo"]!.GetValue<string>()));
    }

    [ContratoFact]
    public async Task Los_clientes_y_los_almacenes_tienen_su_forma()
    {
        var clientes = (await bridge.Http.GetFromJsonAsync<JsonObject>("/api/v1/catalogs/clients?limit=10"))!;
        clientes["items"]!.AsArray().Should().AllSatisfy(c => c!["razon_social"]!.GetValue<string>().Should().NotBeEmpty());

        var almacenes = (await bridge.Http.GetFromJsonAsync<JsonArray>("/api/v1/catalogs/warehouses"))!;
        almacenes.Should().AllSatisfy(a => a!["id_erp"]!.GetValue<long>().Should().BePositive());
    }

    [ContratoFact]
    public async Task Las_existencias_requieren_productos_y_vienen_en_la_unidad_base()
    {
        (await bridge.Http.GetAsync("/api/v1/inventory/stocks")).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var productos = (await bridge.Http.GetFromJsonAsync<JsonObject>("/api/v1/catalogs/products?limit=500"))!["items"]!.AsArray();
        var conLote = productos.First(p => p!["lleva_lote"]!.GetValue<bool>())!;
        var codigo = conLote["codigo"]!.GetValue<string>();
        var existencias = (await bridge.Http.GetFromJsonAsync<JsonArray>($"/api/v1/inventory/stocks?productos={Uri.EscapeDataString(codigo)}"))!;
        existencias.Should().AllSatisfy(e =>
        {
            e!["producto"]!.GetValue<string>().Should().Be(codigo);
            e["unidad"]!.GetValue<string>().Should().Be(conLote["unidad_base"]!.GetValue<string>());
        });
    }

    [ContratoFact]
    public async Task Un_limite_fuera_de_rango_se_rechaza()
    {
        (await bridge.Http.GetAsync("/api/v1/catalogs/products?limit=0")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await bridge.Http.GetAsync("/api/v1/catalogs/products?limit=501")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [ContratoFact]
    public async Task Las_recepciones_de_compra_se_paginan()
    {
        Assert.SkipUnless(bridge.EsSimulado, "En modo real llegan en F3 (D-102).");
        var pagina = (await bridge.Http.GetFromJsonAsync<JsonObject>("/api/v1/inventory/purchases?limit=10"))!;
        pagina["items"].Should().NotBeNull();
    }
}
