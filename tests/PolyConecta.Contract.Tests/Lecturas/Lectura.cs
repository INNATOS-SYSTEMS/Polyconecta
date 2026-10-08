using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using PolyConecta.Contract.Tests.Infraestructura;

namespace PolyConecta.Contract.Tests.Lecturas;

/// <summary>Ayudas de las pruebas de lecturas (§6): recorrer páginas, revisar `snake_case` y los límites.</summary>
internal static partial class Lectura
{
    /// <summary>Tamaño de página con el que se recorre: chico en el simulador para forzar varias páginas, el máximo en el real.</summary>
    public static int Pagina(Bridge bridge) => bridge.EsSimulado ? 3 : 500;

    /// <summary>Recorre el catálogo con `limit` y `cursor` hasta agotarlo y revisa cada página.</summary>
    public static async Task<IReadOnlyList<JsonObject>> RecorrerAsync(Bridge bridge, string ruta, int limit, int maximoPaginas = 5000)
    {
        var todos = new List<JsonObject>();
        string? cursor = null;
        var paginas = 0;
        do
        {
            var url = $"{ruta}?limit={limit}" + (cursor is null ? "" : $"&cursor={Uri.EscapeDataString(cursor)}");
            var pagina = (await bridge.Http.GetFromJsonAsync<JsonObject>(url))!;
            var items = pagina["items"]!.AsArray();
            items.Count.Should().BeLessThanOrEqualTo(limit, "una página no pasa de limit");
            pagina.ContainsKey("next_cursor").Should().BeTrue("toda página trae next_cursor, null en la última");
            todos.AddRange(items.Select(i => i!.AsObject()));
            cursor = pagina["next_cursor"]?.GetValue<string>();
            if (cursor is not null) items.Should().HaveCount(limit, "solo la última página puede venir incompleta");
            (++paginas).Should().BeLessThan(maximoPaginas, "el cursor debe avanzar hasta agotar el catálogo");
        }
        while (cursor is not null);
        return todos;
    }

    /// <summary>Todos los nombres de propiedad, a cualquier profundidad, están en `snake_case`.</summary>
    public static void EsSnakeCase(JsonNode? nodo)
    {
        switch (nodo)
        {
            case JsonObject o:
                foreach (var (nombre, valor) in o)
                {
                    SnakeCase().IsMatch(nombre).Should().BeTrue($"la propiedad '{nombre}' debe estar en snake_case");
                    EsSnakeCase(valor);
                }
                break;
            case JsonArray a:
                foreach (var i in a) EsSnakeCase(i);
                break;
        }
    }

    /// <summary>`limit` fuera de 1 a 500 responde 400 con CARGA_INVALIDA.</summary>
    public static async Task LimiteFueraDeRangoSeRechazaAsync(Bridge bridge, string ruta)
    {
        foreach (var limit in new[] { "0", "-1", "501" })
        {
            var r = await bridge.Http.GetAsync($"{ruta}?limit={limit}");
            r.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"limit={limit}");
            var cuerpo = (await r.Content.ReadFromJsonAsync<JsonObject>())!;
            cuerpo["code"]!.GetValue<string>().Should().Be("CARGA_INVALIDA");
        }
        (await bridge.Http.GetAsync($"{ruta}?limit=1")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await bridge.Http.GetAsync($"{ruta}?limit=500")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>El bridge habla la versión 1.1 del contrato o una posterior (la de /health).</summary>
    public static async Task<bool> Es11Async(Bridge bridge)
    {
        var salud = (await bridge.Http.GetFromJsonAsync<JsonObject>("/health"))!;
        var version = salud["contract_version"]?.GetValue<string>() ?? "1.0";
        return Version.Parse(version) >= new Version(1, 1);
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(_[a-z0-9]+)*$")]
    private static partial Regex SnakeCase();
}
