using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Erp;
using PolyConecta.Domain.Plataforma;
using PolyConecta.Infrastructure.Common;
using PolyConecta.Infrastructure.Persistence;

namespace PolyConecta.Infrastructure.Erp;

/// <summary>
/// Despachador del outbox hacia el bridge (CT-20, CT-41, D-95, research R-04 de la spec 002):
/// - envía en orden de secuencia y de uno en uno;
/// - un comando espera a los anteriores que comparten producto o almacén con él;
/// - un comando en Error deja Bloqueado solo a los posteriores que comparten llave;
/// - las fallas de red o 5xx se reintentan con espera de 2ⁿ y, al agotarse, dejan Error;
/// - si el callback no llega a tiempo, consulta GET /transactions/{id}.
/// Un bloqueo de aplicación en SQL Server (sp_getapplock) asegura un solo despachador activo.
/// </summary>
public sealed partial class BridgeDispatcher(IServiceScopeFactory scopes, IOptions<ErpOptions> opciones, ILogger<BridgeDispatcher> logger)
    : BackgroundService
{
    private const string Recurso = "polyconecta-despachador-bridge";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await CicloAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                LogErrorCiclo(logger, ex);
            }
            await Task.Delay(opciones.Value.IntervaloMs, stoppingToken);
        }
    }

    /// <summary>Un ciclo completo. Público para las pruebas, que lo invocan sin el temporizador.</summary>
    public async Task CicloAsync(IServiceProvider servicios, CancellationToken cancellationToken)
    {
        var db = servicios.GetRequiredService<PolyDbContext>();
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            if (!await TomarBloqueoAsync(db, cancellationToken)) return;
            try
            {
                await ProcesarAsync(servicios, db, cancellationToken);
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync(
                    "EXEC sp_releaseapplock @Resource = {0}, @LockOwner = 'Session'", [Recurso], CancellationToken.None);
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private async Task ProcesarAsync(IServiceProvider servicios, PolyDbContext db, CancellationToken cancellationToken)
    {
        var http = servicios.GetRequiredService<BridgeHttpClient>();
        var confirmar = servicios.GetRequiredService<IUseCase<ResultadoBridge, EfectoCallback>>();
        var documentos = servicios.GetRequiredService<IDocumentosSincronizables>();
        var correlacion = servicios.GetRequiredService<CorrelationContext>();
        var clock = servicios.GetRequiredService<IClock>();
        var opc = opciones.Value;

        var activos = await db.OutboxMessages
            .Where(m => m.Status != OutboxStatus.Confirmado)
            .OrderBy(m => m.Sequence)
            .ToListAsync(cancellationToken);

        // 1. Callbacks que no llegaron: se consulta la transacción en el bridge.
        foreach (var m in activos.Where(m => m.Status == OutboxStatus.Enviado && m.SentAt + TimeSpan.FromSeconds(opc.CallbackTimeoutSegundos) < clock.Now))
        {
            correlacion.CorrelationId = m.CorrelationId;
            await ConsultarYAplicarAsync(http, confirmar, m, cancellationToken);
        }

        // 2. Bloqueos por llave y envíos, en orden de secuencia.
        var anteriores = new List<OutboxMessage>();
        foreach (var m in activos)
        {
            if (m.Status is OutboxStatus.Pendiente or OutboxStatus.Bloqueado)
            {
                var comparten = anteriores.Where(a => a.ComparteLlaves(m)).ToList();
                if (comparten.Any(a => a.Status is OutboxStatus.Error or OutboxStatus.Bloqueado))
                {
                    m.Bloquear();
                }
                else
                {
                    m.Desbloquear();
                    var esperaAnterior = comparten.Any(a => a.Status is OutboxStatus.Pendiente or OutboxStatus.Enviado);
                    var listo = m.NextAttemptAt is null || m.NextAttemptAt <= clock.Now;
                    if (!esperaAnterior && listo && opc.Habilitado(m.CommandType))
                    {
                        correlacion.CorrelationId = m.CorrelationId;
                        await EnviarAsync(http, confirmar, documentos, clock, m, opc.MaxIntentos, cancellationToken);
                    }
                }
            }
            anteriores.Add(m);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnviarAsync(
        BridgeHttpClient http, IUseCase<ResultadoBridge, EfectoCallback> confirmar, IDocumentosSincronizables documentos,
        IClock clock, OutboxMessage m, int maxIntentos, CancellationToken cancellationToken)
    {
        var documento = await documentos.BuscarAsync(m.DocumentType, m.DocumentId, cancellationToken);
        var ahora = clock.Now;
        switch (await http.EnviarAsync(m, cancellationToken))
        {
            case EnvioBridge.Aceptado a:
                m.MarcarEnviado(a.TransactionId, ahora);
                documento?.Sync.MarcarEnviado(ahora);
                // Reenvío de algo que el bridge ya terminó: el resultado se pide en el acto.
                if (a.EsDuplicado && a.Status is "CONFIRMED" or "FAILED" or "DEAD_LETTER")
                    await ConsultarYAplicarAsync(http, confirmar, m, cancellationToken);
                break;
            case EnvioBridge.Rechazado r:
                m.MarcarError(r.Codigo, r.Mensaje, ahora);
                documento?.Sync.MarcarError(r.Codigo, r.Mensaje, ahora);
                LogRechazado(logger, m.IdempotencyKey, r.Codigo, m.CorrelationId);
                break;
            case EnvioBridge.FallaTransitoria f:
                if (m.RegistrarFallaDeEnvio(f.Mensaje, ahora, maxIntentos))
                    documento?.Sync.MarcarError("ENVIO_FALLIDO", f.Mensaje, ahora);
                break;
        }
    }

    private static async Task ConsultarYAplicarAsync(
        BridgeHttpClient http, IUseCase<ResultadoBridge, EfectoCallback> confirmar, OutboxMessage m, CancellationToken cancellationToken)
    {
        if (m.BridgeTransactionId is null) return;
        using var cuerpo = await http.ConsultarAsync(m.BridgeTransactionId, cancellationToken);
        if (cuerpo is null) return;
        await confirmar.ExecuteAsync(ConfirmarSincronizacion.Leer(cuerpo.RootElement), cancellationToken);
    }

    private static async Task<bool> TomarBloqueoAsync(PolyDbContext db, CancellationToken cancellationToken)
    {
        var conexion = db.Database.GetDbConnection();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = "DECLARE @r int; EXEC @r = sp_getapplock @Resource = @recurso, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0; SELECT @r;";
        var p = comando.CreateParameter();
        p.ParameterName = "@recurso";
        p.Value = Recurso;
        p.DbType = DbType.String;
        comando.Parameters.Add(p);
        var r = Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture);
        return r >= 0;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error en el ciclo del despachador del bridge")]
    private static partial void LogErrorCiclo(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "El bridge rechazó {IdempotencyKey} con {Codigo} [{CorrelationId}]")]
    private static partial void LogRechazado(ILogger logger, string idempotencyKey, string codigo, string correlationId);
}
