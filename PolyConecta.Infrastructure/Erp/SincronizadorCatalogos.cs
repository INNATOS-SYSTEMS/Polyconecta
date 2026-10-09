using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Sincronizacion;

namespace PolyConecta.Infrastructure.Erp;

/// <summary>Sincronización periódica (sección Erp:Sincronizacion).</summary>
public sealed class OpcionesSincronizacion
{
    public const string Seccion = "Erp:Sincronizacion";

    /// <summary>Cada cuánto corre (15 por omisión, FR-014). Se sube si la lectura completa pasa de un minuto (R-05).</summary>
    public int IntervaloMinutos { get; set; } = 15;

    /// <summary>Espera antes de la primera corrida, para no competir con el arranque.</summary>
    public int EsperaInicialSegundos { get; set; } = 10;

    public bool Habilitada { get; set; } = true;
}

/// <summary>
/// Corre SincronizarTodo cada <see cref="OpcionesSincronizacion.IntervaloMinutos"/> como "sistema" (R-05). Una
/// falla queda en el estado de cada catálogo y se reintenta en la siguiente vuelta (US3, escenario 4).
/// </summary>
public sealed partial class SincronizadorCatalogos(
    IServiceScopeFactory scopes, IOptions<OpcionesSincronizacion> opciones, ILogger<SincronizadorCatalogos> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var o = opciones.Value;
        if (!o.Habilitada) return;
        await Task.Delay(TimeSpan.FromSeconds(o.EsperaInicialSegundos), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var estados = await scope.ServiceProvider.GetRequiredService<IUseCase<SincronizarTodo, IReadOnlyList<EstadoCatalogo>>>()
                    .ExecuteAsync(new SincronizarTodo(), stoppingToken);
                foreach (var e in estados)
                    LogCatalogo(logger, e.Catalogo, e.Resultado ?? "-", e.Leidos, e.Cambiados, e.Archivados, e.DuracionMs, e.Error);
                // El pedido representativo de R1 espera a que existan su cliente y su producto.
                foreach (var s in scope.ServiceProvider.GetServices<Persistence.IDataSeeder>().OfType<Persistence.Sembradores.PedidoRepresentativo>())
                    await s.SeedAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                LogFallo(logger, ex);
            }
            await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, o.IntervaloMinutos)), stoppingToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Sincronización de {Catalogo}: {Resultado}, {Leidos} leídos, {Cambiados} cambiados, {Archivados} archivados en {Ms} ms {Error}")]
    private static partial void LogCatalogo(ILogger logger, string catalogo, string resultado, int leidos, int cambiados, int archivados, long ms, string? error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Falló la sincronización periódica de catálogos")]
    private static partial void LogFallo(ILogger logger, Exception ex);
}
