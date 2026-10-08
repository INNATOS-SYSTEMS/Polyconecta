using System.IO;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Tests.Soporte;
using Xunit;

namespace Contpaq.Bridge.Tests.Contract
{
    /// <summary>El sobre y la carga de cada comando (§2, §5) contra los ejemplos del contrato.</summary>
    public class LectorComandosTests
    {
        public static TheoryData<string> Ejemplos() => new(
            Directory.GetFiles(Path.Combine(Entorno.Raiz, "docs", "contratos", "ejemplos"), "*.json")
                .Select(Path.GetFileName)
                .Where(n => !n!.StartsWith("callback", System.StringComparison.Ordinal))!);

        [Theory]
        [MemberData(nameof(Ejemplos))]
        public void Cada_ejemplo_cumple_el_esquema_salvo_los_de_carga_incompleta(string ejemplo)
        {
            var (comando, error) = LectorComandos.Leer(Entorno.Comando(ejemplo));

            if (ejemplo.Contains("carga-incompleta"))
            {
                error!.Code.Should().Be(CodigosError.CargaInvalida);
                return;
            }
            error.Should().BeNull();
            comando!.CommandType.Should().Be(Entorno.Comando(ejemplo).CommandType);
        }

        [Fact]
        public void El_agente_es_opcional_pero_no_puede_venir_vacio()
        {
            var r = Entorno.Comando("alta-pedido.valido.json");
            LectorComandos.Leer(r).Error.Should().BeNull("agente es opcional");

            var conAgente = Entorno.Comando("alta-pedido.valido.con-agente.json");
            ((CargaAltaPedido)LectorComandos.Leer(conAgente).Comando!.Carga).Agente.Should().Be("AG-01");

            conAgente.Payload = JsonDocument.Parse(conAgente.Payload.GetRawText().Replace("\"AG-01\"", "\" \"")).RootElement;
            LectorComandos.Leer(conAgente).Error!.Detail["campo"].Should().Be("agente");
        }

        [Fact]
        public void Una_version_mayor_distinta_se_rechaza()
        {
            var r = Entorno.Comando("traspaso.valido.json");
            r.ContractVersion = "2.0";
            LectorComandos.Leer(r).Error!.Code.Should().Be(CodigosError.VersionNoSoportada);
        }

        [Fact]
        public void El_traspaso_exige_una_variante_conocida()
        {
            var r = Entorno.Comando("traspaso.valido.json");
            r.Variant = null;
            LectorComandos.Leer(r).Error!.Detail["campo"].Should().Be("variant");
            r.Variant = "INVENTADA";
            LectorComandos.Leer(r).Error!.Code.Should().Be(CodigosError.CargaInvalida);
        }

        [Fact]
        public void Un_campo_desconocido_en_la_carga_se_rechaza()
        {
            var r = Entorno.Comando("alta-almacen.valido.json");
            r.Payload = JsonDocument.Parse("""{"codigo":"X","nombre":"Y","fecha_alta":"2026-10-13","concepto":"NO"}""").RootElement;
            LectorComandos.Leer(r).Error!.Code.Should().Be(CodigosError.CargaInvalida);
        }

        [Fact]
        public void El_documento_creado_generico_ya_no_existe()
        {
            var r = Entorno.Comando("traspaso.valido.json");
            r.CommandType = "DOCUMENT_CREATE";
            LectorComandos.Leer(r).Error!.Detail["campo"].Should().Be("command_type");
        }

        [Fact]
        public void La_remision_admite_precio_opcional_por_linea_y_no_negativo()
        {
            var r = Entorno.Comando("remision.valido.json");
            var conPrecio = r.Payload.GetRawText().Replace("\"unidad\": \"PZA\",", "\"unidad\": \"PZA\", \"precio\": 5.70,", System.StringComparison.Ordinal);
            r.Payload = JsonDocument.Parse(conPrecio).RootElement;
            LectorComandos.Leer(r).Error.Should().BeNull();

            r.Payload = JsonDocument.Parse(conPrecio.Replace("5.70", "-1", System.StringComparison.Ordinal)).RootElement;
            LectorComandos.Leer(r).Error!.Detail["campo"].Should().Be("lineas[0].precio");
        }

        [Fact]
        public void La_fecha_debe_ser_AAAA_MM_DD()
        {
            var r = Entorno.Comando("remision.valido.json");
            var json = r.Payload.GetRawText().Replace("2026-10-28", "28/10/2026", System.StringComparison.Ordinal);
            r.Payload = JsonDocument.Parse(json).RootElement;
            LectorComandos.Leer(r).Error!.Detail["campo"].Should().Be("fecha");
        }
    }
}
