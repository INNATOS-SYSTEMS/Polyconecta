using AwesomeAssertions;
using PolyConecta.Contract.Tests.Infraestructura;
using Xunit;

namespace PolyConecta.Contract.Tests.Lecturas;

/// <summary>`GET /api/v1/catalogs/agents` (§6, lectura nueva de 1.1, D-153).</summary>
public class AgentesTests(Bridge bridge)
{
    private const string Ruta = "/api/v1/catalogs/agents";

    [ContratoFact]
    public async Task Se_pagina_con_limit_y_cursor_hasta_agotar_sin_repetir()
    {
        Assert.SkipUnless(await Lectura.Es11Async(bridge), "El bridge habla 1.0: los agentes llegan en 1.1.");
        var agentes = await Lectura.RecorrerAsync(bridge, Ruta, bridge.EsSimulado ? 1 : 500);
        agentes.Select(a => a["codigo"]!.GetValue<string>()).Should().OnlyHaveUniqueItems();
        if (bridge.EsSimulado) agentes.Should().HaveCount(4);
    }

    [ContratoFact]
    public async Task Cada_agente_trae_codigo_nombre_id_erp_y_tipo()
    {
        Assert.SkipUnless(await Lectura.Es11Async(bridge), "El bridge habla 1.0: los agentes llegan en 1.1.");
        var agentes = await Lectura.RecorrerAsync(bridge, Ruta, 500);
        foreach (var a in agentes)
        {
            Lectura.EsSnakeCase(a);
            a["codigo"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
            a["nombre"]!.GetValue<string>().Should().NotBeNull();
            a["id_erp"]!.GetValue<long>().Should().BePositive();
            a["tipo"]!.GetValue<string>().Should().BeOneOf("venta", "venta_cobro", "cobro");
        }
        agentes.Select(a => a["id_erp"]!.GetValue<long>()).Should().OnlyHaveUniqueItems();
    }

    [ContratoFact]
    public async Task El_simulador_trae_agentes_de_venta_y_de_cobro()
    {
        Assert.SkipUnless(bridge.EsSimulado, "El catálogo del bridge real es el de la empresa.");
        var agentes = await Lectura.RecorrerAsync(bridge, Ruta, 500);
        agentes.Select(a => a["tipo"]!.GetValue<string>()).Should().Contain(["venta", "cobro"]);
    }

    [ContratoFact]
    public async Task Un_limite_fuera_de_rango_se_rechaza()
    {
        Assert.SkipUnless(await Lectura.Es11Async(bridge), "El bridge habla 1.0: los agentes llegan en 1.1.");
        await Lectura.LimiteFueraDeRangoSeRechazaAsync(bridge, Ruta);
    }
}
