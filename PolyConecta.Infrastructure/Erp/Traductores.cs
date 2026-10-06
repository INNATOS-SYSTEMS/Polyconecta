using PolyConecta.Domain.Common;

namespace PolyConecta.Infrastructure.Erp;

/// <summary>
/// Traduce un documento de PolyConecta a un comando del contrato bridge-v1 (D-122): comando,
/// variante, carga y las llaves de bloqueo (producto y almacén) que usa el despachador (D-95).
/// </summary>
public interface ICommandPayloadTranslator
{
    Type TipoDocumento { get; }

    string CommandType { get; }

    string? Variante(object documento);

    object Carga(object documento);

    IEnumerable<string> Llaves(object documento);
}

public abstract class CommandPayloadTranslator<TDocumento> : ICommandPayloadTranslator
    where TDocumento : ISyncedDocument
{
    public Type TipoDocumento => typeof(TDocumento);

    public abstract string CommandType { get; }

    public virtual string? Variante(TDocumento documento) => null;

    public abstract object Carga(TDocumento documento);

    public abstract IEnumerable<string> Llaves(TDocumento documento);

    string? ICommandPayloadTranslator.Variante(object documento) => Variante((TDocumento)documento);

    object ICommandPayloadTranslator.Carga(object documento) => Carga((TDocumento)documento);

    IEnumerable<string> ICommandPayloadTranslator.Llaves(object documento) => Llaves((TDocumento)documento);

    protected static string Producto(string codigo) => $"producto:{codigo}";

    protected static string Almacen(string codigo) => $"almacen:{codigo}";
}
