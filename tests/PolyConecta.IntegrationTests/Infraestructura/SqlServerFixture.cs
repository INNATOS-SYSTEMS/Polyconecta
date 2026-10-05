using Microsoft.EntityFrameworkCore;
using PolyConecta.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

[assembly: AssemblyFixture(typeof(PolyConecta.IntegrationTests.Infraestructura.SqlServerFixture))]

namespace PolyConecta.IntegrationTests.Infraestructura;

/// <summary>
/// Un SQL Server 2022 en contenedor por corrida de pruebas. Cada prueba pide su propia base,
/// creada con las migraciones, nunca con EnsureCreated (CT-06, research R-07 de la spec 002).
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    // Fijada por digest para que local y CI usen exactamente la misma imagen.
    private const string Imagen =
        "mcr.microsoft.com/mssql/server:2022-latest@sha256:4402d880dd4c34bfa7d8705e56a86cd6c88da80a1f6bbbe741f999e76264a090";

    private readonly MsSqlContainer _contenedor = new MsSqlBuilder(Imagen).Build();

    public async ValueTask InitializeAsync() => await _contenedor.StartAsync();

    public async ValueTask DisposeAsync() => await _contenedor.DisposeAsync();

    /// <summary>Crea una base vacía, le aplica las migraciones y devuelve su cadena de conexión.</summary>
    public async Task<string> CrearBaseAsync()
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_contenedor.GetConnectionString())
        {
            InitialCatalog = $"pc_{Guid.NewGuid():N}"
        };
        await using var db = new PolyDbContext(Opciones(builder.ConnectionString));
        await db.Database.MigrateAsync();
        return builder.ConnectionString;
    }

    public async Task<PolyDbContext> CrearContextoAsync() => new(Opciones(await CrearBaseAsync()));

    private static DbContextOptions<PolyDbContext> Opciones(string conexion) =>
        new DbContextOptionsBuilder<PolyDbContext>().UseSqlServer(conexion).Options;
}
