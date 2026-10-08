using System.Text.Json.Serialization;
using PolyConecta.Api.Hubs;
using PolyConecta.Api.Middleware;
using PolyConecta.Api.Seguridad;
using PolyConecta.Application;
using PolyConecta.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        // Los enums viajan por su nombre ("Comercial", "Borrador"), como los pinta la web.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    // Un cuerpo que no se puede leer responde como cualquier 400 de la API: code VALIDACION y errores[].
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = contexto =>
    {
        var errores = contexto.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(x => new { campo = e.Key.TrimStart('$', '.'), mensaje = string.IsNullOrEmpty(x.ErrorMessage) ? "Valor inválido." : x.ErrorMessage }))
            .ToList();
        var cuerpo = new Dictionary<string, object?>
        {
            ["status"] = 400, ["title"] = "VALIDACION", ["code"] = "VALIDACION",
            ["razon"] = "La petición no es válida.", ["errores"] = errores,
        };
        return new Microsoft.AspNetCore.Mvc.ObjectResult(cuerpo) { StatusCode = 400, ContentTypes = { "application/problem+json" } };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "PolyConecta Operational API",
        Version = "v1",
        Description = "Operational Routing, Work Centers & CONTPAQi Integration Engine"
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAutenticacionPolyConecta(builder.Environment);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
    // El cliente de SignalR negocia con credenciales, y el navegador rechaza "*" con credenciales
    // (research R-07 de la spec 001): el hub tiene su propia política para la aplicación Angular.
    options.AddPolicy(PoliticaChatter, policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Chatter:Origenes").Get<string[]>() ?? ["http://localhost:9000"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSignalR();

var app = builder.Build();

// Datos iniciales (plantas, permisos, grupos y Administrador inicial). La base ya viene migrada con el
// login de migraciones (CT-30); aquí solo se escriben datos.
try
{
    await PolyConecta.Infrastructure.Persistence.Sembradores.Sembrar.SembrarAsync(app.Services);
}
catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException)
{
    app.Logger.LogError(ex, "No se pudieron sembrar los datos iniciales: {Mensaje}", ex.Message);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ProblemDetailsMiddleware>();
app.UseEncabezadoAntiFalsificacion();
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
// Después de UseRouting: así ve la política propia del hub (RequireCors). Antes solo aplicaba la
// política por omisión y la negociación de SignalR fallaba.
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatterHub>("/hubs/chatter").RequireCors(PoliticaChatter);

await app.RunAsync();

public partial class Program
{
    private const string PoliticaChatter = "chatter";
}
