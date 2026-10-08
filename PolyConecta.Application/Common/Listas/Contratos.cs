using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolyConecta.Application.Common.Listas;

/// <summary>
/// Parámetros de consulta de una lista (contracts/api-listas.md, 07 §4.1).
/// </summary>
public sealed record ConsultaLista(
    int Pagina = 0,
    int Tamano = 80,
    IReadOnlyList<OrdenLista>? Orden = null,
    IReadOnlyList<FiltroLista>? Filtros = null,
    IReadOnlyList<string>? Nombrados = null,
    string? Busqueda = null,
    IReadOnlyList<string>? AgruparPor = null,
    IReadOnlyList<GrupoFiltro>? Grupo = null,
    [property: JsonConverter(typeof(FlexibleStringListConverter))]
    IReadOnlyList<string>? Ids = null)
{
    public IReadOnlyList<OrdenLista> OrdenEfectivo => Orden ?? [];
    public IReadOnlyList<FiltroLista> FiltrosEfectivos => Filtros ?? [];
    public IReadOnlyList<string> NombradosEfectivos => Nombrados ?? [];
    public IReadOnlyList<string> AgruparPorEfectivo => AgruparPor ?? [];
    public IReadOnlyList<GrupoFiltro> GrupoEfectivo => Grupo ?? [];
}

public sealed record OrdenLista(string Campo, bool Desc);

public sealed record FiltroLista(string Campo, string Operador, object? Valor)
{
    public string? ComoTexto() => Valor switch
    {
        null => null,
        JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
        JsonElement je when je.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => je.ToString(),
        string s => s,
        _ => Valor.ToString(),
    };

    public (object? Desde, object? Hasta) ComoRango()
    {
        if (Valor is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            var items = je.EnumerateArray().ToList();
            var d = items.Count > 0 ? ExtraerPrimitivo(items[0]) : null;
            var h = items.Count > 1 ? ExtraerPrimitivo(items[1]) : null;
            return (d, h);
        }
        if (Valor is System.Collections.IEnumerable en and not string)
        {
            var list = en.Cast<object?>().ToList();
            var d = list.Count > 0 ? list[0] : null;
            var h = list.Count > 1 ? list[1] : null;
            return (d, h);
        }
        return (null, null);
    }

    public IReadOnlyList<object?> ComoColeccion()
    {
        if (Valor is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            return je.EnumerateArray().Select(ExtraerPrimitivo).ToList();
        }
        if (Valor is System.Collections.IEnumerable en and not string)
        {
            return en.Cast<object?>().ToList();
        }
        return [Valor];
    }

    private static object? ExtraerPrimitivo(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDecimal(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => el.ToString(),
    };
}

public sealed record GrupoFiltro(string Campo, string Valor);

public sealed record GrupoLista(
    string Campo,
    string Valor,
    string Etiqueta,
    int Cantidad,
    IReadOnlyDictionary<string, decimal> Totales,
    IReadOnlyDictionary<string, string>? Textos = null);

public sealed record ResultadoLista<T>(
    IReadOnlyList<T> Filas,
    IReadOnlyList<GrupoLista>? Grupos,
    int Total,
    IReadOnlyDictionary<string, decimal> Totales);

public sealed record ResultadoConjunto<T>(
    bool Completo,
    int Total,
    DateTimeOffset? Generado = null,
    IReadOnlyList<T>? Filas = null);

// --- DTOs de descripción de vista (GET …/vista) ---

public sealed record ColumnaVistaDto(
    string Campo,
    string Etiqueta,
    bool Ordenable,
    bool Sumable,
    string? Tipo = null);

public sealed record CampoBuscableDto(string Campo, string Etiqueta);

public sealed record FiltroNombradoDto(string Nombre, string Campo);

public sealed record AgrupacionVistaDto(string Etiqueta, string Campo);

public sealed record VistaDeBusquedaDto(
    string Lista,
    IReadOnlyList<ColumnaVistaDto> Columnas,
    IReadOnlyList<CampoBuscableDto> Campos,
    IReadOnlyList<FiltroNombradoDto> Filtros,
    IReadOnlyList<AgrupacionVistaDto> Agrupaciones,
    IReadOnlyList<string> AgrupacionesPorDefecto);

/// <summary>Convierte arreglos JSON de números o strings en lista de strings.</summary>
public sealed class FlexibleStringListConverter : JsonConverter<IReadOnlyList<string>>
{
    public override IReadOnlyList<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Se esperaba un arreglo.");
        var list = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (s is not null) list.Add(s);
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                list.Add(reader.GetInt64().ToString());
            }
        }
        return list;
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyList<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value) writer.WriteStringValue(item);
        writer.WriteEndArray();
    }
}
