using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using PolyConecta.Contract.Tests.Infraestructura;
using Xunit;

namespace PolyConecta.Contract.Tests.Lecturas;

/// <summary>`GET /api/v1/inventory/purchases` (§6): llega en F3 para el bridge real (D-102).</summary>
public class RecepcionesCompraTests(Bridge bridge)
{
    [ContratoFact]
    public async Task Las_recepciones_de_compra_se_paginan()
    {
        Assert.SkipUnless(bridge.EsSimulado, "En modo real llegan en F3 (D-102).");
        var pagina = (await bridge.Http.GetFromJsonAsync<JsonObject>("/api/v1/inventory/purchases?limit=10"))!;
        pagina["items"].Should().NotBeNull();
    }
}
