using System.Globalization;

namespace PolyConecta.Domain.Plataforma;

public enum ResetRule
{
    Nunca,
    Anual,
    Mensual,
}

/// <summary>
/// Secuencia de folios de un tipo de documento (04 §1, numeración centralizada). Todo folio
/// visible sale de aquí; nunca se arma concatenando cadenas en otro lado.
/// </summary>
public sealed class ReferenceSequence
{
    public string DocumentType { get; private set; } = string.Empty;

    /// <summary>Puede llevar {yyyy}, {yy}, {MM} o cualquier marcador que llegue en el contexto, como {planta}.</summary>
    public string Prefix { get; private set; } = string.Empty;

    public int Padding { get; private set; }

    public long NextNumber { get; private set; } = 1;

    public ResetRule ResetRule { get; private set; }

    /// <summary>2026 o 2026-10. Si cambia el periodo, el consecutivo vuelve a 1.</summary>
    public string? CurrentPeriod { get; private set; }

    private ReferenceSequence() { }

    public ReferenceSequence(string documentType, string prefix, int padding, ResetRule resetRule)
    {
        if (string.IsNullOrWhiteSpace(documentType)) throw new ArgumentException("Falta el tipo de documento.", nameof(documentType));
        if (padding < 1) throw new ArgumentOutOfRangeException(nameof(padding), "El relleno debe ser de al menos 1 dígito.");
        DocumentType = documentType;
        Prefix = prefix;
        Padding = padding;
        ResetRule = resetRule;
    }

    /// <summary>Devuelve el siguiente folio y avanza el consecutivo.</summary>
    public string Siguiente(DateTimeOffset ahora, IReadOnlyDictionary<string, string>? contexto = null)
    {
        var periodo = ResetRule switch
        {
            ResetRule.Anual => ahora.ToString("yyyy", CultureInfo.InvariantCulture),
            ResetRule.Mensual => ahora.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            _ => null,
        };
        if (periodo != CurrentPeriod)
        {
            CurrentPeriod = periodo;
            NextNumber = 1;
        }

        var prefijo = Prefix
            .Replace("{yyyy}", ahora.ToString("yyyy", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{yy}", ahora.ToString("yy", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{MM}", ahora.ToString("MM", CultureInfo.InvariantCulture), StringComparison.Ordinal);
        foreach (var (clave, valor) in contexto ?? new Dictionary<string, string>())
            prefijo = prefijo.Replace("{" + clave + "}", valor, StringComparison.Ordinal);

        var folio = prefijo + NextNumber.ToString(CultureInfo.InvariantCulture).PadLeft(Padding, '0');
        NextNumber++;
        return folio;
    }
}
