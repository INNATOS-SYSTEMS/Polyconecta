using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace PolyConecta.Api.Middleware;

/// <summary>
/// Global exception handling middleware producing RFC 7807 ProblemDetails responses.
/// </summary>
public class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;

    public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception encountered: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        
        var statusCode = exception switch
        {
            KeyNotFoundException => HttpStatusCode.NotFound,
            PolyConecta.Application.Common.ValidacionException => HttpStatusCode.BadRequest,
            PolyConecta.Domain.Common.TransicionInvalidaException => HttpStatusCode.Conflict,
            ArgumentException or InvalidOperationException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = (int)statusCode;

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = exception.GetType().Name,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        problem.Extensions["timestamp"] = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(problem);
        return context.Response.WriteAsync(json);
    }
}
