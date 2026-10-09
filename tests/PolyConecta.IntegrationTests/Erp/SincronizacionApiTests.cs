using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.IntegrationTests.Plataforma;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.IntegrationTests.Erp;

/// <summary>
/// "Sincronizar ahora" por la API (L2-T016, US3). Las pruebas contra el simulador necesitan BRIDGE_URL y
/// usan sus rutas de operación (C-T006) para inactivar un producto en caliente.
/// </summary>
public class SincronizacionApiTests(SqlServerFixture sql)
{
    private static readonly string? BridgeUrl = Environment.GetEnvironmentVariable("BRIDGE_URL");

    public static bool HayBridge => !string.IsNullOrWhiteSpace(BridgeUrl);

    private static async Task<(Entorno Entorno, ApiDePrueba Api, HttpClient Sistemas)> LevantarAsync(SqlServerFixture sql, string bridge)
    {
        var entorno = await Entorno.CrearAsync(sql);
        var api = new ApiDePrueba(entorno, new Dictionary<string, string?> { ["Erp:BridgeUrl"] = bridge });
        await api.CrearUsuarioAsync("sistemas", "Sistemas", (GruposIniciales.Sistemas, "PIM", false));
        return (entorno, api, await api.ClienteAsync("sistemas"));
    }

    private static async Task<JsonArray> EstadosAsync(HttpResponseMessage r)
    {
        var texto = await r.Content.ReadAsStringAsync();
        r.StatusCode.Should().Be(HttpStatusCode.OK, texto);
        return JsonNode.Parse(texto)!.AsArray();
    }

    [Fact(SkipUnless = nameof(HayBridge), Skip = "Sincronización: define BRIDGE_URL con el bridge simulado.")]
    public async Task Sincronizar_todo_trae_el_catalogo_y_un_producto_inactivado_en_el_simulador_se_archiva()
    {
        var (entorno, api, sistemas) = await LevantarAsync(sql, BridgeUrl!);
        await using var _ = api;

        var todo = await EstadosAsync(await sistemas.PostAsync("/api/v1/plataforma/sincronizacion", null));
        todo.Select(e => e!["resultado"]!.GetValue<string>()).Should().AllBe("Exito");
        todo.Single(e => e!["catalogo"]!.GetValue<string>() == "productos")!["leidos"]!.GetValue<int>().Should().BeGreaterThan(0);

        var segunda = JsonNode.Parse(await (await sistemas.PostAsync("/api/v1/plataforma/sincronizacion/productos", null)).Content.ReadAsStringAsync())!;
        segunda["cambiados"]!.GetValue<int>().Should().Be(0, "SC-004");

        // Un producto activo del simulador pasa a inactivo en caliente (C-T006).
        string codigo;
        await using (var db = entorno.Contexto())
            codigo = (await db.Productos.OrderBy(p => p.ErpProductId).FirstAsync()).ErpCode;
        using var simulador = new HttpClient { BaseAddress = new Uri(BridgeUrl!) };
        var actual = (await simulador.GetFromJsonAsync<JsonObject>($"/api/v1/catalogs/products?limit=500"))!["items"]!.AsArray()
            .Single(p => p!["codigo"]!.GetValue<string>() == codigo)!.AsObject();
        var inactivo = JsonNode.Parse(actual.ToJsonString())!.AsObject();
        inactivo["activo"] = false;
        (await simulador.PutAsJsonAsync($"/admin/simulated/catalog/products/{Uri.EscapeDataString(codigo)}", inactivo)).EnsureSuccessStatusCode();
        try
        {
            var r = JsonNode.Parse(await (await sistemas.PostAsync("/api/v1/plataforma/sincronizacion/productos", null)).Content.ReadAsStringAsync())!;
            r["archivados"]!.GetValue<int>().Should().Be(1);
            await using var db = entorno.Contexto();
            (await db.Productos.AnyAsync(p => p.ErpCode == codigo)).Should().BeFalse("no se ofrece en una línea nueva");
            (await db.Productos.IgnoreQueryFilters().AnyAsync(p => p.ErpCode == codigo)).Should().BeTrue("se archiva, no se borra");
        }
        finally
        {
            (await simulador.PutAsJsonAsync($"/admin/simulated/catalog/products/{Uri.EscapeDataString(codigo)}", actual)).EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task Con_el_bridge_caido_queda_en_Error_con_su_motivo_y_no_archiva_nada()
    {
        var (_, api, sistemas) = await LevantarAsync(sql, "http://localhost:1");
        await using var _ = api;

        var r = JsonNode.Parse(await (await sistemas.PostAsync("/api/v1/plataforma/sincronizacion/clientes", null)).Content.ReadAsStringAsync())!;

        r["resultado"]!.GetValue<string>().Should().Be("Error");
        r["error"]!.GetValue<string>().Should().Contain("No se pudo leer");
        var estado = await EstadosAsync(await sistemas.GetAsync("/api/v1/plataforma/sincronizacion"));
        estado.Select(e => e!["catalogo"]!.GetValue<string>()).Should().Equal("almacenes", "agentes", "clientes", "productos");
    }

    [Fact]
    public async Task Sin_el_permiso_de_sincronizar_la_API_responde_403()
    {
        var (_, api, _) = await LevantarAsync(sql, "http://localhost:1");
        await using var _ = api;
        await api.CrearUsuarioAsync("planner1", "Planner", (GruposIniciales.Planner, "PIM", false));
        var planner = await api.ClienteAsync("planner1");

        (await planner.PostAsync("/api/v1/plataforma/sincronizacion", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await planner.GetAsync("/api/v1/plataforma/sincronizacion")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
