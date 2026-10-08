using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Erp;

namespace PolyConecta.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso de Application. Sin MediatR ni Scrutor (D-72, research R-02).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddUseCase<ResultadoBridge, EfectoCallback, ConfirmarSincronizacion>();
        services.AddUseCase<ReintentarSincronizacionRequest, Unit, ReintentarSincronizacion>();
        return services;
    }

    /// <summary>
    /// Registra un caso de uso envuelto en sus decoradores, de fuera hacia dentro:
    /// registro → autorización → validación → transacción → caso de uso. Sin permiso no se valida ni se
    /// abre transacción (R-02); una petición inválida no abre transacción.
    /// </summary>
    public static IServiceCollection AddUseCase<TRequest, TResult, TUseCase>(this IServiceCollection services)
        where TUseCase : class, IUseCase<TRequest, TResult>
    {
        services.TryAddScoped<Autorizacion>();
        services.AddScoped<TUseCase>();
        services.AddScoped<IUseCase<TRequest, TResult>>(sp =>
        {
            IUseCase<TRequest, TResult> useCase = sp.GetRequiredService<TUseCase>();
            useCase = new TransactionDecorator<TRequest, TResult>(useCase, sp.GetRequiredService<IUnitOfWork>());
            useCase = new ValidationDecorator<TRequest, TResult>(useCase, sp.GetServices<IValidator<TRequest>>());
            useCase = new AuthorizationDecorator<TRequest, TResult>(
                useCase, sp.GetRequiredService<Autorizacion>(), sp.GetRequiredService<ICurrentUser>());
            useCase = new LoggingDecorator<TRequest, TResult>(
                useCase,
                sp.GetRequiredService<ICorrelationContext>(),
                sp.GetRequiredService<ILogger<LoggingDecorator<TRequest, TResult>>>());
            return useCase;
        });
        return services;
    }
}
