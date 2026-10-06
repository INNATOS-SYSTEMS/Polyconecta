using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace PolyConecta.Application.Tests.Erp;

/// <summary>Bridge falso: registra los comandos recibidos y responde lo que la prueba indique.</summary>
public sealed class BridgeFalso : HttpMessageHandler
{
    public List<JsonObject> Recibidos { get; } = [];

    /// <summary>Respuesta al POST; por omisión, 202 con un transaction_id nuevo.</summary>
    public Func<JsonObject, HttpResponseMessage> AlEnviar { get; set; } = c => Aceptar();

    /// <summary>Respuesta a GET /transactions/{id}; por omisión, 404.</summary>
    public Func<string, HttpResponseMessage> AlConsultar { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

    public IEnumerable<string> Llaves => Recibidos.Select(c => c["idempotency_key"]!.GetValue<string>());

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Get)
            return AlConsultar(request.RequestUri!.Segments[^1]);
        var comando = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!.AsObject();
        Recibidos.Add(comando);
        return AlEnviar(comando);
    }

    public static HttpResponseMessage Aceptar(string? transactionId = null, string status = "PENDING", bool duplicado = false) =>
        Json(HttpStatusCode.Accepted, new JsonObject
        {
            ["transaction_id"] = transactionId ?? Guid.NewGuid().ToString(),
            ["correlation_id"] = "x",
            ["status"] = status,
            ["created_at"] = "2026-10-12T00:00:00Z",
            ["is_duplicate"] = duplicado,
        });

    public static HttpResponseMessage Json(HttpStatusCode status, JsonObject cuerpo) =>
        new(status) { Content = new StringContent(cuerpo.ToJsonString(), Encoding.UTF8, "application/json") };

    /// <summary>Cuerpo de callback (contrato §3) confirmado con un documento.</summary>
    public static JsonObject Confirmado(string llave, string folio = "1042", long idErp = 55120) => new()
    {
        ["event_type"] = "transaction.status_changed",
        ["contract_version"] = "1.0",
        ["transaction_id"] = Guid.NewGuid().ToString(),
        ["correlation_id"] = "x",
        ["idempotency_key"] = llave,
        ["command_type"] = "TRASPASO",
        ["status"] = "CONFIRMED",
        ["result"] = new JsonObject
        {
            ["documentos"] = new JsonArray(
                new JsonObject { ["rol"] = "salida", ["concepto"] = "SAL", ["folio"] = folio, ["id_erp"] = idErp },
                new JsonObject { ["rol"] = "entrada", ["concepto"] = "ENT", ["folio"] = "877", ["id_erp"] = idErp + 1 }),
        },
        ["error"] = null,
        ["timestamp"] = "2026-10-12T00:00:00Z",
    };

    public static JsonObject Fallido(string llave, string codigo) => new()
    {
        ["event_type"] = "transaction.status_changed",
        ["contract_version"] = "1.0",
        ["transaction_id"] = Guid.NewGuid().ToString(),
        ["correlation_id"] = "x",
        ["idempotency_key"] = llave,
        ["command_type"] = "TRASPASO",
        ["status"] = "FAILED",
        ["result"] = null,
        ["error"] = new JsonObject { ["code"] = codigo, ["retryable"] = false, ["message"] = "sin existencia" },
        ["timestamp"] = "2026-10-12T00:00:00Z",
    };
}
