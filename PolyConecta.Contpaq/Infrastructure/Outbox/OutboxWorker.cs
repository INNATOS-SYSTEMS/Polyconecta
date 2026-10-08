using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Configuration;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Core.Models;
using Contpaq.Bridge.Core.Services;
using Contpaq.Bridge.Core.Validation;
using Contpaq.Bridge.Infrastructure.Persistence;
using Contpaq.Bridge.Infrastructure.Sdk;
using Contpaq.Bridge.Infrastructure.Webhooks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Contpaq.Bridge.Infrastructure.Outbox
{
    /// <summary>
    /// Ciclo del outbox del bridge, separado del gateway (D-122): toma las transacciones en orden de
    /// llegada y una a la vez (CT-41), valida (CT-39), ejecuta en el gateway del modo activo,
    /// guarda el resultado y avisa por callback. Corre en un solo hilo, STA en Windows, porque el
    /// SDK real lo exige; es el mismo código en modo real y simulado.
    /// </summary>
    public sealed partial class OutboxWorker(
        IOutboxRepository repo,
        ISdkGateway gateway,
        ValidadorComandos validador,
        IWebhookDispatcher callbacks,
        BridgeOptions opciones,
        CircuitBreakerPolicy circuito,
        ILogger<OutboxWorker> logger,
        ICierreProceso? proceso = null,
        ReinicioDiario? reinicio = null,
        VigilanteSdk? vigilante = null) : BackgroundService
    {
        // Una llamada nativa venció: la transacción ya se respondió y el proceso está por salir; no se sigue.
        private volatile bool _timeoutDisparado;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tcs = new TaskCompletionSource();
            var hilo = new Thread(() =>
            {
                try
                {
                    Ciclo(stoppingToken);
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            })
            { IsBackground = true, Name = "bridge-outbox" };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                hilo.SetApartmentState(ApartmentState.STA);
            hilo.Start();
            return tcs.Task;
        }

        private void Ciclo(CancellationToken stoppingToken)
        {
            LogInicio(logger, gateway.EsReal ? "Real" : "Simulated");
            if (vigilante is not null) vigilante.AlVencer = ManejarTimeout;
            // El SDK se inicia una vez al arrancar (R-07); si falla, AsegurarSesion lo reintenta con cada lote.
            if (!gateway.IniciarSdk()) LogSdkNoIniciado(logger);
            var reiniciar = false;
            while (!stoppingToken.IsCancellationRequested)
            {
                if (reinicio?.Debido() == true)
                {
                    LogReinicio(logger, reinicio.Hora.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture));
                    reiniciar = true;
                    break;
                }
                gateway.BombearMensajes();
                try
                {
                    MetricCollectorService.CircuitState = circuito.State;
                    if (!circuito.AllowExecution())
                    {
                        Thread.Sleep(2000);
                        continue;
                    }

                    var pendientes = repo.GetPendingTransactionsAsync(10).GetAwaiter().GetResult().ToList();
                    if (pendientes.Count == 0)
                    {
                        gateway.CerrarSiInactiva();
                        Thread.Sleep(200);
                        continue;
                    }

                    if (!gateway.AsegurarSesion())
                    {
                        circuito.RecordFailure();
                        Thread.Sleep(3000);
                        continue;
                    }
                    MetricCollectorService.IsSdkSessionActive = gateway.SesionActiva;

                    foreach (var tx in pendientes)
                    {
                        if (stoppingToken.IsCancellationRequested) break;
                        if (reinicio?.Debido() == true) break; // termina la actual, no toma otra
                        Procesar(tx);
                        PerformanceMetrics.RecordWriteOp();
                    }
                }
                catch (Exception ex)
                {
                    LogErrorCiclo(logger, ex);
                    circuito.RecordFailure();
                    Thread.Sleep(2000);
                }
            }
            gateway.Apagar();
            if (reiniciar) proceso?.Salir(0); // el supervisor del VPS lo vuelve a levantar
        }

        /// <summary>
        /// Una llamada nativa no regresó en el tiempo límite (FR-005). Desde el hilo del vigilante: registra el
        /// bloqueo con su correlation_id, responde SDK_TIMEOUT a la transacción en curso (reintentable) y saca
        /// el proceso con código distinto de 0. El hilo bloqueado en la DLL no se puede abortar sin dejar el SDK
        /// en estado desconocido.
        /// </summary>
        public void ManejarTimeout(string llamada, ContextoLlamada? contexto)
        {
            _timeoutDisparado = true;
            LogTimeout(logger, llamada, contexto?.CorrelationId ?? "(sin transacción)", vigilante?.Limite.TotalSeconds ?? 0);
            try
            {
                if (contexto is not null && repo.GetByIdAsync(contexto.TransactionId).GetAwaiter().GetResult() is { } tx)
                    ResponderTimeout(tx, llamada);
            }
            catch (Exception ex)
            {
                LogErrorCiclo(logger, ex);
            }
            proceso?.Salir(VigilanteSdk.CodigoSalidaTimeout);
        }

        /// <summary>Responde SDK_TIMEOUT a una transacción: se reintenta tras el proceso nuevo o, agotados los reintentos, va a la DLQ.</summary>
        public void ResponderTimeout(BridgeTransaction tx, string llamada)
        {
            var error = ErrorContrato.De(CodigosError.SdkTimeout,
                $"La llamada {llamada} al SDK no regresó en {vigilante?.Limite.TotalSeconds ?? 0:0} s.",
                new() { ["llamada"] = llamada, ["correlation_id"] = tx.CorrelationId });
            var ms = (long)(vigilante?.Limite.TotalMilliseconds ?? 0);
            Registrar(tx, ResultadoEjecucion.Fallo(error), ms);
            ManejarFalloReintentable(tx, error, omitirCallback: false);
        }

        /// <summary>Procesa una transacción. Público para las pruebas, que lo llaman sin el hilo.</summary>
        public void Procesar(BridgeTransaction tx)
        {
            repo.MarcarProcesandoAsync(tx.TransactionId).GetAwaiter().GetResult();
            var reloj = Stopwatch.StartNew();
            ResultadoEjecucion resultado;
            if (vigilante is not null) vigilante.Contexto = new ContextoLlamada(tx.TransactionId, tx.CorrelationId);
            try
            {
                var (comando, errorLectura) = LectorComandos.Leer(Reconstruir(tx));
                var errorValidacion = errorLectura ?? validador.ValidarAsync(comando!).GetAwaiter().GetResult();
                resultado = errorValidacion is not null
                    ? ResultadoEjecucion.Fallo(errorValidacion)
                    : gateway.Ejecutar(tx.TransactionId, comando!);
            }
            catch (Exception ex)
            {
                resultado = ResultadoEjecucion.Fallo(ErrorContrato.De(CodigosError.SdkError, ex.Message,
                    new() { ["excepcion"] = ex.GetType().Name }));
            }
            finally
            {
                if (vigilante is not null) vigilante.Contexto = null;
            }
            if (_timeoutDisparado) return; // el vigilante ya respondió la transacción
            reloj.Stop();
            MetricCollectorService.AverageSdkLatencyMs = reloj.ElapsedMilliseconds;
            Registrar(tx, resultado, reloj.ElapsedMilliseconds);

            if (resultado.Error is null)
            {
                circuito.RecordSuccess();
                Terminar(tx, Estados.Confirmed, JsonSerializer.Serialize(resultado.Resultado), null, null, resultado.OmitirCallback);
                return;
            }

            var error = resultado.Error;
            var errorJson = JsonSerializer.Serialize(error);
            if (!error.Retryable)
            {
                Terminar(tx, Estados.Failed, null, errorJson, error.Message, resultado.OmitirCallback);
                return;
            }

            ManejarFalloReintentable(tx, error, resultado.OmitirCallback);
        }

        /// <summary>Reintentable (SDK_TIMEOUT, SDK_SESION): no se notifica hasta agotar los reintentos (§3).</summary>
        private void ManejarFalloReintentable(BridgeTransaction tx, ErrorContrato error, bool omitirCallback)
        {
            var errorJson = JsonSerializer.Serialize(error);
            circuito.RecordFailure();
            var intento = tx.RetryCount + 1;
            if (intento >= opciones.MaxRetries)
            {
                Terminar(tx, Estados.DeadLetter, null, errorJson, error.Message, omitirCallback);
                return;
            }
            repo.ProgramarReintentoAsync(tx.TransactionId, intento, DateTime.UtcNow.Add(Espera(intento)), errorJson, error.Message)
                .GetAwaiter().GetResult();
        }

        private void Terminar(BridgeTransaction tx, string estado, string? resultJson, string? errorJson, string? mensaje, bool omitirCallback)
        {
            repo.CompletarAsync(tx.TransactionId, estado, resultJson, errorJson, mensaje).GetAwaiter().GetResult();
            if (omitirCallback) return;
            var actualizada = repo.GetByIdAsync(tx.TransactionId).GetAwaiter().GetResult()!;
            // El callback no bloquea la cola: sus reintentos corren aparte.
            _ = Task.Run(() => callbacks.DeliverAsync(actualizada));
        }

        private void Registrar(BridgeTransaction tx, ResultadoEjecucion resultado, long ms) =>
            repo.AddLogAsync(new TransactionLog
            {
                TransactionId = tx.TransactionId,
                CorrelationId = tx.CorrelationId,
                AttemptNumber = tx.RetryCount + 1,
                SdkFunctionName = tx.CommandType,
                SdkErrorCode = resultado.Error is null ? 0 : -1,
                ErrorMessage = resultado.Error is null ? null : $"{resultado.Error.Code}: {resultado.Error.Message}",
                DurationMs = ms,
            }).GetAwaiter().GetResult();

        /// <summary>Espera antes del siguiente intento: 1, 5, 15 y 60 segundos.</summary>
        public static TimeSpan Espera(int intento) => TimeSpan.FromSeconds(intento switch
        {
            1 => 1,
            2 => 5,
            3 => 15,
            _ => 60,
        });

        private static ComandoRequest Reconstruir(BridgeTransaction tx) => new()
        {
            ContractVersion = tx.ContractVersion,
            CommandType = tx.CommandType,
            Variant = tx.Variant,
            IdempotencyKey = tx.IdempotencyKey,
            CorrelationId = tx.CorrelationId,
            ClientAppId = tx.ClientAppId,
            CallbackUrl = tx.CallbackUrl,
            Payload = JsonDocument.Parse(tx.PayloadJson).RootElement.Clone(),
        };

        [LoggerMessage(Level = LogLevel.Information, Message = "Ciclo del outbox del bridge iniciado (modo {Modo})")]
        private static partial void LogInicio(ILogger logger, string modo);

        [LoggerMessage(Level = LogLevel.Error, Message = "El SDK no se pudo iniciar al arrancar; se reintenta con cada lote")]
        private static partial void LogSdkNoIniciado(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information, Message = "Reinicio diario ({Hora}): ya no se toman transacciones; se cierra el SDK y el proceso sale con código 0")]
        private static partial void LogReinicio(ILogger logger, string hora);

        [LoggerMessage(Level = LogLevel.Critical, Message = "SDK_TIMEOUT: la llamada {Llamada} no regresó en {Segundos} s (correlation_id {CorrelationId}); el proceso sale para que lo relance el supervisor del VPS")]
        private static partial void LogTimeout(ILogger logger, string llamada, string correlationId, double segundos);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error no controlado en el ciclo del outbox")]
        private static partial void LogErrorCiclo(ILogger logger, Exception ex);
    }
}
