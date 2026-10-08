namespace PolyConecta.Infrastructure.Erp;

/// <summary>
/// Conexión con el bridge (sección Erp). Pasar del simulador al bridge real es solo cambiar la URL
/// y los comandos habilitados (CT-03, FR-021). Los secretos llegan por variable de entorno (CT-29).
/// </summary>
public sealed class ErpOptions
{
    public const string Seccion = "Erp";

    /// <summary>URL del bridge (Erp__BridgeUrl). Sin valor, el despachador no arranca.</summary>
    public string? BridgeUrl { get; set; }

    /// <summary>URL pública de esta API que el bridge usa para el callback (Erp__CallbackBaseUrl).</summary>
    public string CallbackBaseUrl { get; set; } = "http://localhost:9020";

    /// <summary>Secreto compartido para verificar la firma del callback (Erp__CallbackSecret, D-121).</summary>
    public string CallbackSecret { get; set; } = string.Empty;

    /// <summary>Comandos que se envían; los demás se quedan Pendiente (CT-03). Vacío = todos.</summary>
    public List<string> ComandosHabilitados { get; set; } = [];

    /// <summary>Intentos de envío ante fallas de red o 5xx antes de dejar el comando en Error (CT-20).</summary>
    public int MaxIntentos { get; set; } = 5;

    /// <summary>Si el callback no llega en este plazo, se consulta GET /transactions/{id} (§3).</summary>
    public int CallbackTimeoutSegundos { get; set; } = 120;

    public int IntervaloMs { get; set; } = 1000;

    /// <summary>Moneda base: su tipo de cambio es 1 y no se edita (D-146, R-09).</summary>
    public string MonedaBase { get; set; } = "MXN";

    /// <summary>Monedas que el bridge sabe traducir (BridgeConfig__Monedas__{ISO}); un pedido no acepta otra.</summary>
    public List<string> Monedas { get; set; } = ["MXN", "USD"];

    public const string RutaCallback = "/api/v1/plataforma/bridge/callbacks";

    public bool Habilitado(string commandType) => ComandosHabilitados.Count == 0 || ComandosHabilitados.Contains(commandType);
}
