using System.Net;
using AwesomeAssertions;
using PolyConecta.Contract.Tests.Infraestructura;
using Xunit;

namespace PolyConecta.Contract.Tests.Sobre;

/// <summary>Sobre del comando (§2), versión (§8) y consulta de la transacción (§3).</summary>
public class SobreTests(Bridge bridge)
{
    [ContratoFact]
    public async Task Una_version_mayor_distinta_se_rechaza()
    {
        var (status, error) = await bridge.EnviarAsync(bridge.Ejemplo("traspaso.valido.json", c => c["contract_version"] = "2.0"));
        status.Should().Be(HttpStatusCode.BadRequest);
        error["code"]!.GetValue<string>().Should().Be("VERSION_NO_SOPORTADA");
    }

    [ContratoTheory]
    [InlineData("correlation_id")]
    [InlineData("callback_url")]
    [InlineData("idempotency_key")]
    public async Task Sin_un_campo_obligatorio_del_sobre_se_rechaza(string campo)
    {
        var (status, error) = await bridge.EnviarAsync(bridge.Ejemplo("traspaso.valido.json", c => c.Remove(campo)));
        status.Should().Be(HttpStatusCode.BadRequest);
        error["code"]!.GetValue<string>().Should().Be("CARGA_INVALIDA");
    }

    [ContratoFact]
    public async Task Un_comando_desconocido_se_rechaza()
    {
        var (status, error) = await bridge.EnviarAsync(bridge.Ejemplo("traspaso.valido.json", c => c["command_type"] = "DOCUMENT_CREATE"));
        status.Should().Be(HttpStatusCode.BadRequest);
        error["detail"]!["campo"]!.GetValue<string>().Should().Be("command_type");
    }

    [ContratoFact]
    public async Task La_consulta_devuelve_lo_mismo_que_el_callback()
    {
        var comando = bridge.Ejemplo("traspaso.valido.json");
        var (_, acuse) = await bridge.EnviarAsync(comando);
        var callback = await bridge.CallbackAsync(comando["idempotency_key"]!.GetValue<string>());

        var (status, consulta) = await bridge.ConsultarAsync(acuse["transaction_id"]!.GetValue<string>());

        status.Should().Be(HttpStatusCode.OK);
        consulta!.ToJsonString().Should().Be(callback!.ToJsonString());
    }

    [ContratoFact]
    public async Task Una_transaccion_inexistente_da_404()
    {
        (await bridge.ConsultarAsync(Guid.NewGuid().ToString())).Status.Should().Be(HttpStatusCode.NotFound);
    }
}
