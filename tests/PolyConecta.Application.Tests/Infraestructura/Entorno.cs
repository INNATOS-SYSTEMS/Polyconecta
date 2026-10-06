using Microsoft.EntityFrameworkCore;
using PolyConecta.Application.Common;
using PolyConecta.Infrastructure.Common;
using PolyConecta.Infrastructure.Persistence;
using PolyConecta.IntegrationTests.Infraestructura;

namespace PolyConecta.Application.Tests.Infraestructura;

public sealed class RelojFijo(DateTimeOffset ahora) : IClock
{
    public DateTimeOffset Now { get; set; } = ahora;
}

public sealed class UsuarioFijo(string nombre, string? rol = null) : ICurrentUser
{
    public string UserName => nombre;

    public string? Role => rol;
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
