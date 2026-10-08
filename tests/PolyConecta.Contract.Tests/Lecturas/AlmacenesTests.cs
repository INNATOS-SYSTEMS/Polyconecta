using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using PolyConecta.Contract.Tests.Infraestructura;
using Xunit;

namespace PolyConecta.Contract.Tests.Lecturas;

/// <summary>`GET /api/v1/catalogs/warehouses` (§6): sin paginar.</summary>
public class AlmacenesTests(Bridge bridge)
{
    private const string Ruta = "/api/v1/catalogs/warehouses";

    [ContratoFact]
    public async Task Devuelve_todos_los_almacenes_sin_paginar_con_su_id_erp()
    {
        var almacenes = (await bridge.Http.GetFromJsonAsync<JsonArray>(Ruta))!;
        almacenes.Should().NotBeEmpty();
        foreach (var a in almacenes)
        {
            Lectura.EsSnakeCase(a);
            a!["codigo"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
            a["nombre"]!.GetValue<string>().Should().NotBeNull();
            a["id_erp"]!.GetValue<long>().Should().BePositive();
        }
        almacenes.Select(a => a!["codigo"]!.GetValue<string>()).Should().OnlyHaveUniqueItems();
        almacenes.Select(a => a!["id_erp"]!.GetValue<long>()).Should().OnlyHaveUniqueItems();
    }

    [ContratoFact]
    public async Task El_simulador_trae_los_almacenes_de_las_dos_plantas()
    {
        Assert.SkipUnless(bridge.EsSimulado, "El catálogo del bridge real es el de la empresa.");
        var codigos = (await bridge.Http.GetFromJsonAsync<JsonArray>(Ruta))!.Select(a => a!["codigo"]!.GetValue<string>()).ToList();
        codigos.Should().Contain(c => c.EndsWith("-PIM")).And.Contain(c => c.EndsWith("-SC"));
    }
}
