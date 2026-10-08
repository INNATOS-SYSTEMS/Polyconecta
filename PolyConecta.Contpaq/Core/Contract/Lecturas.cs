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
        /// <summary>Desde 1.1: CIDPRODUCTO.</summary>
        [JsonPropertyName("id_erp")] public long IdErp { get; set; }
        /// <summary>Desde 1.1: valor de "TIPO DE PRODUCTOS", o null si el producto no lo tiene.</summary>
        [JsonPropertyName("clasificacion")] public ClasificacionContrato? Clasificacion { get; set; }
    }

    public sealed class ClasificacionContrato
    {
        [JsonPropertyName("codigo")] public string Codigo { get; set; } = string.Empty;
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = string.Empty;
    }

    public sealed class ClienteContrato
    {
        [JsonPropertyName("codigo")] public string Codigo { get; set; } = string.Empty;
        [JsonPropertyName("razon_social")] public string RazonSocial { get; set; } = string.Empty;
        [JsonPropertyName("rfc")] public string? Rfc { get; set; }
        /// <summary>Desde 1.1: CIDCLIENTEPROVEEDOR.</summary>
        [JsonPropertyName("id_erp")] public long IdErp { get; set; }
        [JsonPropertyName("activo")] public bool Activo { get; set; } = true;
        /// <summary>Desde 1.1: ISO de la moneda del cliente (CIDMONEDA); null si el bridge no la tiene configurada.</summary>
        [JsonPropertyName("moneda")] public string? Moneda { get; set; }
        /// <summary>Desde 1.1: un domicilio fiscal y N de envío.</summary>
        [JsonPropertyName("domicilios")] public List<DomicilioContrato> Domicilios { get; set; } = new();
    }

    public sealed class DomicilioContrato
    {
        [JsonPropertyName("id_erp")] public long IdErp { get; set; }
        [JsonPropertyName("tipo")] public string Tipo { get; set; } = "envio";
        [JsonPropertyName("calle")] public string? Calle { get; set; }
        [JsonPropertyName("numero_exterior")] public string? NumeroExterior { get; set; }
        [JsonPropertyName("numero_interior")] public string? NumeroInterior { get; set; }
        [JsonPropertyName("colonia")] public string? Colonia { get; set; }
        [JsonPropertyName("codigo_postal")] public string? CodigoPostal { get; set; }
        [JsonPropertyName("ciudad")] public string? Ciudad { get; set; }
        [JsonPropertyName("municipio")] public string? Municipio { get; set; }
        [JsonPropertyName("estado")] public string? Estado { get; set; }
        [JsonPropertyName("pais")] public string? Pais { get; set; }
        [JsonPropertyName("sucursal")] public string? Sucursal { get; set; }
    }

    /// <summary>Desde 1.1 (D-153): agente de venta de admAgentes.</summary>
    public sealed class AgenteContrato
    {
        [JsonPropertyName("codigo")] public string Codigo { get; set; } = string.Empty;
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = string.Empty;
        [JsonPropertyName("id_erp")] public long IdErp { get; set; }
        /// <summary>venta, venta_cobro o cobro.</summary>
        [JsonPropertyName("tipo")] public string Tipo { get; set; } = "venta";
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
