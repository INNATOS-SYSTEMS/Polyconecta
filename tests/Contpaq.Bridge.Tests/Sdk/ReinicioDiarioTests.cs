using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Configuration;
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
    /// <summary>Reinicio diario (L1-T004, FR-006, R-07), con un <see cref="TimeProvider"/> falso.</summary>
    public class ReinicioDiarioTests
    {
        private static DateTimeOffset Hoy(int hora, int minuto) => new(2026, 10, 8, hora, minuto, 0, TimeSpan.Zero);

        [Fact]
        public void Por_omision_es_a_las_tres_de_la_manana_hora_local()
        {
            var reloj = new RelojFalso(Hoy(12, 0));
            var reinicio = new ReinicioDiario(null, reloj);

            Assert.Equal(new TimeOnly(3, 0), reinicio.Hora);
            Assert.False(reinicio.Debido());
            reloj.Avanzar(TimeSpan.FromHours(14)); // 02:00 del día siguiente
            Assert.False(reinicio.Debido());
            reloj.Avanzar(TimeSpan.FromHours(1)); // 03:00
            Assert.True(reinicio.Debido());
        }

        [Fact]
        public void Un_proceso_que_arranca_pasada_la_hora_espera_a_la_del_dia_siguiente()
        {
            // Lo relanzó el supervisor a las 03:00:05: no debe volver a salir enseguida.
            var reloj = new RelojFalso(Hoy(3, 0).AddSeconds(5));
            var reinicio = new ReinicioDiario("03:00", reloj);

            Assert.False(reinicio.Debido());
            reloj.Avanzar(TimeSpan.FromHours(23));
            Assert.False(reinicio.Debido());
            reloj.Avanzar(TimeSpan.FromHours(1));
            Assert.True(reinicio.Debido());
        }

        [Fact]
        public void La_hora_configurada_se_respeta_y_una_invalida_se_rechaza_al_arrancar()
        {
            var reloj = new RelojFalso(Hoy(10, 0));
            var reinicio = new ReinicioDiario("10:02", reloj);
            Assert.False(reinicio.Debido());
            reloj.Avanzar(TimeSpan.FromMinutes(2));
            Assert.True(reinicio.Debido());

            Assert.Throws<ArgumentException>(() => new ReinicioDiario("a las tres", reloj));
            Assert.Throws<ArgumentException>(() => new ReinicioDiario("25:00", reloj));
        }

        private static OutboxWorker Worker(Entorno e, Mock<ISdkGateway> gateway, ReinicioDiario reinicio, CierreProcesoFalso proceso) =>
            new(e.Outbox, gateway.Object, e.Validador, e.Callbacks,
                BridgeOptions.Leer(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["BridgeConfig:Mode"] = "Simulated" }).Build()),
                new CircuitBreakerPolicy(100, 1), NullLogger<OutboxWorker>.Instance, proceso, reinicio, null);

        [Fact]
        public async Task A_la_hora_el_worker_deja_de_tomar_transacciones_cierra_el_SDK_y_sale_con_cero()
        {
            var e = new Entorno();
            var reloj = new RelojFalso(Hoy(2, 59));
            var reinicio = new ReinicioDiario("03:00", reloj);
            var proceso = new CierreProcesoFalso();
            var gateway = new Mock<ISdkGateway>();
            var secuencia = new List<string>();
            gateway.Setup(g => g.IniciarSdk()).Returns(true);
            gateway.Setup(g => g.AsegurarSesion()).Returns(true);
            gateway.Setup(g => g.Apagar()).Callback(() => secuencia.Add("Apagar"));
            gateway.Setup(g => g.Ejecutar(It.IsAny<string>(), It.IsAny<Contpaq.Bridge.Core.Contract.ComandoLeido>()))
                .Returns(() =>
                {
                    secuencia.Add("Ejecutar");
                    reloj.Avanzar(TimeSpan.FromMinutes(2)); // la hora llega mientras la transacción está en curso
                    return Contpaq.Bridge.Core.Contract.ResultadoEjecucion.Exito(new Contpaq.Bridge.Core.Contract.Resultado { Folio = "1", IdErp = 1 });
                });

            var id1 = await e.EncolarAsync("alta-pedido.valido.json", "reinicio:1");
            var id2 = await e.EncolarAsync("alta-pedido.valido.json", "reinicio:2");
            var worker = Worker(e, gateway, reinicio, proceso);

            await worker.StartAsync(CancellationToken.None);
            await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal(["Ejecutar", "Apagar"], secuencia);
            Assert.Equal(Contpaq.Bridge.Core.Contract.Estados.Confirmed, (await e.Outbox.GetByIdAsync(id1))!.Status); // terminó la actual
            Assert.Equal(Contpaq.Bridge.Core.Contract.Estados.Pending, (await e.Outbox.GetByIdAsync(id2))!.Status); // no tomó la siguiente
            Assert.Equal([0], proceso.Codigos);
        }

        [Fact]
        public async Task Antes_de_la_hora_el_worker_sigue_y_al_cancelarlo_no_pide_salir()
        {
            var e = new Entorno();
            var reinicio = new ReinicioDiario("03:00", new RelojFalso(Hoy(12, 0)));
            var proceso = new CierreProcesoFalso();
            var gateway = new Mock<ISdkGateway>();
            gateway.Setup(g => g.IniciarSdk()).Returns(true);
            var worker = Worker(e, gateway, reinicio, proceso);
            using var cts = new CancellationTokenSource();

            await worker.StartAsync(cts.Token);
            await Task.Delay(300);
            await cts.CancelAsync();
            await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10));

            gateway.Verify(g => g.IniciarSdk(), Times.Once);
            gateway.Verify(g => g.Apagar(), Times.Once);
            Assert.Empty(proceso.Codigos);
        }
    }
}
