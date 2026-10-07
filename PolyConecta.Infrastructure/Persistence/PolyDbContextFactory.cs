using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PolyConecta.Infrastructure.Persistence;

/// <summary>
/// Fábrica para las herramientas de EF (dotnet ef). Las migraciones se aplican con el login de
/// migraciones (CT-30), que llega en ConnectionStrings__PolyConectaMigraciones. Para generar una
/// migración no hace falta conexión; se usa una cadena local de marcador.
/// </summary>
public sealed class PolyDbContextFactory : IDesignTimeDbContextFactory<PolyDbContext>
{
    public PolyDbContext CreateDbContext(string[] args)
    {
        var conexion = Environment.GetEnvironmentVariable("ConnectionStrings__PolyConectaMigraciones")
            ?? "Server=localhost;Database=PolyConecta;Integrated Security=false;TrustServerCertificate=true";
        return new PolyDbContext(new DbContextOptionsBuilder<PolyDbContext>().UseSqlServer(conexion).Options);
    }
}
