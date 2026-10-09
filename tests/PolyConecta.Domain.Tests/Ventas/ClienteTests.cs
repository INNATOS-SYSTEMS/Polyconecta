using AwesomeAssertions;
using PolyConecta.Domain.Ventas;
using Xunit;

namespace PolyConecta.Domain.Tests.Ventas;

/// <summary>Cliente sincronizado con moneda y domicilios (L2-T013, D-146, D-149, D-150).</summary>
public class ClienteTests
{
    private static DatosDomicilio Dom(long id, TipoDomicilio tipo, string calle = "Av. Industrial", string? sucursal = null) =>
        new(id, tipo, calle, "120", null, "Parque Industrial", "66600", "Apodaca", "Apodaca", "Nuevo León", "México", sucursal);

    private static DatosErpCliente Datos(string? moneda = "USD", params DatosDomicilio[] domicilios) =>
        new(57, "EMM-001", "EMPRESA MEXICANA DE MANUFACTURA", "EMM010101AAA", true, moneda, domicilios);

    [Fact]
    public void Trae_su_moneda_y_un_domicilio_fiscal_y_varios_de_envio()
    {
        var c = Customer.DesdeErp(Datos("usd", Dom(901, TipoDomicilio.Fiscal), Dom(902, TipoDomicilio.Envio, sucursal: "Planta Norte"), Dom(903, TipoDomicilio.Envio)));

        c.Currency.Should().Be("USD");
        c.Addresses.Should().HaveCount(3);
        c.DomiciliosDeEnvio.Select(d => d.ErpAddressId).Should().Equal(902, 903);
        c.DomiciliosDeEnvio.First().Texto.Should().Be("Planta Norte · Av. Industrial 120, Parque Industrial, Apodaca, Nuevo León, CP 66600");
        c.Etiqueta.Should().Be("EMM-001 - EMPRESA MEXICANA DE MANUFACTURA");
    }

    [Fact]
    public void Sin_cambios_no_cambia_y_un_domicilio_que_ya_no_viene_se_archiva_y_vuelve_si_regresa()
    {
        var c = Customer.DesdeErp(Datos("USD", Dom(901, TipoDomicilio.Fiscal), Dom(902, TipoDomicilio.Envio)));

        c.ActualizarDesdeErp(Datos("USD", Dom(901, TipoDomicilio.Fiscal), Dom(902, TipoDomicilio.Envio))).Should().BeFalse();

        c.ActualizarDesdeErp(Datos("USD", Dom(901, TipoDomicilio.Fiscal))).Should().BeTrue();
        c.Addresses.Single(d => d.ErpAddressId == 902).IsActive.Should().BeFalse();
        c.DomiciliosDeEnvio.Should().BeEmpty();

        c.ActualizarDesdeErp(Datos("USD", Dom(901, TipoDomicilio.Fiscal), Dom(902, TipoDomicilio.Envio, "Otra calle"))).Should().BeTrue();
        c.DomiciliosDeEnvio.Single().Street.Should().Be("Otra calle");
    }

    [Fact]
    public void Sin_domicilios_en_la_lectura_no_toca_los_que_hay_y_sin_moneda_queda_vacia()
    {
        var c = Customer.DesdeErp(Datos("USD", Dom(902, TipoDomicilio.Envio)));

        c.ActualizarDesdeErp(new DatosErpCliente(57, "EMM-001", "EMPRESA MEXICANA DE MANUFACTURA", "EMM010101AAA", true, null, null)).Should().BeTrue();

        c.Currency.Should().BeNull();
        c.DomiciliosDeEnvio.Should().ContainSingle();
    }

    [Fact]
    public void El_agente_cambia_solo_si_cambia_algo()
    {
        var a = new ErpAgent(3, "AG-01", "Celia Villarreal", TipoAgente.Venta);
        a.ActualizarDesdeErp("AG-01", "Celia Villarreal", TipoAgente.Venta).Should().BeFalse();
        a.ActualizarDesdeErp("AG-01", "Celia V.", TipoAgente.VentaCobro).Should().BeTrue();
        a.Etiqueta.Should().Be("AG-01 - Celia V.");
    }
}
