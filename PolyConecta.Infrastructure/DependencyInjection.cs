using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Folios;
using PolyConecta.Infrastructure.Plataforma;
using PolyConecta.Infrastructure.Erp;
using PolyConecta.Application.Plataforma.Erp;
using PolyConecta.Domain.Common;
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

        services.AddScoped<AuditoriaInterceptor>();
        services.AddDbContext<PolyDbContext>((sp, options) =>
            options.UseSqlServer(conexion).AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>()));
        services.AddScoped<IReferenceSequenceService, ReferenceSequenceService>();

        // Bridge (D-122, CT-03): el despachador solo arranca si hay Erp__BridgeUrl.
        services.Configure<ErpOptions>(configuration.GetSection(ErpOptions.Seccion));
        services.AddSingleton<RegistroDocumentosSincronizables>();
        services.AddScoped<IBridgeSyncService, BridgeSyncService>();
        services.AddScoped<IOutboxStore, OutboxStore>();
        services.AddScoped<IDocumentosSincronizables, DocumentosSincronizables>();
        services.AddHttpClient<BridgeHttpClient>((sp, http) =>
        {
            var url = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ErpOptions>>().Value.BridgeUrl;
            http.BaseAddress = new Uri(string.IsNullOrWhiteSpace(url) ? "http://localhost:5005" : url);
            http.Timeout = TimeSpan.FromSeconds(30);
        });
        if (!string.IsNullOrWhiteSpace(configuration[$"{ErpOptions.Seccion}:BridgeUrl"]))
            services.AddHostedService<BridgeDispatcher>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<CorrelationContext>();
        services.AddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ICurrentUser, SistemaCurrentUser>();
        return services;
    }

    /// <summary>Registra un documento que escribe en CONTPAQi y su traductor al contrato.</summary>
    public static IServiceCollection AddDocumentoSincronizable<TDocumento, TTraductor>(this IServiceCollection services)
        where TDocumento : class, ISyncedDocument
        where TTraductor : class, ICommandPayloadTranslator
    {
        services.AddSingleton<ICommandPayloadTranslator, TTraductor>();
        services.AddSingleton(new DocumentoRegistrado(typeof(TDocumento)));
        return services;
    }
}
