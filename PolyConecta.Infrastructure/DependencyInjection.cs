using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PolyConecta.Application.Common;
using PolyConecta.Infrastructure.Common;
using PolyConecta.Infrastructure.Persistence;

namespace PolyConecta.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // La cadena nunca se versiona: ConnectionStrings__PolyConecta (CT-29). Es el login sin DDL (CT-30).
        var conexion = configuration.GetConnectionString("PolyConecta")
            ?? throw new InvalidOperationException(
                "Falta ConnectionStrings:PolyConecta. Defínela en la variable de entorno ConnectionStrings__PolyConecta.");

        services.AddDbContext<PolyDbContext>(options => options.UseSqlServer(conexion));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<CorrelationContext>();
        services.AddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ICurrentUser, SistemaCurrentUser>();
        return services;
    }
}
