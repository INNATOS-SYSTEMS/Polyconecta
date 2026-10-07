namespace PolyConecta.Application.Plataforma.Folios;

/// <summary>
/// Folios de todos los documentos (04 §1). Debe llamarse dentro de la transacción del caso de uso:
/// el bloqueo de la fila se libera al confirmar, así dos peticiones nunca obtienen el mismo folio.
/// </summary>
public interface IReferenceSequenceService
{
    /// <param name="documentType">Tipo configurado en plt.reference_sequence. Uno inexistente es un error.</param>
    /// <param name="contexto">Valores para los marcadores del prefijo, como {planta}.</param>
    Task<string> NextAsync(string documentType, IReadOnlyDictionary<string, string>? contexto = null, CancellationToken cancellationToken = default);
}

/// <summary>Se pidió un folio de un tipo de documento que no tiene secuencia configurada.</summary>
public sealed class SecuenciaNoConfiguradaException(string documentType)
    : InvalidOperationException($"No hay secuencia de folios configurada para el tipo de documento '{documentType}'.")
{
    public string DocumentType { get; } = documentType;
}
