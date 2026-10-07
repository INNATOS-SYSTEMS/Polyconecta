using System.Text.Json.Serialization;
using PolyConecta.Api.Hubs;
using PolyConecta.Api.Middleware;
using PolyConecta.Application;
using PolyConecta.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
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

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ProblemDetailsMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
// Después de UseRouting: así ve la política propia del hub (RequireCors). Antes solo aplicaba la
// política por omisión y la negociación de SignalR fallaba.
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatterHub>("/hubs/chatter").RequireCors(PoliticaChatter);

app.Run();

public partial class Program
{
    private const string PoliticaChatter = "chatter";
}
