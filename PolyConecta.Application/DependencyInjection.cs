using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Erp;
using PolyConecta.Application.Plataforma.Seguridad;

namespace PolyConecta.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso de Application. Sin MediatR ni Scrutor (D-72, research R-02).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddUseCase<ResultadoBridge, EfectoCallback, ConfirmarSincronizacion>();
        services.AddUseCase<ReintentarSincronizacionRequest, Unit, ReintentarSincronizacion>();
        AddSeguridad(services);
        AddCatalogos(services);
        services.AddScoped<Plataforma.Sincronizacion.Sincronizador>();
        services.AddUseCase<Plataforma.Sincronizacion.SincronizarCatalogo, Plataforma.Sincronizacion.EstadoCatalogo, Plataforma.Sincronizacion.SincronizarCatalogoCaso>();
        services.AddUseCase<Plataforma.Sincronizacion.SincronizarTodo, IReadOnlyList<Plataforma.Sincronizacion.EstadoCatalogo>, Plataforma.Sincronizacion.SincronizarTodoCaso>();
        services.AddUseCase<Plataforma.Sincronizacion.EstadoDeSincronizacion, IReadOnlyList<Plataforma.Sincronizacion.EstadoCatalogo>, Plataforma.Sincronizacion.EstadoDeSincronizacionCaso>();
        return services;
    }

    private static void AddCatalogos(IServiceCollection services)
    {
        services.AddScoped<Inventario.DetalleDeProducto>();
        services.AddUseCase<Inventario.ObtenerProducto, Inventario.ProductoDetalle, Inventario.ObtenerProductoCaso>();
        services.AddUseCase<Inventario.BuscarProductos, IReadOnlyList<Inventario.ProductoBusqueda>, Inventario.BuscarProductosCaso>();
        services.AddUseCase<Inventario.ClasificarProducto, Inventario.ProductoDetalle, Inventario.ClasificarProductoCaso>();
        services.AddUseCase<Inventario.GuardarFichaTecnica, Inventario.ProductoDetalle, Inventario.GuardarFichaTecnicaCaso>();
        services.AddUseCase<Inventario.ListarClasificaciones, IReadOnlyList<Inventario.ClasificacionDto>, Inventario.ListarClasificacionesCaso>();
        services.AddUseCase<Inventario.GuardarClasificacion, Inventario.ClasificacionDto, Inventario.GuardarClasificacionCaso>();
        services.AddUseCase<Inventario.ListarAlmacenes, IReadOnlyList<Inventario.AlmacenDto>, Inventario.ListarAlmacenesCaso>();
        services.AddUseCase<Ventas.ObtenerCliente, Ventas.ClienteDetalle, Ventas.ObtenerClienteCaso>();
        services.AddUseCase<Ventas.BuscarClientes, IReadOnlyList<Ventas.ClienteBusqueda>, Ventas.BuscarClientesCaso>();
        services.AddUseCase<Ventas.ListarAgentes, IReadOnlyList<Ventas.AgenteDto>, Ventas.ListarAgentesCaso>();
    }

    private static void AddSeguridad(IServiceCollection services)
    {
        services.AddScoped<CatalogoDeSeguridad>();
        services.AddScoped<DetalleDeGrupo>();
        services.AddScoped<IValidator<CrearUsuario>, ValidarCrearUsuario>();
        services.AddScoped<IValidator<EditarUsuario>, ValidarEditarUsuario>();
        services.AddUseCase<ObtenerUsuario, UsuarioDetalle, ObtenerUsuarioCaso>();
        services.AddUseCase<CrearUsuario, UsuarioDetalle, CrearUsuarioCaso>();
        services.AddUseCase<EditarUsuario, UsuarioDetalle, EditarUsuarioCaso>();
        services.AddUseCase<ArchivarUsuario, UsuarioDetalle, ArchivarUsuarioCaso>();
        services.AddUseCase<RestablecerContrasena, Unit, RestablecerContrasenaCaso>();
        services.AddUseCase<LigarAgente, UsuarioDetalle, LigarAgenteCaso>();
        services.AddUseCase<ObtenerGrupo, GrupoDetalle, ObtenerGrupoCaso>();
        services.AddUseCase<ArbolDePermisos, IReadOnlyList<ModuloDePermiso>, ArbolDePermisosCaso>();
        services.AddUseCase<CrearGrupo, GrupoDetalle, CrearGrupoCaso>();
        services.AddUseCase<EditarGrupo, GrupoDetalle, EditarGrupoCaso>();
        services.AddUseCase<ArchivarGrupo, GrupoDetalle, ArchivarGrupoCaso>();
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
