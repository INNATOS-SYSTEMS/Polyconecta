using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Application.Common;
using PolyConecta.Domain.Common;

namespace PolyConecta.Api.Middleware;

/// <summary>
/// Traduce las excepciones a ProblemDetails (RFC 7807) con el <c>code</c> estable de contracts/api-f1.md:
/// 400 VALIDACION (con <c>errores[]</c>), 403 PERMISO_DENEGADO y 409 TRANSICION_INVALIDA,
/// DOCUMENTO_MODIFICADO o el de la regla, siempre con la <c>razon</c> que pinta la interfaz (CT-26).
/// </summary>
public partial class ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (estado, codigo, razon) = Traducir(ex);
            if (estado >= HttpStatusCode.InternalServerError)
                LogNoControlada(logger, ex, ex.Message);
            else
                LogRechazada(logger, codigo, ex.Message);
            await EscribirAsync(context, ex, estado, codigo, razon);
        }
    }

    public static (HttpStatusCode Estado, string Codigo, string? Razon) Traducir(Exception ex) => ex switch
    {
        ValidacionException => (HttpStatusCode.BadRequest, "VALIDACION", ex.Message),
        DatosIncompletosException => (HttpStatusCode.BadRequest, "VALIDACION", ex.Message),
        PermisoDenegadoException p => (HttpStatusCode.Forbidden, "PERMISO_DENEGADO", p.Razon),
        KeyNotFoundException => (HttpStatusCode.NotFound, "NO_ENCONTRADO", ex.Message),
        DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "DOCUMENTO_MODIFICADO",
            "Otro usuario cambió el documento mientras lo tenías abierto. Recarga para ver sus cambios."),
        DbUpdateException { InnerException: Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 } } =>
            (HttpStatusCode.Conflict, "DUPLICADO", "Ya existe un registro con esos datos, o alguien hizo lo mismo al mismo tiempo."),
        TransicionInvalidaException => (HttpStatusCode.Conflict, "TRANSICION_INVALIDA", ex.Message),
        ReglaDeNegocioException r => (HttpStatusCode.Conflict, r.Codigo, r.Razon),
        ArgumentException or InvalidOperationException => (HttpStatusCode.BadRequest, "VALIDACION", ex.Message),
        UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "SIN_SESION", ex.Message),
        _ => (HttpStatusCode.InternalServerError, "ERROR_INTERNO", null),
    };

    private static Task EscribirAsync(HttpContext context, Exception exception, HttpStatusCode estado, string codigo, string? razon)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)estado;

        var problem = new ProblemDetails
        {
            Status = (int)estado,
            Title = codigo,
            Detail = estado == HttpStatusCode.InternalServerError ? "Error interno." : exception.Message,
            Instance = context.Request.Path,
        };
        problem.Extensions["code"] = codigo;
        problem.Extensions["razon"] = razon;
        if (exception is ValidacionException v)
            problem.Extensions["errores"] = v.Errores.Select(e => new { campo = e.Campo, mensaje = e.Mensaje }).ToList();
        if (exception is DatosIncompletosException d)
            problem.Extensions["errores"] = d.Faltantes.Select(e => new { campo = e.Campo, mensaje = e.Mensaje }).ToList();
        problem.Extensions["traceId"] = context.TraceIdentifier;
        problem.Extensions["timestamp"] = DateTime.UtcNow;

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, Json));
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Excepción no controlada: {Mensaje}")]
    private static partial void LogNoControlada(ILogger logger, Exception error, string mensaje);

    [LoggerMessage(Level = LogLevel.Information, Message = "Petición rechazada {Codigo}: {Mensaje}")]
    private static partial void LogRechazada(ILogger logger, string codigo, string mensaje);
}
