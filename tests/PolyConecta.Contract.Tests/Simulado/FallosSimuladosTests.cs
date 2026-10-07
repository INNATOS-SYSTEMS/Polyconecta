using System.Diagnostics;
using System.Net;
using AwesomeAssertions;
using PolyConecta.Contract.Tests.Infraestructura;
using Xunit;

namespace PolyConecta.Contract.Tests.Simulado;

/// <summary>
/// Fallos provocados por configuración (FR-008), solo contra el simulador. Cada regla apunta a una
/// referencia_negocio propia para no afectar a las demás pruebas; la colección no corre en paralelo
/// porque PUT /admin/simulated/faults reemplaza todas las reglas.
/// </summary>
[Collection(nameof(FallosSimuladosTests))]
[CollectionDefinition(nameof(FallosSimuladosTests), DisableParallelization = true)]
[Trait("Categoria", "Simulado")]
public class FallosSimuladosTests(Bridge bridge) : IAsyncLifetime
{
    private readonly string _referencia = $"PRUEBA-{Guid.NewGuid():N}"[..20];

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    private void SoloSimulado() => Assert.SkipUnless(bridge.EsSimulado, "Solo contra el bridge simulado.");

    public async ValueTask DisposeAsync()
    {
        if (bridge.EsSimulado) await bridge.FallosAsync();
    }

    private System.Text.Json.Nodes.JsonObject Pedido() =>
        bridge.Ejemplo("alta-pedido.valido.json", c => c["payload"]!["referencia_negocio"] = _referencia);

    [ContratoFact]
    public async Task Un_error_provocado_llega_como_FAILED_con_su_codigo()
    {
        SoloSimulado();
        await bridge.FallosAsync(new { command_type = "ALTA_PEDIDO", referencia_negocio = _referencia, error_code = "SDK_ERROR" });
        var comando = Pedido();
        await bridge.EnviarAsync(comando);

        var callback = await bridge.CallbackAsync(comando["idempotency_key"]!.GetValue<string>());
        callback!["status"]!.GetValue<string>().Should().Be("FAILED");
        callback["error"]!["code"]!.GetValue<string>().Should().Be("SDK_ERROR");
    }

    [ContratoFact]
    public async Task Una_demora_provocada_retrasa_el_callback()
    {
        SoloSimulado();
        await bridge.FallosAsync(new { command_type = "ALTA_PEDIDO", referencia_negocio = _referencia, delay_ms = 1500 });
        var comando = Pedido();
        var reloj = Stopwatch.StartNew();
        await bridge.EnviarAsync(comando);

        var callback = await bridge.CallbackAsync(comando["idempotency_key"]!.GetValue<string>());
        reloj.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(1500));
        callback!["status"]!.GetValue<string>().Should().Be("CONFIRMED");
    }

    [ContratoFact]
    public async Task Un_callback_perdido_se_recupera_consultando_la_transaccion()
    {
        SoloSimulado();
        await bridge.FallosAsync(new { command_type = "ALTA_PEDIDO", referencia_negocio = _referencia, drop_callback = true });
        var comando = Pedido();
        var (_, acuse) = await bridge.EnviarAsync(comando);

        (await bridge.CallbackAsync(comando["idempotency_key"]!.GetValue<string>(), TimeSpan.FromSeconds(3))).Should().BeNull();
        var (status, consulta) = await bridge.ConsultarAsync(acuse["transaction_id"]!.GetValue<string>());
        status.Should().Be(HttpStatusCode.OK);
        consulta!["status"]!.GetValue<string>().Should().Be("CONFIRMED");
        consulta["result"]!["folio"].Should().NotBeNull();
    }

    [ContratoFact]
    public async Task El_reenvio_de_una_transaccion_FAILED_la_vuelve_a_procesar_sin_duplicar()
    {
        SoloSimulado();
        await bridge.FallosAsync(new { command_type = "ALTA_PEDIDO", referencia_negocio = _referencia, error_code = "EXISTENCIA_INSUFICIENTE", veces = 1 });
        var comando = Pedido();
        var llave = comando["idempotency_key"]!.GetValue<string>();
        var (_, primero) = await bridge.EnviarAsync(comando);
        (await bridge.CallbackAsync(llave))!["status"]!.GetValue<string>().Should().Be("FAILED");

        var (status, reenvio) = await bridge.EnviarAsync(comando);

        status.Should().Be(HttpStatusCode.Accepted);
        reenvio["is_duplicate"]!.GetValue<bool>().Should().BeTrue();
        reenvio["transaction_id"]!.GetValue<string>().Should().Be(primero["transaction_id"]!.GetValue<string>());
        var callbacks = await bridge.CallbacksAsync(llave, 2);
        callbacks.Select(c => c["status"]!.GetValue<string>()).Should().Equal("FAILED", "CONFIRMED");
    }
}
