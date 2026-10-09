using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.IntegrationTests.Plataforma;
using PolyConecta.IntegrationTests.Soporte;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.IntegrationTests.Catalogos;

/// <summary>API de catálogos (L2-T017, US3): producto con ficha, clasificación, clientes, búsquedas, agentes y almacenes.</summary>
public class CatalogosApiTests(SqlServerFixture sql)
{
    private static async Task<(ApiDePrueba Api, Catalogo Cat, HttpClient Admin, HttpClient Ac)> LevantarAsync(SqlServerFixture sql)
    {
        var entorno = await Entorno.CrearAsync(sql);
        var cat = await CatalogoDePrueba.SembrarAsync(entorno);
        var api = new ApiDePrueba(entorno);
        await api.CrearUsuarioAsync("ac1", "Celia Villarreal", (GruposIniciales.AtencionClientes, "PIM", false));
        return (api, cat, await api.ClienteAsync(), await api.ClienteAsync("ac1"));
    }

    private static async Task<JsonNode> JsonAsync(HttpResponseMessage r)
    {
        var texto = await r.Content.ReadAsStringAsync();
        r.IsSuccessStatusCode.Should().BeTrue(texto);
        return JsonNode.Parse(texto)!;
    }

    private static readonly object Rollo = new
    {
        materialType = "PEBD", rollTypeSize = "Tubular 44 cm", gaugeMicrons = 60, kgPerRoll = 25, treatmentDynes = 38,
        pigment = "Blanco", additive = (string?)null, perforation = (string?)null, preliminaryPrint = (string?)null,
    };

    private static readonly object Pt = new
    {
        customerPartNumber = "BOL-004", finalSize = "44x84", inks = "2 tintas", pantones = "286", dieCut = "Sin suaje",
        packaging = "Caja 1000", sealType = "Fondo", kgPerThousand = 9.8,
    };

    [Fact]
    public async Task El_Administrador_clasifica_y_AC_captura_la_ficha_tecnica_con_los_dos_bloques()
    {
        var (api, cat, admin, ac) = await LevantarAsync(sql);
        await using var _ = api;

        var clasificacion = await JsonAsync(await admin.PostAsJsonAsync("/api/v1/inventario/clasificaciones", new { codigo = "bolsa", nombre = "Bolsa" }));
        var clasificado = await JsonAsync(await admin.PutAsJsonAsync($"/api/v1/inventario/productos/{cat.Bolsa}/clasificacion",
            new { clasificacionId = clasificacion["id"]!.GetValue<long>() }));
        clasificado["clasificacion"]!.GetValue<string>().Should().Be("Bolsa");

        // AC no clasifica (permiso del Administrador) pero sí captura la ficha.
        (await ac.PutAsJsonAsync($"/api/v1/inventario/productos/{cat.Bolsa}/clasificacion", new { clasificacionId = (long?)null }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var detalleAc = await JsonAsync(await ac.GetAsync($"/api/v1/inventario/productos/{cat.Bolsa}"));
        detalleAc["acciones"]!.AsArray().Single(a => a!["accion"]!.GetValue<string>() == "clasificar")!["disponible"]!.GetValue<bool>().Should().BeFalse();

        var conFicha = await JsonAsync(await ac.PutAsJsonAsync($"/api/v1/inventario/productos/{cat.Bolsa}/ficha-tecnica", new { rollo = Rollo, pt = Pt }));
        conFicha["ficha"]!["rollo"]!["materialType"]!.GetValue<string>().Should().Be("PEBD");
        conFicha["ficha"]!["pt"]!["kgPerThousand"]!.GetValue<decimal>().Should().Be(9.8m);
        conFicha["unidad"]!.GetValue<string>().Should().Be("PZA");

        var sinPt = await ac.PutAsJsonAsync($"/api/v1/inventario/productos/{cat.Bolsa}/ficha-tecnica", new { rollo = Rollo, pt = (object?)null });
        sinPt.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await sinPt.Content.ReadAsStringAsync()).Should().Contain("\"code\":\"VALIDACION\"");
    }

    [Fact]
    public async Task El_cliente_trae_moneda_y_domicilios_y_la_busqueda_del_pedido_sus_domicilios_de_envio()
    {
        var (api, cat, _, ac) = await LevantarAsync(sql);
        await using var _ = api;

        var cliente = await JsonAsync(await ac.GetAsync($"/api/v1/ventas/clientes/{cat.Emm}"));
        cliente["moneda"]!.GetValue<string>().Should().Be("USD");
        cliente["domicilios"]!.AsArray().Should().HaveCount(3);

        var encontrados = (await JsonAsync(await ac.GetAsync("/api/v1/ventas/clientes/buscar?texto=EMM"))).AsArray();
        encontrados.Should().ContainSingle();
        encontrados[0]!["domiciliosEnvio"]!.AsArray().Select(d => d!["id"]!.GetValue<long>()).Should().Equal(cat.EnvioNorte, cat.EnvioSaltillo);

        var productos = (await JsonAsync(await ac.GetAsync("/api/v1/inventario/productos/buscar?texto="))).AsArray();
        productos.Select(p => p!["clave"]!.GetValue<string>()).Should().Equal("PEBD-001", "PT1113 C567");
        productos[1]!["unidad"]!.GetValue<string>().Should().Be("PZA");

        (await JsonAsync(await ac.GetAsync("/api/v1/ventas/agentes"))).AsArray().Should().HaveCount(2);
        (await JsonAsync(await ac.GetAsync("/api/v1/inventario/almacenes"))).AsArray().Should().ContainSingle();
    }

    [Fact]
    public async Task Un_producto_archivado_no_se_ofrece_y_su_id_sigue_abriendo()
    {
        var (api, cat, admin, _) = await LevantarAsync(sql);
        await using var _ = api;

        await api.CrearUsuarioAsync("ac2", "AC Dos", (GruposIniciales.AtencionClientes, "PIM", false));
        var ac = await api.ClienteAsync("ac2");
        (await JsonAsync(await ac.GetAsync("/api/v1/inventario/productos/buscar?texto=VIEJO"))).AsArray().Should().BeEmpty();
        // El Administrador no captura pedidos (D-148): la búsqueda del pedido es 403 para él.
        (await admin.GetAsync("/api/v1/inventario/productos/buscar?texto=VIEJO")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await JsonAsync(await admin.GetAsync($"/api/v1/inventario/productos/{cat.Inactivo}")))["activo"]!.GetValue<bool>().Should().BeFalse();
        (await admin.GetAsync("/api/v1/inventario/productos/987654")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
