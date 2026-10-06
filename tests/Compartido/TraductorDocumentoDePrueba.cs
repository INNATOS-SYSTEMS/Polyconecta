using PolyConecta.Infrastructure.Erp;

namespace PolyConecta.Tests.Compartido;

/// <summary>
/// El documento de prueba se envía como una recolección (TRASPASO, variante RECOLECCION) de un
/// producto sin lote del catálogo del simulador, para recorrer el ciclo completo (SC-004).
/// </summary>
public sealed class TraductorDocumentoDePrueba : CommandPayloadTranslator<DocumentoDePrueba>
{
    public override string CommandType => "TRASPASO";

    public override string? Variante(DocumentoDePrueba documento) => "RECOLECCION";

    public override object Carga(DocumentoDePrueba documento) => new Dictionary<string, object?>
    {
        ["fecha"] = "2026-10-12",
        ["referencia_negocio"] = documento.Folio,
        ["almacen_origen"] = documento.Origen,
        ["almacen_destino"] = documento.Destino,
        ["lineas"] = new[]
        {
            new Dictionary<string, object?>
            {
                ["producto"] = documento.Producto,
                ["cantidad"] = documento.Cantidad,
                ["unidad"] = documento.Unidad,
                ["lotes"] = Array.Empty<object>(),
            },
        },
    };

    public override IEnumerable<string> Llaves(DocumentoDePrueba documento) =>
        [Producto(documento.Producto), Almacen(documento.Origen), Almacen(documento.Destino)];
}
