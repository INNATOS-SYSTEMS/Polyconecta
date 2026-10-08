using Microsoft.EntityFrameworkCore;
using PolyConecta.Application.Common;
using PolyConecta.Infrastructure.Common;
using PolyConecta.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using PolyConecta.Application;
using PolyConecta.Infrastructure;

namespace PolyConecta.Tests.Compartido;

public sealed class RelojFijo(DateTimeOffset ahora) : IClock
{
    public DateTimeOffset Now { get; set; } = ahora;
}

public sealed class UsuarioFijo(string nombre, string? rol = null, long? id = null) : ICurrentUser
{
    public string UserName => nombre;

    public long? UserId { get; set; } = id;

    public string NombreVisible { get; set; } = nombre;

    public string? GrupoEjercido { get; private set; } = rol;

    public bool EsSuplente { get; private set; }

    public void EjercerGrupo(string? grupo, bool esSuplente)
    {
        GrupoEjercido = grupo;
        EsSuplente = esSuplente;
    }
}

/// <summary>Base migrada más la tabla de prueba, y contextos con el interceptor de auditoría.</summary>
public sealed class Entorno
{
    public required string Conexion { get; init; }

    public RelojFijo Reloj { get; } = new(new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero));

    public UsuarioFijo Usuario { get; init; } = new("luis.alvarado", "Planner");

    public CorrelationContext Correlacion { get; } = new() { CorrelationId = "corr-prueba" };

    public static async Task<Entorno> CrearAsync(SqlServerFixture sql)
    {
        var entorno = new Entorno { Conexion = await sql.CrearBaseAsync() };
        await using var db = entorno.Contexto();
        await db.Database.ExecuteSqlRawAsync(PruebasDbContext.CrearTabla);
        return entorno;
    }

    public PruebasDbContext Contexto() =>
        new(new DbContextOptionsBuilder<PolyDbContext>()
            .UseSqlServer(Conexion)
            .AddInterceptors(new AuditoriaInterceptor(Reloj, Usuario, Correlacion))
            .Options);
}

/// <summary>
/// Servicios de Application e Infrastructure apuntando a la base de un <see cref="Entorno"/>, con el
/// documento de prueba registrado. El bridge se reemplaza con un HttpMessageHandler falso.
/// </summary>
public static class ServiciosDePrueba
{
    public static ServiceProvider Crear(
        Entorno entorno, HttpMessageHandler? bridge = null, Action<PolyConecta.Infrastructure.Erp.ErpOptions>? erp = null,
        Action<IServiceCollection>? configurar = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IClock>(entorno.Reloj);
        services.AddSingleton<ICurrentUser>(entorno.Usuario);
        services.AddScoped(_ => new CorrelationContext { CorrelationId = entorno.Correlacion.CorrelationId });
        services.AddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());
        services.AddScoped<PolyDbContext>(sp => new PruebasDbContext(
            new DbContextOptionsBuilder<PolyDbContext>()
                .UseSqlServer(entorno.Conexion)
                .AddInterceptors(new AuditoriaInterceptor(entorno.Reloj, entorno.Usuario, sp.GetRequiredService<CorrelationContext>()))
                .Options));
        services.AddScoped(sp => (PruebasDbContext)sp.GetRequiredService<PolyDbContext>());
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<PolyConecta.Application.Plataforma.Folios.IReferenceSequenceService, PolyConecta.Infrastructure.Plataforma.ReferenceSequenceService>();

        services.Configure<PolyConecta.Infrastructure.Erp.ErpOptions>(o =>
        {
            o.BridgeUrl = "http://bridge.prueba";
            o.CallbackSecret = "secreto";
            o.MaxIntentos = 3;
            o.CallbackTimeoutSegundos = 120;
            erp?.Invoke(o);
        });
        services.AddSingleton<PolyConecta.Infrastructure.Erp.RegistroDocumentosSincronizables>();
        services.AddScoped<PolyConecta.Application.Plataforma.Erp.IBridgeSyncService, PolyConecta.Infrastructure.Erp.BridgeSyncService>();
        services.AddScoped<PolyConecta.Application.Plataforma.Erp.IOutboxStore, PolyConecta.Infrastructure.Erp.OutboxStore>();
        services.AddScoped<PolyConecta.Application.Plataforma.Erp.IDocumentosSincronizables, PolyConecta.Infrastructure.Erp.DocumentosSincronizables>();
        services.AddDocumentoSincronizable<DocumentoDePrueba, TraductorDocumentoDePrueba>();
        services.AddScoped(sp => new PolyConecta.Infrastructure.Erp.BridgeHttpClient(
            new HttpClient(bridge ?? new HttpClientHandler()) { BaseAddress = new Uri("http://bridge.prueba") },
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PolyConecta.Infrastructure.Erp.ErpOptions>>()));
        services.AddSingleton<PolyConecta.Infrastructure.Erp.BridgeDispatcher>();
        services.AddApplication();
        configurar?.Invoke(services);
        return services.BuildServiceProvider();
    }
}
