using System;
using System.IO;
using Contpaq.Bridge.Api.Hubs;
using Contpaq.Bridge.Api.Middleware;
using Contpaq.Bridge.Core.Services;
using Contpaq.Bridge.Infrastructure.Persistence;
using Contpaq.Bridge.Infrastructure.Sdk;
using Contpaq.Bridge.Infrastructure.Webhooks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Contpaq.Bridge.Infrastructure.Logging;
using Microsoft.AspNetCore.SignalR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Console Log Stream Service & Serilog
var logStreamService = new ConsoleLogStreamService();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/bridge-.log", rollingInterval: RollingInterval.Day, flushToDiskInterval: TimeSpan.FromSeconds(1))
    .WriteTo.Sink(new ConsoleLogStreamSink(logStreamService))
    .CreateLogger();

builder.Host.UseSerilog();

var config = builder.Configuration;
var sqliteConn = config["BridgeConfig:SqliteConnectionString"] ?? "Data Source=bridge_outbox.db";
// La cadena de conexion a CONTPAQi nunca se versiona: se toma de la variable de entorno
// BridgeConfig__SqlConnectionString (o de dotnet user-secrets en desarrollo).
var sqlConn = config["BridgeConfig:SqlConnectionString"];
if (string.IsNullOrWhiteSpace(sqlConn))
    throw new InvalidOperationException(
        "Falta BridgeConfig:SqlConnectionString. Definela en la variable de entorno BridgeConfig__SqlConnectionString.");
var port = int.TryParse(config["BridgeConfig:DashboardPort"], out var p) ? p : 5005;

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Initialize SQLite schema
var dbInit = new DbInitializer(sqliteConn);
dbInit.Initialize();

// Register Repositories & Services
builder.Services.AddSingleton(logStreamService);
builder.Services.AddSingleton<IOutboxRepository>(new OutboxRepository(sqliteConn));
builder.Services.AddSingleton<ISqlReadRepository>(new SqlReadRepository(sqlConn));
builder.Services.AddSingleton<IWebhookDispatcher, WebhookDispatcher>();

// Register Background Services
builder.Services.AddHostedService<MetricCollectorService>();
builder.Services.AddHostedService<ContpaqiSdkGateway>();

// Register Controllers & SignalR & OpenAPI
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CONTPAQi Integration Bridge API",
        Version = "v1",
        Description = "Microservicio de integración REST & SignalR x86 para CONTPAQi Comercial Premium"
    });
});

var app = builder.Build();

logStreamService.SetHubContext(app.Services.GetRequiredService<IHubContext<DashboardHub>>());

app.UseMiddleware<CorrelationMiddleware>();

// Configure Dashboard Static Files
var dashboardWwwroot = Path.Combine(AppContext.BaseDirectory, "Dashboard", "wwwroot");
if (!Directory.Exists(dashboardWwwroot))
{
    dashboardWwwroot = Path.Combine(builder.Environment.ContentRootPath, "Dashboard", "wwwroot");
}

if (Directory.Exists(dashboardWwwroot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(dashboardWwwroot),
        RequestPath = ""
    });
}
else
{
    app.UseStaticFiles();
}

// Configure Swagger & OpenAPI Specification
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CONTPAQi Bridge API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.MapControllers();
app.MapHub<DashboardHub>("/hubs/dashboard");

// Configure Scalar API Reference
app.MapScalarApiReference(options =>
{
    options.WithTitle("CONTPAQi Integration Bridge API")
           .WithTheme(ScalarTheme.Moon)
           .WithOpenApiRoutePattern("/swagger/v1/swagger.json")
           .WithCdnUrl("https://cdn.jsdelivr.net/npm/@scalar/api-reference");
});

app.MapGet("/health", (IOutboxRepository repo) =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        worker_architecture = "x86",
        sdk_initialized = true,
        sql_connected = true,
        circuit_state = MetricCollectorService.CircuitState,
        timestamp = DateTime.UtcNow.ToString("o")
    });
});

app.Run();
