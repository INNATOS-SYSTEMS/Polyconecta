using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using PolyConecta.Domain.Plataforma;

namespace PolyConecta.Infrastructure.Erp;

/// <summary>Resultado de enviar un comando al bridge.</summary>
public abstract record EnvioBridge
{
    /// <summary>202: el bridge lo aceptó. Si es un reenvío, trae el estado del original.</summary>
    public sealed record Aceptado(string TransactionId, string Status, bool EsDuplicado) : EnvioBridge;

    /// <summary>400: la carga no cumple el contrato. No se reintenta.</summary>
    public sealed record Rechazado(string Codigo, string Mensaje) : EnvioBridge;

    /// <summary>Red caída, tiempo agotado o 5xx: se reintenta con espera creciente.</summary>
    public sealed record FallaTransitoria(string Mensaje) : EnvioBridge;
}

/// <summary>Cliente HTTP del contrato bridge-v1: arma el sobre (§2) y propaga el correlation_id (CT-31).</summary>
public sealed class BridgeHttpClient(HttpClient http, IOptions<ErpOptions> opciones)
{
    public const string VersionContrato = "1.0";

    public async Task<EnvioBridge> EnviarAsync(OutboxMessage mensaje, CancellationToken cancellationToken)
    {
        var sobre = new JsonObject
        {
            ["contract_version"] = VersionContrato,
            ["command_type"] = mensaje.CommandType,
            ["variant"] = mensaje.Variant,
            ["idempotency_key"] = mensaje.IdempotencyKey,
            ["correlation_id"] = mensaje.CorrelationId,
            ["client_app_id"] = "polyconecta",
            ["callback_url"] = opciones.Value.CallbackBaseUrl.TrimEnd('/') + ErpOptions.RutaCallback,
            ["payload"] = JsonNode.Parse(mensaje.Payload),
        };
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transactions")
        {
            Content = new StringContent(sobre.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        peticion.Headers.TryAddWithoutValidation("X-Correlation-ID", mensaje.CorrelationId);

        try
        {
            using var r = await http.SendAsync(peticion, cancellationToken);
            var texto = await r.Content.ReadAsStringAsync(cancellationToken);
            if (r.StatusCode == HttpStatusCode.Accepted)
            {
                using var acuse = JsonDocument.Parse(texto);
                var raiz = acuse.RootElement;
                return new EnvioBridge.Aceptado(
                    raiz.GetProperty("transaction_id").GetString()!,
                    raiz.GetProperty("status").GetString()!,
                    raiz.TryGetProperty("is_duplicate", out var d) && d.GetBoolean());
            }
            if ((int)r.StatusCode is >= 400 and < 500)
            {
                var (codigo, msj) = LeerError(texto);
                return new EnvioBridge.Rechazado(codigo, msj);
            }
            return new EnvioBridge.FallaTransitoria($"HTTP {(int)r.StatusCode}: {Recortar(texto)}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return new EnvioBridge.FallaTransitoria(ex.Message);
        }
    }

    /// <summary>GET /api/v1/transactions/{id}: el mismo cuerpo que el callback, o null si no existe.</summary>
    public async Task<JsonDocument?> ConsultarAsync(string transactionId, CancellationToken cancellationToken)
    {
        using var r = await http.GetAsync($"/api/v1/transactions/{Uri.EscapeDataString(transactionId)}", cancellationToken);
        if (r.StatusCode == HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync(cancellationToken));
    }

    private static (string Codigo, string Mensaje) LeerError(string texto)
    {
        try
        {
            using var doc = JsonDocument.Parse(texto);
            return (doc.RootElement.GetProperty("code").GetString() ?? "CARGA_INVALIDA",
                doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : string.Empty);
        }
        catch (JsonException)
        {
            return ("CARGA_INVALIDA", Recortar(texto));
        }
    }

    private static string Recortar(string texto) => texto.Length > 300 ? texto[..300] : texto;
}
