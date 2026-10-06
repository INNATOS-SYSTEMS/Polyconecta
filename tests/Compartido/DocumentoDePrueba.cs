using PolyConecta.Domain.Common;

namespace PolyConecta.Tests.Compartido;

public enum EstadoPrueba
{
    Borrador,
    Confirmado,
    Cancelado,
}

/// <summary>
/// Documento que solo existe en las pruebas (spec 002, SC-004): hereda los mixins, tiene estados
/// cerrados y escribe en CONTPAQi al confirmarse.
/// </summary>
public sealed class DocumentoDePrueba : DocumentoConEstado<EstadoPrueba>, ISyncedDocument
{
    public string Folio { get; private set; } = string.Empty;

    public string Producto { get; private set; } = string.Empty;

    public decimal Cantidad { get; private set; }

    public string Unidad { get; private set; } = string.Empty;

    public string Origen { get; private set; } = string.Empty;

    public string Destino { get; private set; } = string.Empty;

    public SyncState Sync { get; private set; } = new();

    private DocumentoDePrueba() : base(EstadoPrueba.Borrador) { }

    public DocumentoDePrueba(string folio, string producto, decimal cantidad, string unidad, string origen = "MP-PIM", string destino = "WIP-PIM")
        : this()
    {
        Folio = folio;
        Producto = producto;
        Cantidad = cantidad;
        Unidad = unidad;
        Origen = origen;
        Destino = destino;
    }

    public void Confirmar() => Transicionar(EstadoPrueba.Confirmado, "Confirmado en prueba", EstadoPrueba.Borrador);

    public void Cancelar() => Transicionar(EstadoPrueba.Cancelado, null, EstadoPrueba.Borrador, EstadoPrueba.Confirmado);
}
