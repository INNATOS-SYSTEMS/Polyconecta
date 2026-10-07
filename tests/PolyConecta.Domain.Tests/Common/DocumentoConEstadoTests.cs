using AwesomeAssertions;
using PolyConecta.Domain.Common;
using Xunit;

namespace PolyConecta.Domain.Tests.Common;

public class DocumentoConEstadoTests
{
    private enum Estado { Borrador, Confirmado, Cancelado }

    private sealed class Documento() : DocumentoConEstado<Estado>(Estado.Borrador)
    {
        public void Confirmar() => Transicionar(Estado.Confirmado, "listo", Estado.Borrador);

        public void Cancelar() => Transicionar(Estado.Cancelado, null, Estado.Borrador, Estado.Confirmado);
    }

    [Fact]
    public void Una_transicion_valida_cambia_el_estado_y_queda_pendiente_de_bitacora()
    {
        var doc = new Documento();
        doc.Confirmar();

        doc.State.Should().Be(Estado.Confirmado);
        doc.TransicionesPendientes.Should().ContainSingle()
            .Which.Should().Be(new TransicionRegistrada("Borrador", "Confirmado", "listo"));
    }

    [Fact]
    public void Una_transicion_invalida_se_rechaza_sin_cambiar_nada()
    {
        var doc = new Documento();
        doc.Cancelar();

        var accion = doc.Confirmar;
        accion.Should().Throw<TransicionInvalidaException>()
            .WithMessage("Documento: no se puede pasar de Cancelado a Confirmado.");
        doc.State.Should().Be(Estado.Cancelado);
        doc.TransicionesPendientes.Should().HaveCount(1);
    }

    [Fact]
    public void Archivar_no_borra_y_se_puede_restaurar()
    {
        var doc = new Documento();
        doc.Archive();
        doc.IsActive.Should().BeFalse();
        doc.Restore();
        doc.IsActive.Should().BeTrue();
    }
}
