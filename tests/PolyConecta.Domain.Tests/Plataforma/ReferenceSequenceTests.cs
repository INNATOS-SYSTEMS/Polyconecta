using AwesomeAssertions;
using PolyConecta.Domain.Plataforma;
using Xunit;

namespace PolyConecta.Domain.Tests.Plataforma;

public class ReferenceSequenceTests
{
    private static readonly DateTimeOffset Octubre = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Arma_el_folio_con_prefijo_marcadores_y_relleno()
    {
        var seq = new ReferenceSequence("OF_BOLSEO", "BOL-{yyyy}-", 4, ResetRule.Nunca);

        seq.Siguiente(Octubre).Should().Be("BOL-2026-0001");
        seq.Siguiente(Octubre).Should().Be("BOL-2026-0002");
    }

    [Fact]
    public void Sustituye_los_marcadores_del_contexto()
    {
        var seq = new ReferenceSequence("ROLLO_EXTRUSION", "EX-{linea}-{yy}", 6, ResetRule.Anual);

        seq.Siguiente(Octubre, new Dictionary<string, string> { ["linea"] = "01" }).Should().Be("EX-01-26000001");
    }

    [Fact]
    public void Reinicia_el_consecutivo_al_cambiar_de_periodo()
    {
        var anual = new ReferenceSequence("PEDIDO", "IV", 3, ResetRule.Anual);
        anual.Siguiente(Octubre);
        anual.Siguiente(Octubre);
        anual.Siguiente(new DateTimeOffset(2027, 1, 2, 0, 0, 0, TimeSpan.Zero)).Should().Be("IV001");

        var mensual = new ReferenceSequence("OUT", "OUT/{MM}/", 2, ResetRule.Mensual);
        mensual.Siguiente(Octubre);
        mensual.Siguiente(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero)).Should().Be("OUT/11/01");
    }

    [Fact]
    public void Sin_reinicio_el_consecutivo_sigue_entre_periodos()
    {
        var seq = new ReferenceSequence("QC", "QC-", 3, ResetRule.Nunca);
        seq.Siguiente(Octubre);
        seq.Siguiente(new DateTimeOffset(2027, 1, 2, 0, 0, 0, TimeSpan.Zero)).Should().Be("QC-002");
    }
}
