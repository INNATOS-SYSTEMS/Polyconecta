using System.Threading.Tasks;
using AwesomeAssertions;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Tests.Soporte;
using Xunit;

namespace Contpaq.Bridge.Tests.Validation
{
    /// <summary>Validar antes de escribir (CT-39, D-127), con el catálogo del modo simulado.</summary>
    public class ValidadorComandosTests
    {
        private readonly Entorno _e = new();

        private Task<ErrorContrato?> Validar(string ejemplo, System.Func<string, string>? cambiar = null)
        {
            var r = Entorno.Comando(ejemplo);
            if (cambiar is not null)
                r.Payload = System.Text.Json.JsonDocument.Parse(cambiar(r.Payload.GetRawText())).RootElement;
            var (comando, error) = LectorComandos.Leer(r);
            error.Should().BeNull();
            return _e.Validador.ValidarAsync(comando!);
        }

        [Theory]
        [InlineData("traspaso.valido.json")]
        [InlineData("alta-pedido.valido.json")]
        [InlineData("alta-pedido.valido.con-agente.json")]
        [InlineData("alta-almacen.valido.json")]
        [InlineData("cierre-produccion.valido.json")]
        [InlineData("remision.valido.json")]
        public async Task Los_ejemplos_validos_pasan(string ejemplo) => (await Validar(ejemplo)).Should().BeNull();

        [Theory]
        [InlineData("traspaso.invalido.lotes-no-cuadran.json", CodigosError.LotesNoCuadran)]
        [InlineData("alta-pedido.invalido.cliente-no-existe.json", CodigosError.ClienteNoExiste)]
        [InlineData("alta-pedido.invalido.agente-no-existe.json", CodigosError.AgenteNoExiste)]
        [InlineData("cierre-produccion.invalido.unidad-no-admitida.json", CodigosError.UnidadNoAdmitida)]
        [InlineData("remision.invalido.existencia-insuficiente-lote.json", CodigosError.ExistenciaInsuficienteLote)]
        public async Task Los_ejemplos_invalidos_dan_su_codigo(string ejemplo, string codigo) =>
            (await Validar(ejemplo))!.Code.Should().Be(codigo);

        [Fact]
        public async Task Producto_desconocido_o_dado_de_baja()
        {
            (await Validar("alta-pedido.valido.json", j => j.Replace("PT1113 C567", "NO-EXISTE", System.StringComparison.Ordinal)))!
                .Code.Should().Be(CodigosError.ProductoNoExiste);
            (await Validar("alta-pedido.valido.json", j => j.Replace("\"PT1113 C567\", \"cantidad\": 5500, \"unidad\": \"PZA\"", "\"MP-BAJA\", \"cantidad\": 5500, \"unidad\": \"KG\"", System.StringComparison.Ordinal)))!
                .Code.Should().Be(CodigosError.ProductoInactivo);
        }

        [Fact]
        public async Task Almacen_desconocido() =>
            (await Validar("traspaso.valido.json", j => j.Replace("WIP-PIM", "NO-EXISTE", System.StringComparison.Ordinal)))!
                .Code.Should().Be(CodigosError.AlmacenNoExiste);

        [Fact]
        public async Task Sin_existencia_de_un_producto_sin_lote() =>
            (await Validar("traspaso.valido.json", j => j.Replace("1250.0", "9999.0", System.StringComparison.Ordinal)))!
                .Code.Should().Be(CodigosError.ExistenciaInsuficiente);

        [Fact]
        public async Task Moneda_sin_traduccion_a_CIDMONEDA() =>
            (await Validar("alta-pedido.valido.json", j => j.Replace("\"MXN\"", "\"EUR\"", System.StringComparison.Ordinal)))!
                .Code.Should().Be(CodigosError.MonedaNoSoportada);

        [Fact]
        public async Task Un_producto_sin_lote_no_acepta_lotes() =>
            (await Validar("traspaso.valido.json", j => j.Replace("\"lotes\": []", "\"lotes\": [ { \"numero\": \"X\", \"cantidad\": 1250.0 } ]", System.StringComparison.Ordinal)))!
                .Code.Should().Be(CodigosError.CargaInvalida);
    }
}
