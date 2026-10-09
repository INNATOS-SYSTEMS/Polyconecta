using System.Globalization;

namespace PolyConecta.Application.Plataforma.Chatter;

/// <summary>
/// Redacta los cambios que se anotan en la bitácora de un registro (D-157): "Nombre: A → B",
/// "Agregó Comercial · PIM". Un valor vacío se escribe "—".
/// </summary>
public static class Bitacora
{
    public static string Valor(object? v) => v switch
    {
        null => "—",
        string s when string.IsNullOrWhiteSpace(s) => "—",
        string s => s.Trim(),
        bool b => b ? "Sí" : "No",
        decimal d => d.ToString("0.####", CultureInfo.InvariantCulture),
        double d => d.ToString("0.####", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => v.ToString() ?? "—",
    };

    /// <summary>"Etiqueta: antes → después", o null si no cambió.</summary>
    public static string? Cambio(string etiqueta, object? antes, object? despues)
    {
        var a = Valor(antes);
        var d = Valor(despues);
        return a == d ? null : $"{etiqueta}: {a} → {d}";
    }

    /// <summary>"Agregó …" y "Quitó …" entre dos conjuntos de elementos ya redactados.</summary>
    public static IEnumerable<string> Conjunto(IEnumerable<string> antes, IEnumerable<string> despues)
    {
        var a = antes.ToHashSet();
        var d = despues.ToHashSet();
        foreach (var x in d.Where(x => !a.Contains(x)).Order()) yield return $"Agregó {x}";
        foreach (var x in a.Where(x => !d.Contains(x)).Order()) yield return $"Quitó {x}";
    }

    /// <summary>Compara las propiedades públicas de dos registros del mismo tipo y redacta cada diferencia.</summary>
    public static IEnumerable<string> Propiedades<T>(T? antes, T? despues, IReadOnlyDictionary<string, string> etiquetas, string? prefijo = null)
        where T : class
    {
        foreach (var p in typeof(T).GetProperties().Where(p => p.CanRead && p.GetIndexParameters().Length == 0))
        {
            var etiqueta = etiquetas.GetValueOrDefault(p.Name, p.Name);
            var c = Cambio(prefijo is null ? etiqueta : $"{prefijo} · {etiqueta}", antes is null ? null : p.GetValue(antes), despues is null ? null : p.GetValue(despues));
            if (c is not null) yield return c;
        }
    }
}
