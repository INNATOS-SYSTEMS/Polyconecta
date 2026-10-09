using AwesomeAssertions;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Inventario;
using Xunit;

namespace PolyConecta.Domain.Tests.Inventario;

/// <summary>Producto sincronizado, clasificación propia y ficha técnica (L2-T013, FR-015 a FR-018).</summary>
public class ProductoTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 13, 10, 0, 0, TimeSpan.Zero);

    private static DatosErpProducto Datos(string nombre = "BOLSA MEDIANA", string unidad = "PZA", bool lote = true) =>
        new(1113, "PT1113 C567", nombre, unidad, lote, true);

    private static readonly DatosRollo Rollo = new("PEBD", "Tubular 44 cm", 60m, 25m, 38, "Blanco", null, null, null);
    private static readonly DatosPt Pt = new("BOL-004", "44x84", "2 tintas", "Pantone 286", "Sin suaje", "Caja 1000", "Fondo", 9.8m);

    [Fact]
    public void Desde_CONTPAQi_lleva_su_unidad_base_como_unica_unidad_y_su_etiqueta()
    {
        var p = Product.DesdeErp(Datos(), Ahora);

        p.ErpUom.Should().Be("PZA");
        p.PackagingUnits.Should().ContainSingle().Which.IsErpBaseUnit.Should().BeTrue();
        p.Etiqueta.Should().Be("PT1113 C567 - BOLSA MEDIANA");
        p.TracksLots.Should().BeTrue();
    }

    [Fact]
    public void SC004_actualizar_sin_cambios_no_cambia_nada()
    {
        var p = Product.DesdeErp(Datos(), Ahora);

        p.ActualizarDesdeErp(Datos(), Ahora.AddDays(1)).Should().BeFalse();
        p.ErpSyncedAt.Should().Be(Ahora);

        p.ActualizarDesdeErp(Datos(nombre: "BOLSA GRANDE", unidad: "mil"), Ahora.AddDays(1)).Should().BeTrue();
        p.ErpUom.Should().Be("MIL");
        p.UnidadBase.Code.Should().Be("MIL");
    }

    [Fact]
    public void Archivar_y_restaurar_por_CONTPAQi_dicen_si_cambiaron()
    {
        var p = Product.DesdeErp(Datos(), Ahora);
        p.ArchivarPorErp().Should().BeTrue();
        p.ArchivarPorErp().Should().BeFalse();
        p.IsActive.Should().BeFalse();
        p.RestaurarPorErp().Should().BeTrue();
    }

    [Fact]
    public void D86_la_clasificacion_de_CONTPAQi_solo_llena_un_producto_sin_clasificacion()
    {
        var p = Product.DesdeErp(Datos(), Ahora);
        p.ClasificarSiVacia(1).Should().BeTrue();
        p.Clasificar(7);

        p.ClasificarSiVacia(1).Should().BeFalse();
        p.ClassificationId.Should().Be(7);
    }

    [Fact]
    public void FR018_la_ficha_tecnica_lleva_los_dos_bloques_y_el_PT_ligado_a_su_rollo()
    {
        var p = Product.DesdeErp(Datos(), Ahora);

        var sinPt = () => p.GuardarFichaTecnica(Rollo, null!);
        sinPt.Should().Throw<ReglaDeNegocioException>().Which.Codigo.Should().Be("FICHA_INCOMPLETA");
        var rolloIncompleto = () => p.GuardarFichaTecnica(Rollo with { MaterialType = " " }, Pt);
        rolloIncompleto.Should().Throw<ReglaDeNegocioException>();

        p.GuardarFichaTecnica(Rollo, Pt);
        p.Roll!.MaterialType.Should().Be("PEBD");
        p.Pt!.RelatedRoll.Should().BeSameAs(p.Roll);
        p.Pt.KgPerThousand.Should().Be(9.8m);

        p.GuardarFichaTecnica(Rollo with { GaugeMicrons = 70m }, Pt with { Inks = "3 tintas" });
        p.Roll.GaugeMicrons.Should().Be(70m);
        p.Pt.Inks.Should().Be("3 tintas");
    }

    [Fact]
    public void El_PT_puede_ligarse_al_rollo_de_otro_producto()
    {
        var segundoProceso = Product.DesdeErp(Datos() with { IdErp = 9, Codigo = "RI-9" }, Ahora);
        segundoProceso.GuardarFichaTecnica(Rollo, Pt);
        var p = Product.DesdeErp(Datos(), Ahora);

        p.GuardarFichaTecnica(Rollo, Pt, segundoProceso.Roll);

        p.Pt!.RelatedRoll.Should().BeSameAs(segundoProceso.Roll);
    }
}
