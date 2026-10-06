using System.Net;
using AwesomeAssertions;
using PolyConecta.Contract.Tests.Infraestructura;
using Xunit;

namespace PolyConecta.Contract.Tests.Comandos;

/// <summary>
/// Los cinco comandos (§5) con sus ejemplos: la carga válida se confirma por callback con su
/// resultado, la inválida falla con el código del contrato y el reenvío no duplica (CT-19).
/// </summary>
public class ComandosTests(Bridge bridge)
{
    [ContratoTheory]
    [InlineData("traspaso.valido.json")]
    [InlineData("alta-pedido.valido.json")]
    [InlineData("alta-almacen.valido.json")]
    [InlineData("cierre-produccion.valido.json")]
    [InlineData("remision.valido.json")]
    public async Task La_carga_valida_se_acepta_y_se_confirma_por_callback(string ejemplo)
    {
        var comando = bridge.Ejemplo(ejemplo);
        var (status, acuse) = await bridge.EnviarAsync(comando);

        status.Should().Be(HttpStatusCode.Accepted);
        acuse["status"]!.GetValue<string>().Should().Be("PENDING");
        acuse["correlation_id"]!.GetValue<string>().Should().Be(comando["correlation_id"]!.GetValue<string>());

        var callback = await bridge.CallbackAsync(comando["idempotency_key"]!.GetValue<string>());
        callback.Should().NotBeNull("el bridge debe avisar por callback");
        callback!["status"]!.GetValue<string>().Should().Be("CONFIRMED");
        callback["transaction_id"]!.GetValue<string>().Should().Be(acuse["transaction_id"]!.GetValue<string>());
        callback["error"].Should().BeNull();

        var resultado = callback["result"]!.AsObject();
        switch (comando["command_type"]!.GetValue<string>())
        {
            case "TRASPASO":
                resultado["documentos"]!.AsArray().Select(d => d!["rol"]!.GetValue<string>()).Should().Equal("salida", "entrada");
                break;
            case "CIERRE_PRODUCCION":
                resultado["documentos"]!.AsArray().Select(d => d!["rol"]!.GetValue<string>()).Should().Equal("consumo", "entrada", "subproducto");
                break;
            case "ALTA_ALMACEN":
                resultado["id_erp"]!.GetValue<long>().Should().BePositive();
                break;
            case "REMISION":
                resultado["folio"].Should().NotBeNull();
                resultado["pedido_cancelado"]!.GetValue<bool>().Should().BeTrue();
                break;
            default:
                resultado["folio"].Should().NotBeNull();
                resultado["id_erp"]!.GetValue<long>().Should().BePositive();
                break;
        }
        bridge.FirmasInvalidas.Should().Be(0);
    }

    [ContratoTheory]
    [InlineData("traspaso.invalido.lotes-no-cuadran.json", "LOTES_NO_CUADRAN")]
    [InlineData("alta-pedido.invalido.cliente-no-existe.json", "CLIENTE_NO_EXISTE")]
    [InlineData("cierre-produccion.invalido.unidad-no-admitida.json", "UNIDAD_NO_ADMITIDA")]
    [InlineData("remision.invalido.existencia-insuficiente-lote.json", "EXISTENCIA_INSUFICIENTE_LOTE")]
    public async Task La_carga_invalida_falla_por_callback_con_el_codigo_del_contrato(string ejemplo, string codigo)
    {
        var comando = bridge.Ejemplo(ejemplo);
        (await bridge.EnviarAsync(comando)).Status.Should().Be(HttpStatusCode.Accepted);

        var callback = await bridge.CallbackAsync(comando["idempotency_key"]!.GetValue<string>());
        callback!["status"]!.GetValue<string>().Should().Be("FAILED");
        callback["result"].Should().BeNull();
        callback["error"]!["code"]!.GetValue<string>().Should().Be(codigo);
        callback["error"]!["retryable"]!.GetValue<bool>().Should().BeFalse();
    }

    [ContratoFact]
    public async Task La_carga_que_no_cumple_el_esquema_se_rechaza_con_400_sin_crear_transaccion()
    {
        var (status, error) = await bridge.EnviarAsync(bridge.Ejemplo("alta-almacen.invalido.carga-incompleta.json"));

        status.Should().Be(HttpStatusCode.BadRequest);
        error["code"]!.GetValue<string>().Should().Be("CARGA_INVALIDA");
        error["detail"]!["campo"]!.GetValue<string>().Should().Be("codigo");
    }

    [ContratoFact]
    public async Task El_reenvio_con_la_misma_llave_devuelve_el_original_y_no_duplica()
    {
        var comando = bridge.Ejemplo("alta-pedido.valido.json");
        var llave = comando["idempotency_key"]!.GetValue<string>();
        var (_, primero) = await bridge.EnviarAsync(comando);
        var callback = await bridge.CallbackAsync(llave);

        var (status, segundo) = await bridge.EnviarAsync(comando);

        status.Should().Be(HttpStatusCode.Accepted);
        segundo["is_duplicate"]!.GetValue<bool>().Should().BeTrue();
        segundo["transaction_id"]!.GetValue<string>().Should().Be(primero["transaction_id"]!.GetValue<string>());
        segundo["status"]!.GetValue<string>().Should().Be("CONFIRMED");
        await Task.Delay(1000);
        bridge.CallbacksRecibidos(llave).Should().Be(1);
        callback!["result"]!["folio"]!.GetValue<string>().Should().NotBeEmpty();
    }
}
