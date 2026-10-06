using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Simulated;
using Contpaq.Bridge.Tests.Soporte;
using Xunit;

namespace Contpaq.Bridge.Tests.Simulated
{
    /// <summary>El ciclo del outbox con el gateway simulado (CT-21, FR-007, FR-008).</summary>
    public class CicloSimuladoTests
    {
        private readonly Entorno _e = new(maxRetries: 3);

        [Fact]
        public async Task Un_traspaso_valido_confirma_con_salida_y_entrada_y_avisa()
        {
            var tx = await _e.ProcesarAsync(await _e.EncolarAsync("traspaso.valido.json"));

            tx.Status.Should().Be(Estados.Confirmed);
            var r = JsonSerializer.Deserialize<Resultado>(tx.ResultJson!)!;
            r.Documentos!.Select(d => (d.Rol, d.Concepto)).Should().Equal(("salida", "SAL-RECOL"), ("entrada", "ENT-RECOL"));
            _e.Callbacks.Enviados.Should().ContainSingle(c => c.TransactionId == tx.TransactionId && c.Status == Estados.Confirmed);
        }

        [Fact]
        public async Task Los_folios_son_consecutivos_por_concepto()
        {
            var a = await _e.ProcesarAsync(await _e.EncolarAsync("alta-pedido.valido.json", "pedido:1:autorizar"));
            var b = await _e.ProcesarAsync(await _e.EncolarAsync("alta-pedido.valido.json", "pedido:2:autorizar"));

            JsonSerializer.Deserialize<Resultado>(a.ResultJson!)!.Folio.Should().Be("1");
            JsonSerializer.Deserialize<Resultado>(b.ResultJson!)!.Folio.Should().Be("2");
        }

        [Fact]
        public async Task Una_carga_que_no_valida_falla_sin_reintentos_con_su_codigo()
        {
            var tx = await _e.ProcesarAsync(await _e.EncolarAsync("remision.invalido.existencia-insuficiente-lote.json"));

            tx.Status.Should().Be(Estados.Failed);
            JsonSerializer.Deserialize<ErrorContrato>(tx.ErrorJson!)!.Code.Should().Be(CodigosError.ExistenciaInsuficienteLote);
            _e.Callbacks.Enviados.Should().ContainSingle(c => c.Status == Estados.Failed);
        }

        [Fact]
        public async Task Un_error_reintentable_no_avisa_hasta_agotar_los_reintentos_y_termina_en_dead_letter()
        {
            _e.Fallos.Reemplazar([new FaultRule { CommandType = Comandos.AltaPedido, ErrorCode = CodigosError.SdkTimeout }]);
            var id = await _e.EncolarAsync("alta-pedido.valido.json");

            (await _e.ProcesarAsync(id)).Status.Should().Be(Estados.Pending);
            (await _e.ProcesarAsync(id)).Status.Should().Be(Estados.Pending);
            _e.Callbacks.Enviados.Should().BeEmpty();

            var final = await _e.ProcesarAsync(id);
            final.Status.Should().Be(Estados.DeadLetter);
            final.RetryCount.Should().Be(2);
            _e.Callbacks.Enviados.Should().ContainSingle(c => c.Status == Estados.DeadLetter);
        }

        [Fact]
        public async Task Un_fallo_limitado_a_una_vez_se_recupera_en_el_reintento()
        {
            _e.Fallos.Reemplazar([new FaultRule { CommandType = Comandos.Traspaso, ErrorCode = CodigosError.SdkSesion, Veces = 1 }]);
            var id = await _e.EncolarAsync("traspaso.valido.json");

            (await _e.ProcesarAsync(id)).Status.Should().Be(Estados.Pending);
            (await _e.ProcesarAsync(id)).Status.Should().Be(Estados.Confirmed);
        }

        [Fact]
        public async Task Un_callback_perdido_no_se_envia_pero_la_consulta_devuelve_el_resultado()
        {
            _e.Fallos.Reemplazar([new FaultRule { CommandType = Comandos.Remision, DropCallback = true }]);
            var tx = await _e.ProcesarAsync(await _e.EncolarAsync("remision.valido.json"));

            tx.Status.Should().Be(Estados.Confirmed);
            _e.Callbacks.Enviados.Should().BeEmpty();
            var cuerpo = CuerpoCallback.Desde(tx);
            cuerpo["status"]!.GetValue<string>().Should().Be(Estados.Confirmed);
            cuerpo["result"]!["pedido_cancelado"]!.GetValue<bool>().Should().BeTrue();
        }

        [Fact]
        public async Task Un_fallo_puede_apuntar_a_una_sola_referencia()
        {
            _e.Fallos.Reemplazar([new FaultRule { CommandType = Comandos.AltaPedido, ReferenciaNegocio = "OTRA", ErrorCode = CodigosError.SdkError }]);
            (await _e.ProcesarAsync(await _e.EncolarAsync("alta-pedido.valido.json"))).Status.Should().Be(Estados.Confirmed);
        }

        [Fact]
        public async Task Alta_de_almacen_con_el_mismo_nombre_devuelve_el_existente_y_con_otro_falla()
        {
            var primera = await _e.ProcesarAsync(await _e.EncolarAsync("alta-almacen.valido.json", "almacen:1:alta"));
            var repetida = await _e.ProcesarAsync(await _e.EncolarAsync("alta-almacen.valido.json", "almacen:2:alta"));
            var idPrimera = JsonSerializer.Deserialize<Resultado>(primera.ResultJson!)!.IdErp;
            JsonSerializer.Deserialize<Resultado>(repetida.ResultJson!)!.IdErp.Should().Be(idPrimera);

            (await _e.Lecturas.AlmacenAsync("WIP-SC"))!.Nombre.Should().Be("WIP Santa Cruz");

            var otroNombre = await _e.ProcesarAsync(await _e.EncolarAsync("alta-almacen.valido.json", "almacen:3:alta",
                j => j.Replace("WIP Santa Cruz", "Otro nombre", System.StringComparison.Ordinal)));
            otroNombre.Status.Should().Be(Estados.Failed);
            JsonSerializer.Deserialize<ErrorContrato>(otroNombre.ErrorJson!)!.Code.Should().Be(CodigosError.AlmacenYaExiste);
        }

        [Fact]
        public async Task Sistemas_reencola_desde_la_dlq_con_la_carga_corregida()
        {
            var id = await _e.EncolarAsync("alta-pedido.invalido.cliente-no-existe.json");
            (await _e.ProcesarAsync(id)).Status.Should().Be(Estados.Failed);
            await _e.Outbox.CompletarAsync(id, Estados.DeadLetter, null, null, null);

            var corregida = (await _e.Outbox.GetByIdAsync(id))!.PayloadJson.Replace("NO-EXISTE", "EMM-001", System.StringComparison.Ordinal);
            (await _e.Outbox.ReencolarAsync(id, corregida)).Should().BeTrue();

            (await _e.ProcesarAsync(id)).Status.Should().Be(Estados.Confirmed);
        }
    }
}
