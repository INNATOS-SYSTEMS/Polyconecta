using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Contpaq.Bridge.Core.Contract
{
    /// <summary>Formas de las lecturas del contrato (§6).</summary>
    public sealed class ProductoContrato
    {
        [JsonPropertyName("codigo")] public string Codigo { get; set; } = string.Empty;
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = string.Empty;
        [JsonPropertyName("unidad_base")] public string UnidadBase { get; set; } = string.Empty;
        [JsonPropertyName("lleva_lote")] public bool LlevaLote { get; set; }
        [JsonPropertyName("activo")] public bool Activo { get; set; } = true;
    }

    public sealed class ClienteContrato
    {
        [JsonPropertyName("codigo")] public string Codigo { get; set; } = string.Empty;
        [JsonPropertyName("razon_social")] public string RazonSocial { get; set; } = string.Empty;
        [JsonPropertyName("rfc")] public string? Rfc { get; set; }
    }

    public sealed class AlmacenContrato
    {
        [JsonPropertyName("codigo")] public string Codigo { get; set; } = string.Empty;
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = string.Empty;
        [JsonPropertyName("id_erp")] public long IdErp { get; set; }
    }

    public sealed class ExistenciaContrato
    {
        [JsonPropertyName("producto")] public string Producto { get; set; } = string.Empty;
        [JsonPropertyName("almacen")] public string Almacen { get; set; } = string.Empty;
        [JsonPropertyName("unidad")] public string Unidad { get; set; } = string.Empty;
        [JsonPropertyName("cantidad")] public decimal Cantidad { get; set; }
        [JsonPropertyName("lote")] public string? Lote { get; set; }
    }

    public sealed class RecepcionCompraContrato
    {
        [JsonPropertyName("folio")] public string Folio { get; set; } = string.Empty;
        [JsonPropertyName("fecha")] public string Fecha { get; set; } = string.Empty;
        [JsonPropertyName("proveedor")] public string? Proveedor { get; set; }
        [JsonPropertyName("almacen")] public string Almacen { get; set; } = string.Empty;
        [JsonPropertyName("lineas")] public List<LineaConLotes> Lineas { get; set; } = new();
    }

    public sealed class Pagina<T>
    {
        [JsonPropertyName("items")] public IReadOnlyList<T> Items { get; init; } = new List<T>();
        [JsonPropertyName("next_cursor")] public string? NextCursor { get; init; }
    }
}
