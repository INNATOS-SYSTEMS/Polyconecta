using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

[assembly: AssemblyFixture(typeof(PolyConecta.Contract.Tests.Infraestructura.Bridge))]

namespace PolyConecta.Contract.Tests.Infraestructura;

/// <summary>
/// El bridge bajo prueba (BRIDGE_URL) y un receptor de callbacks propio que verifica la firma
/// (D-121, contrato §3) con BRIDGE_CALLBACK_SECRET. CALLBACK_HOST es el nombre con el que el
/// bridge alcanza este proceso (localhost por omisión).
/// </summary>
public sealed class Bridge : IAsyncLifetime
{
    public static string Raiz { get; } = BuscarRaiz();

    private readonly ConcurrentDictionary<string, List<JsonObject>> _callbacks = new();
    private WebApplication? _receptor;

    public HttpClient Http { get; } = new()
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("BRIDGE_URL") is { Length: > 0 } url ? url : "http://localhost:5005"),
        Timeout = TimeSpan.FromSeconds(30),
    };

    public string Secreto { get; } = Environment.GetEnvironmentVariable("BRIDGE_CALLBACK_SECRET") ?? "secreto-de-pruebas";

    public string CallbackUrl { get; private set; } = string.Empty;

    public bool EsSimulado { get; private set; }

    /// <summary>Callbacks recibidos con firma inválida o vencida. Debe quedar en cero.</summary>
    public int FirmasInvalidas;

    public async ValueTask InitializeAsync()
    {
        if (!Activacion.HayBridge) return;
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://0.0.0.0:0");
        _receptor = builder.Build();
        _receptor.MapPost("/callbacks", async (HttpRequest req) =>
        {
            using var lector = new StreamReader(req.Body, Encoding.UTF8);
            var cuerpo = await lector.ReadToEndAsync();
            if (!FirmaValida(req.Headers["X-Bridge-Signature"].ToString(), cuerpo))
            {
                Interlocked.Increment(ref FirmasInvalidas);
                return Results.Unauthorized();
            }
            var json = JsonNode.Parse(cuerpo)!.AsObject();
            var lista = _callbacks.GetOrAdd(json["idempotency_key"]!.GetValue<string>(), _ => new());
            lock (lista) lista.Add(json);
            return Results.Ok();
        });
        await _receptor.StartAsync();
        var puerto = new Uri(_receptor.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First()).Port;
        CallbackUrl = $"http://{Environment.GetEnvironmentVariable("CALLBACK_HOST") ?? "localhost"}:{puerto}/callbacks";

        var salud = await Http.GetFromJsonAsync<JsonObject>("/health");
        EsSimulado = salud!["mode"]?.GetValue<string>() == "Simulated";
    }

    public async ValueTask DisposeAsync()
    {
        if (_receptor is not null) await _receptor.DisposeAsync();
        Http.Dispose();
    }

    /// <summary>Lee un ejemplo del contrato con una idempotency_key nueva y el callback de esta suite.</summary>
    public JsonObject Ejemplo(string nombre, Action<JsonObject>? cambiar = null)
    {
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(Raiz, "docs", "contratos", "ejemplos", nombre)))!.AsObject();
        json["idempotency_key"] = $"{json["idempotency_key"]}:{Guid.NewGuid():N}";
        json["callback_url"] = CallbackUrl;
        cambiar?.Invoke(json);
        return json;
    }

    public async Task<(HttpStatusCode Status, JsonObject Cuerpo)> EnviarAsync(JsonObject comando)
    {
        using var r = await Http.PostAsync("/api/v1/transactions",
            new StringContent(comando.ToJsonString(), Encoding.UTF8, "application/json"));
        var texto = await r.Content.ReadAsStringAsync();
        return (r.StatusCode, JsonNode.Parse(texto)!.AsObject());
    }

    /// <summary>Espera el primer callback de la llave; null si no llega en el plazo.</summary>
    public async Task<JsonObject?> CallbackAsync(string idempotencyKey, TimeSpan? plazo = null)
    {
        var limite = DateTime.UtcNow + (plazo ?? TimeSpan.FromSeconds(15));
        while (DateTime.UtcNow < limite)
        {
            if (_callbacks.TryGetValue(idempotencyKey, out var lista))
                lock (lista) if (lista.Count > 0) return lista[0];
            await Task.Delay(100);
        }
        return null;
    }

    public int CallbacksRecibidos(string idempotencyKey) =>
        _callbacks.TryGetValue(idempotencyKey, out var lista) ? lista.Count : 0;

    public async Task<(HttpStatusCode Status, JsonObject? Cuerpo)> ConsultarAsync(string transactionId)
    {
        using var r = await Http.GetAsync($"/api/v1/transactions/{transactionId}");
        var texto = await r.Content.ReadAsStringAsync();
        return (r.StatusCode, string.IsNullOrWhiteSpace(texto) ? null : JsonNode.Parse(texto)!.AsObject());
    }

    public async Task FallosAsync(params object[] reglas)
    {
        using var r = await Http.PutAsJsonAsync("/admin/simulated/faults", reglas);
        r.EnsureSuccessStatusCode();
    }

    private bool FirmaValida(string cabecera, string cuerpo)
    {
        var partes = cabecera.Split(',').Select(p => p.Split('=', 2)).Where(p => p.Length == 2).ToDictionary(p => p[0], p => p[1]);
        if (!partes.TryGetValue("t", out var t) || !partes.TryGetValue("v1", out var v1) || !long.TryParse(t, out var unix))
            return false;
        if (Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - unix) > 300)
            return false;
        var esperado = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secreto), Encoding.UTF8.GetBytes(t + "." + cuerpo)));
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(esperado), Encoding.ASCII.GetBytes(v1));
    }

    private static string BuscarRaiz()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("Polyconecta.slnx").Any()) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }
}
