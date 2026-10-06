using AwesomeAssertions;
using PolyConecta.Domain.Common;
using Xunit;

namespace PolyConecta.Domain.Tests.Common;

public class SyncStateTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Ciclo_feliz_NoAplica_Pendiente_Enviado_Confirmado()
    {
        var sync = new SyncState();
        sync.MarcarPendiente(Ahora);
        sync.MarcarEnviado(Ahora);
        sync.Confirmar("A-15", "4521", "[]", Ahora).Should().BeTrue();

        sync.Status.Should().Be(SyncStatus.Confirmado);
        sync.ErpFolio.Should().Be("A-15");
        sync.ErpId.Should().Be("4521");
    }

    [Fact]
    public void Un_callback_sobre_un_documento_confirmado_no_cambia_nada()
    {
        var sync = new SyncState();
        sync.MarcarPendiente(Ahora);
        sync.MarcarEnviado(Ahora);
        sync.Confirmar("A-15", "4521", null, Ahora);

        sync.Confirmar("OTRO", "9", null, Ahora).Should().BeFalse();
        sync.MarcarError("SDK_ERROR", "tarde", Ahora).Should().BeFalse();
        sync.ErpFolio.Should().Be("A-15");
        sync.Status.Should().Be(SyncStatus.Confirmado);
    }

    [Fact]
    public void Error_se_recupera_reintentando()
    {
        var sync = new SyncState();
        sync.MarcarPendiente(Ahora);
        sync.MarcarEnviado(Ahora);
        sync.MarcarError("EXISTENCIA_INSUFICIENTE", "sin existencia", Ahora);
        sync.LastErrorCode.Should().Be("EXISTENCIA_INSUFICIENTE");

        sync.Reintentar(Ahora);
        sync.Status.Should().Be(SyncStatus.Pendiente);
    }

    [Fact]
    public void No_se_puede_enviar_lo_que_no_esta_pendiente()
    {
        var sync = new SyncState();
        var accion = () => sync.MarcarEnviado(Ahora);
        accion.Should().Throw<TransicionInvalidaException>().Which.EstadoActual.Should().Be("NoAplica");
    }

    [Fact]
    public void No_se_reintenta_lo_que_no_esta_en_error()
    {
        var sync = new SyncState();
        sync.MarcarPendiente(Ahora);
        var accion = () => sync.Reintentar(Ahora);
        accion.Should().Throw<TransicionInvalidaException>();
    }
}
