using System;
using System.Threading;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Configuration;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Infrastructure.Outbox;
using Contpaq.Bridge.Infrastructure.Persistence;
using Contpaq.Bridge.Infrastructure.Sdk;
using Contpaq.Bridge.Tests.Soporte;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Contpaq.Bridge.Tests.Sdk
{
    /// <summary>Vigilante de tiempo límite por llamada nativa (L1-T003, FR-005, R-07), con una llamada falsa que no regresa.</summary>
    public class VigilanteTests
    {
        [Fact]
        public async Task Una_llamada_que_no_regresa_dispara_el_aviso_con_la_llamada_y_la_transaccion_en_curso()
        {
            var nativo = new SdkNativoFalso { BloquearAbreEmpresa = new ManualResetEventSlim(false) };
            var vigilante = new VigilanteSdk(TimeSpan.FromMilliseconds(100), TimeProvider.System)
            {
                Contexto = new ContextoLlamada("tx-1", "corr-1"),
            };
            var aviso = new TaskCompletionSource<(string Llamada, ContextoLlamada? Contexto)>();
            vigilante.AlVencer = (llamada, contexto) => aviso.TrySetResult((llamada, contexto));
            var vigilado = new SdkNativoVigilado(nativo, vigilante);

            var llamada = Task.Run(() => vigilado.AbreEmpresa(@"C:\Empresa"));

            // El vigilante debe avisar mientras la llamada sigue bloqueada.
            var (nombre, contexto) = await aviso.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(llamada.IsCompleted, "la llamada nativa sigue bloqueada: no se aborta");
            Assert.Equal("AbreEmpresa", nombre);
            Assert.Equal("corr-1", contexto!.CorrelationId);

            nativo.BloquearAbreEmpresa.Set();
            await llamada.WaitAsync(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Una_llamada_que_regresa_a_tiempo_no_avisa()
        {
            var nativo = new SdkNativoFalso();
            var avisos = 0;
            var vigilante = new VigilanteSdk(TimeSpan.FromMilliseconds(300), TimeProvider.System) { AlVencer = (_, _) => Interlocked.Increment(ref avisos) };
            var vigilado = new SdkNativoVigilado(nativo, vigilante);

            for (var i = 0; i < 20; i++) vigilado.AbreEmpresa("x");
            Thread.Sleep(500);

            Assert.Equal(0, avisos);
            Assert.Equal(20, nativo.Veces("AbreEmpresa"));
        }

        private static BridgeOptions Opciones(int maxRetries) => BridgeOptions.Leer(
            new ConfigurationBuilder().AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
            {
                ["BridgeConfig:Mode"] = "Simulated",
                ["BridgeConfig:MaxRetries"] = maxRetries.ToString(System.Globalization.CultureInfo.InvariantCulture),
            }).Build());

        private sealed class Banco
        {
            public Entorno E { get; } = new();
            public CierreProcesoFalso Proceso { get; } = new();
            public VigilanteSdk Vigilante { get; } = new(TimeSpan.FromSeconds(60), TimeProvider.System);
            public OutboxWorker Worker { get; }

            public Banco(int maxRetries = 3)
            {
                var gateway = new Mock<ISdkGateway>();
                Worker = new OutboxWorker(E.Outbox, gateway.Object, E.Validador, E.Callbacks,
                    Opciones(maxRetries), new CircuitBreakerPolicy(100, 1), NullLogger<OutboxWorker>.Instance,
                    Proceso, null, Vigilante);
            }
        }

        [Fact]
        public async Task Al_vencer_la_transaccion_en_curso_responde_SDK_TIMEOUT_reintentable_y_el_proceso_sale_distinto_de_cero()
        {
            var b = new Banco();
            var id = await b.E.EncolarAsync("alta-pedido.valido.json");
            var tx = (await b.E.Outbox.GetByIdAsync(id))!;

            b.Worker.ManejarTimeout("AbreEmpresa", new ContextoLlamada(tx.TransactionId, tx.CorrelationId));

            var despues = (await b.E.Outbox.GetByIdAsync(id))!;
            Assert.Equal(Estados.Pending, despues.Status); // reintentable: se vuelve a intentar con el proceso nuevo
            Assert.Equal(1, despues.RetryCount);
            Assert.Contains("SDK_TIMEOUT", despues.ErrorJson);
            Assert.Contains(tx.CorrelationId, despues.ErrorJson); // queda el correlation_id del bloqueo
            Assert.Equal([VigilanteSdk.CodigoSalidaTimeout], b.Proceso.Codigos);
            Assert.NotEqual(0, VigilanteSdk.CodigoSalidaTimeout);
        }

        [Fact]
        public async Task Agotados_los_reintentos_el_timeout_va_a_la_cola_de_cartas_muertas()
        {
            var b = new Banco(maxRetries: 1);
            var id = await b.E.EncolarAsync("alta-pedido.valido.json");
            var tx = (await b.E.Outbox.GetByIdAsync(id))!;

            b.Worker.ManejarTimeout("AbreEmpresa", new ContextoLlamada(tx.TransactionId, tx.CorrelationId));

            Assert.Equal(Estados.DeadLetter, (await b.E.Outbox.GetByIdAsync(id))!.Status);
        }

        [Fact]
        public void Un_bloqueo_fuera_de_una_transaccion_igual_saca_el_proceso()
        {
            var b = new Banco();

            b.Worker.ManejarTimeout("InicioSesionSdk", null);

            Assert.Equal([VigilanteSdk.CodigoSalidaTimeout], b.Proceso.Codigos);
        }
    }
}
