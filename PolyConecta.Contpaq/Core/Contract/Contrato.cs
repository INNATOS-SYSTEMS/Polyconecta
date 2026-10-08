using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Contpaq.Bridge.Core.Contract
{
    /// <summary>Contrato bridge-v1 (docs/contratos/bridge-v1.md). El documento manda si hay diferencias.</summary>
    public static class Contrato
    {
        public const string VersionMayor = "1";
        public const string VersionActual = "1.1";
        public const string ClientApp = "polyconecta";

        public static readonly JsonSerializerOptions Json = new()
        {
            PropertyNameCaseInsensitive = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };
    }

    public static class Comandos
    {
        public const string AltaAlmacen = "ALTA_ALMACEN";
        public const string Traspaso = "TRASPASO";
        public const string AltaPedido = "ALTA_PEDIDO";
        public const string CierreProduccion = "CIERRE_PRODUCCION";
        public const string Remision = "REMISION";

        public static readonly IReadOnlyList<string> Todos = new[] { AltaAlmacen, Traspaso, AltaPedido, CierreProduccion, Remision };

        public static readonly IReadOnlyList<string> VariantesTraspaso = new[]
        {
            "RECOLECCION", "DEVOLUCION", "CUARENTENA", "LIBERACION", "TRASLADO_SALIDA", "TRASLADO_RECEPCION",
        };
    }

    /// <summary>Estados de la transacción (§3).</summary>
    public static class Estados
    {
        public const string Pending = "PENDING";
        public const string Processing = "PROCESSING";
        public const string Confirmed = "CONFIRMED";
        public const string Failed = "FAILED";
        public const string DeadLetter = "DEAD_LETTER";

        public static bool EsTerminal(string estado) => estado is Confirmed or Failed or DeadLetter;
    }

    /// <summary>Catálogo de errores (§4). El código es estable; el mensaje puede cambiar.</summary>
    public static class CodigosError
    {
        public const string CargaInvalida = "CARGA_INVALIDA";
        public const string VersionNoSoportada = "VERSION_NO_SOPORTADA";
        public const string ProductoNoExiste = "PRODUCTO_NO_EXISTE";
        public const string ProductoInactivo = "PRODUCTO_INACTIVO";
        public const string AlmacenNoExiste = "ALMACEN_NO_EXISTE";
        public const string AlmacenYaExiste = "ALMACEN_YA_EXISTE";
        public const string ClienteNoExiste = "CLIENTE_NO_EXISTE";
        public const string AgenteNoExiste = "AGENTE_NO_EXISTE";
        public const string MonedaNoSoportada = "MONEDA_NO_SOPORTADA";
        public const string UnidadNoAdmitida = "UNIDAD_NO_ADMITIDA";
        public const string VarianteSinConcepto = "VARIANTE_SIN_CONCEPTO";
        public const string LotesNoCuadran = "LOTES_NO_CUADRAN";
        public const string ExistenciaInsuficiente = "EXISTENCIA_INSUFICIENTE";
        public const string ExistenciaInsuficienteLote = "EXISTENCIA_INSUFICIENTE_LOTE";
        public const string VerificacionFallida = "VERIFICACION_FALLIDA";
        public const string SdkTimeout = "SDK_TIMEOUT";
        public const string SdkSesion = "SDK_SESION";
        public const string SdkError = "SDK_ERROR";
        /// <summary>La reconciliación encontró más de un documento posible por encima de la marca (D-131): revisión manual.</summary>
        public const string ReconciliacionAmbigua = "RECONCILIACION_AMBIGUA";

        /// <summary>Solo SDK_TIMEOUT y SDK_SESION se reintentan (§4).</summary>
        public static bool EsReintentable(string codigo) => codigo is SdkTimeout or SdkSesion;
    }

    public sealed class ErrorContrato
    {
        [JsonPropertyName("code")] public string Code { get; init; } = CodigosError.SdkError;
        [JsonPropertyName("retryable")] public bool Retryable { get; init; }
        [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
        [JsonPropertyName("detail")] public Dictionary<string, object?> Detail { get; init; } = new();

        public static ErrorContrato De(string codigo, string mensaje, Dictionary<string, object?>? detalle = null) => new()
        {
            Code = codigo,
            Retryable = CodigosError.EsReintentable(codigo),
            Message = mensaje,
            Detail = detalle ?? new(),
        };
    }

    /// <summary>Sobre del comando (§2). La carga se lee según command_type.</summary>
    public sealed class ComandoRequest
    {
        [JsonPropertyName("contract_version")] public string? ContractVersion { get; set; }
        [JsonPropertyName("command_type")] public string? CommandType { get; set; }
        [JsonPropertyName("variant")] public string? Variant { get; set; }
        [JsonPropertyName("idempotency_key")] public string? IdempotencyKey { get; set; }
        [JsonPropertyName("correlation_id")] public string? CorrelationId { get; set; }
        [JsonPropertyName("client_app_id")] public string? ClientAppId { get; set; }
        [JsonPropertyName("callback_url")] public string? CallbackUrl { get; set; }
        [JsonPropertyName("payload")] public JsonElement Payload { get; set; }
    }

    public sealed class Lote
    {
        [JsonPropertyName("numero")] public string Numero { get; set; } = string.Empty;
        [JsonPropertyName("cantidad")] public decimal Cantidad { get; set; }
    }

    public class LineaConLotes
    {
        [JsonPropertyName("producto")] public string Producto { get; set; } = string.Empty;
        [JsonPropertyName("cantidad")] public decimal Cantidad { get; set; }
        [JsonPropertyName("unidad")] public string Unidad { get; set; } = string.Empty;
        [JsonPropertyName("lotes")] public List<Lote>? Lotes { get; set; }
    }

    public sealed class LineaConDestino : LineaConLotes
    {
        [JsonPropertyName("almacen_destino")] public string AlmacenDestino { get; set; } = string.Empty;
    }

    /// <summary>Línea de la remisión: el precio es opcional en 1.0 y se decide antes de F6 (P-26).</summary>
    public sealed class LineaRemision : LineaConLotes
    {
        [JsonPropertyName("precio")] public decimal? Precio { get; set; }
    }

    public sealed class LineaPedido
    {
        [JsonPropertyName("producto")] public string Producto { get; set; } = string.Empty;
        [JsonPropertyName("cantidad")] public decimal Cantidad { get; set; }
        [JsonPropertyName("unidad")] public string Unidad { get; set; } = string.Empty;
        [JsonPropertyName("precio")] public decimal? Precio { get; set; }
    }

    public interface ICarga
    {
        string? Fecha { get; }
    }

    public sealed class CargaTraspaso : ICarga
    {
        [JsonPropertyName("fecha")] public string? Fecha { get; set; }
        [JsonPropertyName("referencia_negocio")] public string? ReferenciaNegocio { get; set; }
        [JsonPropertyName("almacen_origen")] public string AlmacenOrigen { get; set; } = string.Empty;
        [JsonPropertyName("almacen_destino")] public string AlmacenDestino { get; set; } = string.Empty;
        [JsonPropertyName("lineas")] public List<LineaConLotes>? Lineas { get; set; }
    }

    public sealed class CargaAltaPedido : ICarga
    {
        [JsonPropertyName("fecha")] public string? Fecha { get; set; }
        [JsonPropertyName("referencia_negocio")] public string? ReferenciaNegocio { get; set; }
        [JsonPropertyName("cliente")] public string Cliente { get; set; } = string.Empty;
        [JsonPropertyName("orden_compra_cliente")] public string? OrdenCompraCliente { get; set; }
        /// <summary>Desde 1.1 (D-153): código del agente de CONTPAQi (CCODIGOAGENTE). Opcional.</summary>
        [JsonPropertyName("agente")] public string? Agente { get; set; }
        [JsonPropertyName("moneda")] public string Moneda { get; set; } = string.Empty;
        [JsonPropertyName("tipo_cambio")] public decimal? TipoCambio { get; set; }
        [JsonPropertyName("lineas")] public List<LineaPedido>? Lineas { get; set; }
    }

    public sealed class CargaAltaAlmacen : ICarga
    {
        [JsonPropertyName("codigo")] public string Codigo { get; set; } = string.Empty;
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = string.Empty;
        [JsonPropertyName("fecha_alta")] public string? FechaAlta { get; set; }
        [JsonIgnore] public string? Fecha => FechaAlta;
    }

    public sealed class CargaCierreProduccion : ICarga
    {
        [JsonPropertyName("fecha")] public string? Fecha { get; set; }
        [JsonPropertyName("referencia_negocio")] public string? ReferenciaNegocio { get; set; }
        [JsonPropertyName("almacen_wip")] public string AlmacenWip { get; set; } = string.Empty;
        [JsonPropertyName("consumos")] public List<LineaConLotes>? Consumos { get; set; }
        [JsonPropertyName("entradas")] public List<LineaConDestino>? Entradas { get; set; }
        [JsonPropertyName("subproductos")] public List<LineaConDestino>? Subproductos { get; set; }
    }

    public sealed class CargaRemision : ICarga
    {
        [JsonPropertyName("fecha")] public string? Fecha { get; set; }
        [JsonPropertyName("referencia_negocio")] public string? ReferenciaNegocio { get; set; }
        [JsonPropertyName("cliente")] public string Cliente { get; set; } = string.Empty;
        [JsonPropertyName("almacen")] public string Almacen { get; set; } = string.Empty;
        [JsonPropertyName("pedido_erp")] public string? PedidoErp { get; set; }
        [JsonPropertyName("cierra_pedido")] public bool? CierraPedido { get; set; }
        [JsonPropertyName("lineas")] public List<LineaRemision>? Lineas { get; set; }
    }

    /// <summary>Comando ya leído y con su carga tipada.</summary>
    public sealed record ComandoLeido(string CommandType, string? Variant, ICarga Carga);

    public sealed class DocumentoErp
    {
        [JsonPropertyName("rol")] public string Rol { get; set; } = string.Empty;
        [JsonPropertyName("concepto")] public string Concepto { get; set; } = string.Empty;
        [JsonPropertyName("folio")] public string Folio { get; set; } = string.Empty;
        [JsonPropertyName("id_erp")] public long IdErp { get; set; }
        [JsonPropertyName("costo")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public decimal? Costo { get; set; }
    }

    public sealed class Resultado
    {
        [JsonPropertyName("folio")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Folio { get; set; }
        [JsonPropertyName("id_erp")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public long? IdErp { get; set; }
        [JsonPropertyName("documentos")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<DocumentoErp>? Documentos { get; set; }
        [JsonPropertyName("pedido_cancelado")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? PedidoCancelado { get; set; }
    }

    /// <summary>Lo que devuelve un gateway al ejecutar un comando: un resultado o un error del contrato.</summary>
    public sealed record ResultadoEjecucion(Resultado? Resultado, ErrorContrato? Error)
    {
        /// <summary>Solo en modo simulado: la regla de fallo pide perder el callback (FR-008).</summary>
        public bool OmitirCallback { get; init; }

        public static ResultadoEjecucion Exito(Resultado r) => new(r, null);

        public static ResultadoEjecucion Fallo(ErrorContrato e) => new(null, e);
    }
}
