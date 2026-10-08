using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using PolyConecta.Contract.Tests.Infraestructura;
using Xunit;

namespace PolyConecta.Contract.Tests.Lecturas;

/// <summary>`GET /api/v1/catalogs/clients` (§6): paginación, forma, moneda y domicilios de 1.1.</summary>
public class ClientesTests(Bridge bridge)
{
    private const string Ruta = "/api/v1/catalogs/clients";

    [ContratoFact]
    public async Task Se_pagina_con_limit_y_cursor_hasta_agotar_sin_repetir()
    {
        var clientes = await Lectura.RecorrerAsync(bridge, Ruta, Lectura.Pagina(bridge));
        clientes.Should().NotBeEmpty();
        clientes.Select(c => c["codigo"]!.GetValue<string>()).Should().OnlyHaveUniqueItems();
        if (bridge.EsSimulado) clientes.Should().HaveCount(5);
    }

    [ContratoFact]
    public async Task Cada_cliente_trae_codigo_y_razon_social_en_snake_case()
    {
        var clientes = await Lectura.RecorrerAsync(bridge, Ruta, Lectura.Pagina(bridge));
        foreach (var c in clientes)
        {
            Lectura.EsSnakeCase(c);
            c["codigo"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
            c["razon_social"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
        }
    }

    [ContratoFact]
    public async Task Desde_1_1_trae_id_erp_activo_y_moneda_opcional_en_iso()
    {
        Assert.SkipUnless(await Lectura.Es11Async(bridge), "El bridge habla 1.0: estos campos llegan en 1.1.");
        var clientes = await Lectura.RecorrerAsync(bridge, Ruta, Lectura.Pagina(bridge));
        clientes.Select(c => c["id_erp"]!.GetValue<long>()).Should().OnlyHaveUniqueItems().And.OnlyContain(id => id > 0);
        foreach (var c in clientes) c["activo"]!.GetValueKind().Should().BeOneOf(System.Text.Json.JsonValueKind.True, System.Text.Json.JsonValueKind.False);
        foreach (var moneda in clientes.Select(c => c["moneda"]).Where(m => m is not null))
            moneda!.GetValue<string>().Should().MatchRegex("^[A-Z]{3}$", "la moneda va como código ISO");
    }

    [ContratoFact]
    public async Task Si_trae_domicilios_tiene_exactamente_un_fiscal_y_la_forma_completa()
    {
        Assert.SkipUnless(await Lectura.Es11Async(bridge), "El bridge habla 1.0: los domicilios llegan en 1.1.");
        string[] campos = ["id_erp", "tipo", "calle", "numero_exterior", "numero_interior", "colonia", "codigo_postal", "ciudad", "municipio", "estado", "pais", "sucursal"];
        var clientes = await Lectura.RecorrerAsync(bridge, Ruta, Lectura.Pagina(bridge));
        foreach (var cliente in clientes)
        {
            if (cliente["domicilios"] is not JsonArray domicilios || domicilios.Count == 0) continue;
            domicilios.Count(d => d!["tipo"]!.GetValue<string>() == "fiscal").Should().Be(1,
                $"el cliente {cliente["codigo"]} debe tener un solo domicilio fiscal");
            foreach (var d in domicilios)
            {
                d!["tipo"]!.GetValue<string>().Should().BeOneOf("fiscal", "envio");
                d["id_erp"]!.GetValue<long>().Should().BePositive();
                d.AsObject().Select(p => p.Key).Should().Contain(campos);
            }
        }
    }

    [ContratoFact]
    public async Task El_simulador_trae_variedad_de_moneda_y_domicilios()
    {
        Assert.SkipUnless(bridge.EsSimulado, "El catálogo del bridge real es el de la empresa.");
        var clientes = await Lectura.RecorrerAsync(bridge, Ruta, 500);
        clientes.Select(c => c["moneda"]!.GetValue<string>()).Distinct().Should().BeEquivalentTo(["MXN", "USD"]);
        clientes.Should().Contain(c => c["domicilios"]!.AsArray().Count == 0);
        clientes.Should().Contain(c => c["domicilios"]!.AsArray().Count(d => d!["tipo"]!.GetValue<string>() == "envio") >= 2);
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
