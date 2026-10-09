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
using PolyConecta.Infrastructure.Persistence.Sembradores;
using PolyConecta.Infrastructure.Plataforma.Identidad;

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

        // Identity solo para credenciales (R-01): contraseña de 8 con mayúscula y número, bloqueo de 15
        // minutos tras 5 intentos fallidos. Grupos y permisos son del dominio.
        services.AddIdentityCore<CredencialUsuario>(o =>
            {
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = true;
                o.Password.RequireDigit = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                o.Lockout.AllowedForNewUsers = true;
                o.User.AllowedUserNameCharacters += "ñÑ";
            })
            .AddEntityFrameworkStores<PolyDbContext>();
        services.AddScoped<IDataSeeder, SembradorSeguridad>();
        services.AddScoped<IDataSeeder, DatosR1>();
        services.AddScoped<IDataSeeder, PedidoRepresentativo>();
        services.AddScoped<PolyConecta.Application.Plataforma.Seguridad.ICredenciales, Credenciales>();

        // Bridge (D-122, CT-03): el despachador solo arranca si hay Erp__BridgeUrl.
        services.Configure<ErpOptions>(configuration.GetSection(ErpOptions.Seccion));
        services.AddSingleton<RegistroDocumentosSincronizables>();
        services.AddScoped<IBridgeSyncService, BridgeSyncService>();
        services.AddScoped<IOutboxStore, OutboxStore>();
        services.AddScoped<IDocumentosSincronizables, DocumentosSincronizables>();
        services.AddHttpClient<BridgeHttpClient>((sp, http) =>
        {
            var url = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ErpOptions>>().Value.BridgeUrl;
            http.BaseAddress = new Uri(string.IsNullOrWhiteSpace(url) ? "http://localhost:9030" : url);
            http.Timeout = TimeSpan.FromSeconds(30);
        });
        // Lecturas de catálogos del contrato §6 (R-05), con la misma URL del bridge.
        services.AddHttpClient<IBridgeLecturas, BridgeLecturasHttp>((sp, http) =>
        {
            var url = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ErpOptions>>().Value.BridgeUrl;
            http.BaseAddress = new Uri(string.IsNullOrWhiteSpace(url) ? "http://localhost:9030" : url);
            http.Timeout = TimeSpan.FromSeconds(60);
        });
        services.Configure<OpcionesSincronizacion>(configuration.GetSection(OpcionesSincronizacion.Seccion));
        if (!string.IsNullOrWhiteSpace(configuration[$"{ErpOptions.Seccion}:BridgeUrl"]))
        {
            services.AddHostedService<BridgeDispatcher>();
            services.AddHostedService<SincronizadorCatalogos>();
        }
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<CorrelationContext>();
        services.AddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());
        services.AddSingleton<IClock, SystemClock>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserDesdeCookie>();
        services.AddAlmacenes();
        return services;
    }

    /// <summary>Permisos del usuario, reglas de fila y almacenes genéricos de los agregados (R-02).</summary>
    public static IServiceCollection AddAlmacenes(this IServiceCollection services)
    {
        services.AddScoped<IPermisosDelUsuario, PermisosDelUsuario>();
        services.AddScoped(typeof(PolyConecta.Application.Plataforma.Seguridad.ReglasDeFila<>));
        services.AddScoped(typeof(IAlmacen<>), typeof(PolyConecta.Infrastructure.Persistence.Almacenes.AlmacenEf<>));
        services.AddScoped<PolyConecta.Infrastructure.Persistence.Almacenes.IIncluirEnAlmacen<Domain.Inventario.Product>, Plataforma.Sincronizacion.IncluirProducto>();
        services.AddScoped<PolyConecta.Infrastructure.Persistence.Almacenes.IIncluirEnAlmacen<Domain.Ventas.Customer>, Plataforma.Sincronizacion.IncluirCliente>();
        services.AddScoped<PolyConecta.Infrastructure.Persistence.Almacenes.IIncluirEnAlmacen<Domain.Ventas.SalesOrder>, Plataforma.Sincronizacion.IncluirPedido>();
        services.AddSingleton<Application.Ventas.IConfiguracionDeMonedas, Plataforma.Sincronizacion.ConfiguracionDeMonedas>();
        services.AddScoped<Application.Plataforma.Sincronizacion.ICandadoDeSincronizacion, Plataforma.Sincronizacion.CandadoSqlServer>();
        services.AddScoped<Application.Plataforma.Sincronizacion.IEstadosDeSincronizacion, Plataforma.Sincronizacion.EstadosDeSincronizacion>();
        services.AddScoped<Application.Plataforma.Listas.IFavoritos, Plataforma.Listas.FavoritosEf>();
        services.AddScoped<Application.Plataforma.Chatter.IMensajesChatter, Plataforma.Chatter.MensajesChatterEf>();
        services.AddListas();
        return services;
    }

    /// <summary>Registro de las cinco listas de F1 (contracts/api-listas.md, D-151).</summary>
    public static IServiceCollection AddListas(this IServiceCollection services)
    {
        services.AddScoped(typeof(Plataforma.Listas.ConsultaDeListaEf<>));
        services.AddScoped<Plataforma.Listas.IIncluirEnLista<Domain.Ventas.SalesOrder>, Plataforma.Listas.IncluirPedidoEnLista>();
        services.AddScoped<Plataforma.Listas.IIncluirEnLista<Domain.Inventario.Product>, Plataforma.Listas.IncluirProductoEnLista>();
        services.AddScoped<Plataforma.Listas.IIncluirEnLista<Domain.Plataforma.Seguridad.User>, Plataforma.Listas.IncluirUsuarioEnLista>();

        services.AddScoped<PolyConecta.Application.Common.Listas.IConsultaDeLista>(sp =>
            ActivatorUtilities.CreateInstance<Plataforma.Listas.ConsultaDeListaEf<Domain.Ventas.SalesOrder>>(sp, PolyConecta.Application.Common.Listas.VistasDeF1.Pedidos));
        services.AddScoped<PolyConecta.Application.Common.Listas.IConsultaDeLista<Domain.Ventas.SalesOrder>>(sp =>
            ActivatorUtilities.CreateInstance<Plataforma.Listas.ConsultaDeListaEf<Domain.Ventas.SalesOrder>>(sp, PolyConecta.Application.Common.Listas.VistasDeF1.Pedidos));

        services.AddScoped<PolyConecta.Application.Common.Listas.IConsultaDeLista>(sp =>
            ActivatorUtilities.CreateInstance<Plataforma.Listas.ConsultaDeListaEf<Domain.Inventario.Product>>(sp, PolyConecta.Application.Common.Listas.VistasDeF1.Productos));
        services.AddScoped<PolyConecta.Application.Common.Listas.IConsultaDeLista<Domain.Inventario.Product>>(sp =>
            ActivatorUtilities.CreateInstance<Plataforma.Listas.ConsultaDeListaEf<Domain.Inventario.Product>>(sp, PolyConecta.Application.Common.Listas.VistasDeF1.Productos));

        services.AddScoped<PolyConecta.Application.Common.Listas.IConsultaDeLista>(sp =>
            ActivatorUtilities.CreateInstance<Plataforma.Listas.ConsultaDeListaEf<Domain.Ventas.Customer>>(sp, PolyConecta.Application.Common.Listas.VistasDeF1.Clientes));
        services.AddScoped<PolyConecta.Application.Common.Listas.IConsultaDeLista<Domain.Ventas.Customer>>(sp =>
            ActivatorUtilities.CreateInstance<Plataforma.Listas.ConsultaDeListaEf<Domain.Ventas.Customer>>(sp, PolyConecta.Application.Common.Listas.VistasDeF1.Clientes));

        services.AddScoped<PolyConecta.Application.Common.Listas.IConsultaDeLista>(sp =>
            ActivatorUtilities.CreateInstance<Plataforma.Listas.ConsultaDeListaEf<Domain.Plataforma.Seguridad.User>>(sp, PolyConecta.Application.Common.Listas.VistasDeF1.Usuarios));
        services.AddScoped<PolyConecta.Application.Common.Listas.IConsultaDeLista<Domain.Plataforma.Seguridad.User>>(sp =>
            ActivatorUtilities.CreateInstance<Plataforma.Listas.ConsultaDeListaEf<Domain.Plataforma.Seguridad.User>>(sp, PolyConecta.Application.Common.Listas.VistasDeF1.Usuarios));

        services.AddScoped<PolyConecta.Application.Common.Listas.IConsultaDeLista>(sp =>
            ActivatorUtilities.CreateInstance<Plataforma.Listas.ConsultaDeListaEf<Domain.Plataforma.Seguridad.Group>>(sp, PolyConecta.Application.Common.Listas.VistasDeF1.Grupos));
        services.AddScoped<PolyConecta.Application.Common.Listas.IConsultaDeLista<Domain.Plataforma.Seguridad.Group>>(sp =>
            ActivatorUtilities.CreateInstance<Plataforma.Listas.ConsultaDeListaEf<Domain.Plataforma.Seguridad.Group>>(sp, PolyConecta.Application.Common.Listas.VistasDeF1.Grupos));

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
