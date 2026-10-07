using AwesomeAssertions;
using PolyConecta.Domain.Plataforma;
using Xunit;

namespace PolyConecta.Domain.Tests.Plataforma;

public class OutboxMessageTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    private static OutboxMessage Mensaje(params string[] llaves) =>
        new("TRASPASO", "RECOLECCION", "{}", $"Prueba:1:{Guid.NewGuid():N}", "c1", "Prueba", 1, llaves, Ahora);

    [Fact]
    public void Comparte_llaves_solo_si_tiene_un_producto_o_almacen_en_comun()
    {
        var a = Mensaje("producto:MP-1", "almacen:PIM-MP");
        Mensaje("producto:MP-2", "almacen:PIM-MP").ComparteLlaves(a).Should().BeTrue();
        Mensaje("producto:MP-3", "almacen:SC-MP").ComparteLlaves(a).Should().BeFalse();
    }

    [Fact]
    public void Las_fallas_de_envio_esperan_2_a_la_n_y_al_agotarse_quedan_en_error()
    {
        var m = Mensaje("producto:MP-1");

        m.RegistrarFallaDeEnvio("timeout", Ahora, maxIntentos: 3).Should().BeFalse();
        m.NextAttemptAt.Should().Be(Ahora.AddSeconds(2));
        m.RegistrarFallaDeEnvio("timeout", Ahora, maxIntentos: 3).Should().BeFalse();
        m.NextAttemptAt.Should().Be(Ahora.AddSeconds(4));
        m.RegistrarFallaDeEnvio("timeout", Ahora, maxIntentos: 3).Should().BeTrue();

        m.Status.Should().Be(OutboxStatus.Error);
        m.LastErrorCode.Should().Be("ENVIO_FALLIDO");
    }

    [Fact]
    public void Reintentar_un_error_conserva_la_llave_de_idempotencia()
    {
        var m = Mensaje("producto:MP-1");
        var llave = m.IdempotencyKey;
        m.MarcarEnviado("tx-1", Ahora);
        m.MarcarError("SDK_TIMEOUT", "lento", Ahora);

        m.Reintentar();

        m.Status.Should().Be(OutboxStatus.Pendiente);
        m.IdempotencyKey.Should().Be(llave);
        m.BridgeTransactionId.Should().BeNull();
        m.Attempts.Should().Be(0);
    }
}
