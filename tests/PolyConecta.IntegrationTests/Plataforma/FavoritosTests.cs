using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.IntegrationTests.Plataforma;

/// <summary>Favoritos por la API (L2-T030, US4 escenario 2, contracts/api-listas.md).</summary>
public class FavoritosTests(SqlServerFixture sql)
{
    private const string Ruta = "/api/v1/plataforma/favoritos/ventas.pedidos";

    private static async Task<(ApiDePrueba Api, HttpClient Ac1, HttpClient Ac2)> LevantarAsync(SqlServerFixture sql)
    {
        var entorno = await Entorno.CrearAsync(sql);
        var api = new ApiDePrueba(entorno);
        await api.CrearUsuarioAsync("ac1", "Atención 1", (GruposIniciales.AtencionClientes, "PIM", false));
        await api.CrearUsuarioAsync("ac2", "Atención 2", (GruposIniciales.AtencionClientes, "PIM", false));
        return (api, await api.ClienteAsync("ac1"), await api.ClienteAsync("ac2"));
    }

    private static object Definicion(params string[] filtros) => new { definicion = new { filtros, agrupacion = new[] { "cliente" } }, porOmision = false };

    private static async Task<JsonArray> ListarAsync(HttpClient c)
    {
        var r = await c.GetAsync(Ruta);
        r.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonNode.Parse(await r.Content.ReadAsStringAsync())!.AsArray();
    }

    [Fact]
    public async Task Un_usuario_no_ve_ni_borra_los_favoritos_de_otro()
    {
        var (api, ac1, ac2) = await LevantarAsync(sql);
        await using var _ = api;

        (await ac1.PutAsJsonAsync($"{Ruta}/Por autorizar", Definicion("Confirmado"))).StatusCode.Should().Be(HttpStatusCode.OK);

        var deAc1 = await ListarAsync(ac1);
        deAc1.Should().HaveCount(1);
        deAc1[0]!["nombre"]!.GetValue<string>().Should().Be("Por autorizar");
        deAc1[0]!["definicion"]!["filtros"]![0]!.GetValue<string>().Should().Be("Confirmado");
        (await ListarAsync(ac2)).Should().BeEmpty();
        (await ac2.DeleteAsync($"{Ruta}/Por autorizar")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ListarAsync(ac1)).Should().HaveCount(1);
    }

    [Fact]
    public async Task Solo_uno_por_omision_por_lista_y_guardar_con_el_mismo_nombre_lo_reemplaza()
    {
        var (api, ac1, _) = await LevantarAsync(sql);
        await using var __ = api;

        await ac1.PutAsJsonAsync($"{Ruta}/Por autorizar", new { definicion = new { filtros = new[] { "Confirmado" } }, porOmision = true });
        await ac1.PutAsJsonAsync($"{Ruta}/Borradores", new { definicion = new { filtros = new[] { "Borrador" } }, porOmision = true });
        (await ac1.PutAsJsonAsync($"{Ruta}/por autorizar", Definicion("Confirmado", "Autorizado"))).StatusCode.Should().Be(HttpStatusCode.OK);

        var lista = await ListarAsync(ac1);
        lista.Should().HaveCount(2);
        var porOmision = lista.Where(f => f!["porOmision"]!.GetValue<bool>()).Select(f => f!["nombre"]!.GetValue<string>());
        porOmision.Should().Equal("Borradores");
        lista.Single(f => f!["nombre"]!.GetValue<string>() == "por autorizar")!["definicion"]!["filtros"]!.AsArray().Should().HaveCount(2);
    }

    [Fact]
    public async Task Borrar_quita_el_favorito_y_una_lista_mal_escrita_responde_400()
    {
        var (api, ac1, _) = await LevantarAsync(sql);
        await using var __ = api;

        await ac1.PutAsJsonAsync($"{Ruta}/Por autorizar", Definicion("Confirmado"));
        (await ac1.DeleteAsync($"{Ruta}/Por autorizar")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ListarAsync(ac1)).Should().BeEmpty();

        (await ac1.GetAsync("/api/v1/plataforma/favoritos/Ventas Pedidos")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ac1.PutAsJsonAsync($"{Ruta}/Sin definición", new { definicion = "texto", porOmision = false })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Sin_sesion_no_hay_favoritos()
    {
        var (api, _, _) = await LevantarAsync(sql);
        await using var __ = api;
        var anonimo = api.CreateClient();

        (await anonimo.GetAsync(Ruta)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
