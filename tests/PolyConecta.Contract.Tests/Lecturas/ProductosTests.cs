using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using PolyConecta.Contract.Tests.Infraestructura;
using Xunit;

namespace PolyConecta.Contract.Tests.Lecturas;

/// <summary>`GET /api/v1/catalogs/products` (§6): paginación, forma y los campos de 1.1.</summary>
public class ProductosTests(Bridge bridge)
{
    private const string Ruta = "/api/v1/catalogs/products";

    [ContratoFact]
    public async Task Se_pagina_con_limit_y_cursor_hasta_agotar_sin_repetir()
    {
        var productos = await Lectura.RecorrerAsync(bridge, Ruta, Lectura.Pagina(bridge));
        productos.Should().NotBeEmpty();
        productos.Select(p => p["codigo"]!.GetValue<string>()).Should().OnlyHaveUniqueItems();
        if (bridge.EsSimulado) productos.Should().HaveCount(20);
    }

    [ContratoFact]
    public async Task Cada_producto_trae_unidad_base_lote_y_estado_en_snake_case()
    {
        var productos = await Lectura.RecorrerAsync(bridge, Ruta, Lectura.Pagina(bridge));
        foreach (var p in productos)
        {
            Lectura.EsSnakeCase(p);
            p["codigo"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
            p["nombre"]!.GetValue<string>().Should().NotBeNull();
            p["unidad_base"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
            p["lleva_lote"]!.GetValue<bool>();
            p["activo"]!.GetValue<bool>();
        }
    }

    [ContratoFact]
    public async Task Desde_1_1_trae_id_erp_y_clasificacion_opcional()
    {
        Assert.SkipUnless(await Lectura.Es11Async(bridge), "El bridge habla 1.0: id_erp y clasificacion llegan en 1.1.");
        var productos = await Lectura.RecorrerAsync(bridge, Ruta, Lectura.Pagina(bridge));
        productos.Select(p => p["id_erp"]!.GetValue<long>()).Should().OnlyHaveUniqueItems().And.OnlyContain(id => id > 0);
        foreach (var clasificacion in productos.Select(p => p["clasificacion"]).Where(c => c is not null))
        {
            clasificacion!["codigo"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
            clasificacion["nombre"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
        }
    }

    [ContratoFact]
    public async Task El_simulador_trae_variedad_para_probar_la_sincronizacion()
    {
        Assert.SkipUnless(bridge.EsSimulado, "El catálogo del bridge real es el de la empresa.");
        var productos = await Lectura.RecorrerAsync(bridge, Ruta, 500);
        productos.Select(p => p["unidad_base"]!.GetValue<string>()).Distinct().Should().BeEquivalentTo(["KG", "PZA", "MIL"]);
        productos.Count(p => !p["activo"]!.GetValue<bool>()).Should().Be(2);
        productos.Should().Contain(p => p["lleva_lote"]!.GetValue<bool>()).And.Contain(p => !p["lleva_lote"]!.GetValue<bool>());
        productos.Should().Contain(p => p["clasificacion"] != null);
    }

    [ContratoFact]
    public async Task La_busqueda_filtra_por_codigo_o_nombre()
    {
        Assert.SkipUnless(bridge.EsSimulado, "Los códigos de la prueba son los del simulador.");
        var respuesta = (await bridge.Http.GetFromJsonAsync<JsonObject>($"{Ruta}?search=PEBD&limit=10"))!;
        respuesta["items"]!.AsArray().Select(i => i!["codigo"]!.GetValue<string>()).Should().Equal("PEBD-001");
    }

    [ContratoFact]
    public async Task Un_limite_fuera_de_rango_se_rechaza() => await Lectura.LimiteFueraDeRangoSeRechazaAsync(bridge, Ruta);

    [ContratoFact]
    public async Task Modified_since_responde_501_porque_esta_obsoleto()
    {
        var r = await bridge.Http.GetAsync($"{Ruta}?modified_since=2026-01-01T00:00:00Z");
        r.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }
}
