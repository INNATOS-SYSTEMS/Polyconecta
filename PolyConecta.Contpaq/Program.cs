using System;
using System.IO;
using Contpaq.Bridge.Api.Hubs;
using Contpaq.Bridge.Api.Middleware;
using Contpaq.Bridge.Core.Services;
using Contpaq.Bridge.Infrastructure.Persistence;
using Contpaq.Bridge.Infrastructure.Sdk;
using Contpaq.Bridge.Infrastructure.Webhooks;
using Contpaq.Bridge.Infrastructure.Outbox;
using Contpaq.Bridge.Core.Configuration;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Core.Validation;
using Contpaq.Bridge.Simulated;
using System.Collections.Generic;
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
var opciones = BridgeOptions.Leer(config);
var sqliteConn = config["BridgeConfig:SqliteConnectionString"] ?? "Data Source=bridge_outbox.db";

var port = int.TryParse(config["BridgeConfig:DashboardPort"], out var p) ? p : 9030;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Initialize SQLite schema
var dbInit = new DbInitializer(sqliteConn);
dbInit.Initialize();

// Lo común a los dos modos (D-122): API, outbox, validaciones, idempotencia y callbacks.
builder.Services.AddSingleton(logStreamService);
builder.Services.AddSingleton(opciones);
builder.Services.AddSingleton<IOutboxRepository>(new OutboxRepository(sqliteConn));
builder.Services.AddSingleton<ConfiguracionConceptos>();
builder.Services.AddSingleton<ValidadorComandos>();
builder.Services.AddHttpClient(nameof(WebhookDispatcher), c => c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<IWebhookDispatcher, WebhookDispatcher>();
builder.Services.AddSingleton(new CircuitBreakerPolicy(
    int.TryParse(config["BridgeConfig:CircuitBreakerFailureThreshold"], out var umbral) ? umbral : 3,
    int.TryParse(config["BridgeConfig:CircuitBreakerCooldownSeconds"], out var enfriamiento) ? enfriamiento : 15));

// Lo que cambia por modo: el adaptador de escritura y el de lectura.
if (opciones.Mode == BridgeMode.Real)
{
    // La cadena de conexion a CONTPAQi nunca se versiona: se toma de la variable de entorno
    // BridgeConfig__SqlConnectionString (o de dotnet user-secrets en desarrollo).
    var sqlConn = config["BridgeConfig:SqlConnectionString"];
    if (string.IsNullOrWhiteSpace(sqlConn))
        throw new InvalidOperationException(
            "Falta BridgeConfig:SqlConnectionString. Definela en la variable de entorno BridgeConfig__SqlConnectionString.");
    builder.Services.AddSingleton<ISqlReadRepository>(new SqlReadRepository(sqlConn));
    // Número (1 a 6) de CIDVALORCLASIFICACION{n} que es "TIPO DE PRODUCTOS" (A-05); sin valor, la clasificación va null.
    int? clasificacionProductos = int.TryParse(config["BridgeConfig:Clasificacion:Productos"], out var clasif) ? clasif : null;
    builder.Services.AddSingleton<IReadRepository>(sp =>
        new SqlContractReadRepository(sqlConn, sp.GetRequiredService<ConfiguracionConceptos>(), clasificacionProductos));
    builder.Services.AddSingleton<ISdkGateway, ContpaqiSdkGateway>();
}
else
{
    var seedPath = config["BridgeConfig:Simulated:SeedPath"] ?? "Simulated/seed.json";
    if (!Path.IsPathRooted(seedPath)) seedPath = Path.Combine(AppContext.BaseDirectory, seedPath);
    var store = new SimulatedStore(sqliteConn);
    store.Inicializar();
    builder.Services.AddSingleton(SimulatedCatalog.Cargar(seedPath));
    builder.Services.AddSingleton(store);
    builder.Services.AddSingleton<FaultStore>();
    builder.Services.AddSingleton<IReadRepository, SimulatedReadRepository>();
    builder.Services.AddSingleton<ISdkGateway, SimulatedSdkGateway>();
}

// Register Background Services
builder.Services.AddHostedService<MetricCollectorService>();
builder.Services.AddSingleton<OutboxWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OutboxWorker>());

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

app.MapGet("/health", (ISdkGateway gateway) => Results.Ok(new
{
    status = "Healthy",
    mode = opciones.Mode.ToString(),
    contract_version = Contrato.VersionActual,
    worker_architecture = Environment.Is64BitProcess ? "x64" : "x86",
    sdk_initialized = gateway.SesionActiva,
    circuit_state = MetricCollectorService.CircuitState,
    timestamp = DateTime.UtcNow.ToString("o"),
}));

// Fallos simulados (FR-008 de la spec 002): solo existen en modo simulado (§7).
if (opciones.Mode == BridgeMode.Simulated)
{
    app.MapGet("/admin/simulated/faults", (FaultStore fallos) => Results.Ok(fallos.Reglas));
    app.MapPut("/admin/simulated/faults", (List<FaultRule> reglas, FaultStore fallos) =>
    {
        fallos.Reemplazar(reglas);
        return Results.Ok(fallos.Reglas);
    });
}

app.Run();

public partial class Program;
