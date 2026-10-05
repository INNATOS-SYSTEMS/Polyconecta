using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Infrastructure.Outbox;
using PolyConecta.Infrastructure.Persistence;

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

// La cadena de conexión nunca se versiona: ConnectionStrings__PolyConecta (CT-29). La API usa
// un login sin permisos de DDL; las migraciones se aplican con otro (CT-30).
var connectionString = builder.Configuration.GetConnectionString("PolyConecta")
    ?? throw new InvalidOperationException(
        "Falta ConnectionStrings:PolyConecta. Defínela en la variable de entorno ConnectionStrings__PolyConecta.");

builder.Services.AddDbContext<PolyDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IOutboxPublisher, OutboxPublisher>();

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
