using Microsoft.Extensions.DependencyInjection;

namespace PolyConecta.Infrastructure.Persistence.Sembradores;

/// <summary>Corre los sembradores registrados, en orden, en un ámbito propio. Lo llama la API al arrancar.</summary>
public static class Sembrar
{
    public static async Task SembrarAsync(this IServiceProvider servicios, CancellationToken cancellationToken = default)
    {
        await using var scope = servicios.CreateAsyncScope();
        foreach (var sembrador in scope.ServiceProvider.GetServices<IDataSeeder>())
            await sembrador.SeedAsync(cancellationToken);
    }
}
