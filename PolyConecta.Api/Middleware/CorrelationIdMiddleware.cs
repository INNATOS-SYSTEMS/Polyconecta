using PolyConecta.Infrastructure.Common;

namespace PolyConecta.Api.Middleware;

/// <summary>
/// Toma X-Correlation-ID de la petición (o genera uno), lo deja en el ámbito y lo devuelve en la
/// respuesta, para que viaje de la interfaz a la API, al outbox y al bridge (CT-31).
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string Header = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context, CorrelationContext correlation)
    {
        if (context.Request.Headers.TryGetValue(Header, out var valor) && !string.IsNullOrWhiteSpace(valor))
            correlation.CorrelationId = valor.ToString();

        context.Response.Headers[Header] = correlation.CorrelationId;
        using (context.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>()
                   .BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlation.CorrelationId }))
        {
            await next(context);
        }
    }
}
