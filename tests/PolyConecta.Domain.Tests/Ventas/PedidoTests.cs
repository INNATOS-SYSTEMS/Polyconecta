using AwesomeAssertions;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Domain.Ventas;
using Xunit;

namespace PolyConecta.Domain.Tests.Ventas;

/// <summary>Reglas del pedido de venta (L2-T020): una prueba por regla, con su id.</summary>
public class PedidoTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 13, 10, 0, 0, TimeSpan.Zero);
    private static readonly ReglasDeMoneda Monedas = ReglasDeMoneda.PorOmision;
    private static readonly DateOnly Hoy = new(2026, 10, 13);

    private static readonly Actor Ana = new(1, "Ana Treviño", "Comercial", false);
    private static readonly Actor Beto = new(2, "Beto Cobranza", "Crédito y Cobranza", false);
    private static readonly Actor Suplente = new(3, "Suplente de Cobranza", "Crédito y Cobranza", true);
    private static readonly Actor Doble = new(4, "Persona Doble", "Comercial", false);
    private static readonly Actor Admin = new(5, "Administrador", "Administrador", false);

    private static T ConId<T>(T entidad, long id) where T : AuditableEntity
    {
        typeof(AuditableEntity).GetProperty(nameof(AuditableEntity.Id))!.SetValue(entidad, id);
        return entidad;
    }

    private static Product Producto(long id = 10, bool activo = true)
    {
        var p = ConId(Product.DesdeErp(new DatosErpProducto(id, $"PT{id}", "Bolsa", "PZA", true, activo), Ahora), id);
        ConId(p.UnidadBase, id * 100);
        if (!activo) p.ArchivarPorErp();
        return p;
    }

    private static Customer Cliente(int envios = 2, string? moneda = "USD")
    {
        var domicilios = new List<DatosDomicilio> { new(900, TipoDomicilio.Fiscal, "Fiscal", null, null, null, null, null, null, null, null, null) };
        for (var i = 1; i <= envios; i++)
            domicilios.Add(new(900 + i, TipoDomicilio.Envio, $"Calle {i}", "1", null, null, "66600", "Apodaca", null, "NL", "México", $"Sucursal {i}"));
        var c = ConId(Customer.DesdeErp(new DatosErpCliente(57, "EMM-001", "EMPRESA MEXICANA", null, true, moneda, domicilios)), 3);
        foreach (var d in c.Addresses) ConId(d, d.ErpAddressId);
        return c;
    }

    private static DatosPedido Datos(Customer? cliente = null, string moneda = "MXN", decimal? tc = null, long? domicilio = null, params LineaSolicitada[] lineas) =>
        new(cliente ?? Cliente(), "OC-4471", null, Hoy, Hoy.AddDays(17), domicilio, moneda, tc,
            lineas.Length > 0 ? lineas : [new LineaSolicitada(null, Producto(), 1200, 0.85m, null, null)]);

    private static SalesOrder Confirmado()
    {
        var p = SalesOrder.Crear("PV-2026-0001", Datos(domicilio: 901), Monedas);
        p.Confirmar(Monedas);
        return p;
    }

    private static bool Comercial(string clave) => clave is Permisos.PedidoFirmarComercial or Permisos.PedidoRevocar or Permisos.PedidoLeer;

    [Fact]
    public void FR019_crear_deja_Borrador_con_folio_unidad_base_y_su_registro()
    {
        var p = SalesOrder.Crear("PV-2026-0001", Datos(), Monedas);

        p.State.Should().Be(SalesOrderState.Borrador);
        p.Name.Should().Be("PV-2026-0001");
        p.Origin.Should().Be(OrigenPedido.Manual);
        p.Lines.Single().RequestedPackagingUnitId.Should().Be(1000, "la unidad es la base del producto (D-127)");
        p.Sync.Status.Should().Be(SyncStatus.NoAplica);
        p.TransicionesPendientes.Should().ContainSingle(t => t.Desde == "Nuevo" && t.Hacia == "Borrador");
    }

    [Fact]
    public void D160_el_domicilio_de_entrega_es_el_primero_de_envio_y_no_se_elige()
    {
        SalesOrder.Crear("PV-1", Datos(Cliente(envios: 1)), Monedas).DeliveryAddressId.Should().Be(901);
        SalesOrder.Crear("PV-2", Datos(Cliente(envios: 2)), Monedas).DeliveryAddressId.Should().Be(901, "con varios, el primero");
        SalesOrder.Crear("PV-3", Datos(Cliente(envios: 2), domicilio: 900), Monedas).DeliveryAddressId.Should().Be(901, "lo que mande la interfaz no cuenta");
        SalesOrder.Crear("PV-4", Datos(Cliente(envios: 0)), Monedas).DeliveryAddressId.Should().BeNull("sin domicilios de envío no hay entrega");
    }

    [Fact]
    public void D161_la_moneda_base_lleva_tipo_de_cambio_1_y_otra_se_confirma_sin_capturarlo()
    {
        SalesOrder.Crear("PV-1", Datos(moneda: "MXN", tc: 20), Monedas).ExchangeRate.Should().Be(1);

        var usd = SalesOrder.Crear("PV-2", Datos(moneda: "USD"), Monedas);
        usd.Confirmar(Monedas);
        usd.State.Should().Be(SalesOrderState.Confirmado, "PolyConecta no captura el tipo de cambio (D-161, P-30)");

        var noAdmitida = () => SalesOrder.Crear("PV-3", Datos(moneda: "EUR"), Monedas);
        noAdmitida.Should().Throw<DatosIncompletosException>();
    }

    [Fact]
    public void FR022_no_confirma_sin_lineas_sin_cantidad_o_sin_precio_y_dice_que_falta()
    {
        var sinLineas = SalesOrder.Crear("PV-1", Datos(lineas: [new LineaSolicitada(null, Producto(), 1, 1, null, null)]), Monedas);
        sinLineas.Editar(Datos() with { Lineas = [] }, false, Monedas);
        ((Action)(() => sinLineas.Confirmar(Monedas))).Should().Throw<DatosIncompletosException>()
            .Which.Faltantes.Single().Campo.Should().Be("lineas");

        var incompleto = SalesOrder.Crear("PV-2", Datos(lineas: [new LineaSolicitada(null, Producto(), 0, null, null, null)]), Monedas);
        var error = ((Action)(() => incompleto.Confirmar(Monedas))).Should().Throw<DatosIncompletosException>().Which;
        error.Faltantes.Select(f => f.Campo).Should().BeEquivalentTo(["lineas[0].cantidad", "lineas[0].precioUnitario"]);
        incompleto.State.Should().Be(SalesOrderState.Borrador);
    }

    [Fact]
    public void FR022_confirmar_un_pedido_completo_lo_deja_Confirmado_y_copia_el_domicilio()
    {
        var p = Confirmado();

        p.State.Should().Be(SalesOrderState.Confirmado);
        p.DeliveryAddressText.Should().StartWith("Sucursal 1 · Calle 1 1");
        p.TransicionesPendientes.Last().Should().Be(new TransicionRegistrada("Borrador", "Confirmado", null));
    }

    [Fact]
    public void FR023_la_primera_firma_deja_Confirmado_y_la_segunda_Autorizado()
    {
        var p = Confirmado();

        p.Firmar(Ana, RolFirma.Comercial, Ahora);
        p.State.Should().Be(SalesOrderState.Confirmado);
        p.TransicionesPendientes.Last().Should().Be(new TransicionRegistrada("Confirmado", "Confirmado", "Firma de Comercial, 1 de 2"));

        p.Firmar(Beto, RolFirma.Cobranza, Ahora);
        p.State.Should().Be(SalesOrderState.Autorizado);
        p.Signatures.Select(f => (f.Role, f.UserId)).Should().BeEquivalentTo([(RolFirma.Comercial, 1L), (RolFirma.Cobranza, 2L)]);
    }

    [Fact]
    public void RF3_un_rol_no_firma_dos_veces()
    {
        var p = Confirmado();
        p.Firmar(Ana, RolFirma.Comercial, Ahora);

        var otraVez = () => p.Firmar(new Actor(9, "Otro Comercial", "Comercial", false), RolFirma.Comercial, Ahora);

        otraVez.Should().Throw<ReglaDeNegocioException>().Which.Razon.Should().Be("Ya está firmado por Comercial.");
    }

    [Fact]
    public void RF4_D34_ninguna_persona_aporta_las_dos_firmas_aunque_tenga_los_dos_roles()
    {
        var p = Confirmado();
        p.Firmar(Doble, RolFirma.Comercial, Ahora);

        var segunda = () => p.Firmar(Doble with { Grupo = "Crédito y Cobranza" }, RolFirma.Cobranza, Ahora);

        var error = segunda.Should().Throw<ReglaDeNegocioException>().Which;
        error.Codigo.Should().Be("TRANSICION_INVALIDA");
        error.Razon.Should().Be("Ya firmaste este pedido; la otra firma la da otra persona");
        p.State.Should().Be(SalesOrderState.Confirmado);

        bool ambos(string c) => c is Permisos.PedidoFirmarComercial or Permisos.PedidoFirmarCobranza;
        p.AccionesDisponibles(Doble.UserId, ambos, false, Monedas).Single(a => a.Accion == "autorizar")
            .Should().Be(("autorizar", false, "Ya firmaste este pedido; la otra firma la da otra persona", (string?)null));
    }

    [Fact]
    public void D38_el_suplente_firma_por_su_rol_y_la_firma_queda_atribuida_a_el()
    {
        var p = Confirmado();
        p.Firmar(Ana, RolFirma.Comercial, Ahora);

        p.Firmar(Suplente, RolFirma.Cobranza, Ahora);

        p.State.Should().Be(SalesOrderState.Autorizado);
        var firma = p.Signatures.Single(f => f.Role == RolFirma.Cobranza);
        (firma.UserId, firma.UserName, firma.IsSubstitute).Should().Be((3L, "Suplente de Cobranza", true));
        p.TransicionesPendientes.Last().Nota.Should().Be("Firma de Cobranza (suplente), 2 de 2");
    }

    [Fact]
    public void D33_un_firmante_revoca_con_motivo_y_regresa_a_Confirmado_sin_firmas()
    {
        var p = Confirmado();
        p.Firmar(Ana, RolFirma.Comercial, Ahora);
        p.Firmar(Beto, RolFirma.Cobranza, Ahora);

        var sinMotivo = () => p.Revocar(Ana, " ", false);
        sinMotivo.Should().Throw<DatosIncompletosException>();

        p.Revocar(Ana, "Cliente pidió otro precio", false);
        p.State.Should().Be(SalesOrderState.Confirmado);
        p.Signatures.Should().BeEmpty();
        p.TransicionesPendientes.Last().Should().Be(new TransicionRegistrada("Autorizado", "Confirmado", "Autorización revocada: Cliente pidió otro precio"));
    }

    [Fact]
    public void D33_quien_no_firmo_no_revoca_y_el_Administrador_si()
    {
        var p = Confirmado();
        p.Firmar(Ana, RolFirma.Comercial, Ahora);

        var otro = () => p.Revocar(Beto, "motivo", false);
        otro.Should().Throw<ReglaDeNegocioException>().Which.Razon.Should().Be("Solo revoca quien firmó el pedido o el Administrador.");
        p.AccionesDisponibles(Beto.UserId, c => c == Permisos.PedidoRevocar, false, Monedas).Single(a => a.Accion == "revocar").Disponible.Should().BeFalse();

        p.Revocar(Admin, "Cambio de condiciones", esAdministrador: true);
        p.Signatures.Should().BeEmpty();
        p.TransicionesPendientes.Last().Should().Be(new TransicionRegistrada("Confirmado", "Confirmado", "Autorización revocada: Cambio de condiciones"));
    }

    [Fact]
    public void D147_editar_con_firmas_sin_confirmar_se_rechaza_y_con_confirmacion_revoca_en_la_misma_operacion()
    {
        var p = Confirmado();
        p.Firmar(Ana, RolFirma.Comercial, Ahora);
        p.Firmar(Beto, RolFirma.Cobranza, Ahora);
        var cambio = Datos(domicilio: 901, lineas: [new LineaSolicitada(p.Lines[0].Id, Producto(), 1500, 0.85m, null, null)]);

        var sinConfirmar = () => p.Editar(cambio, revocarAutorizacion: false, Monedas);
        var error = sinConfirmar.Should().Throw<ReglaDeNegocioException>().Which;
        error.Codigo.Should().Be("EDICION_REVOCA_AUTORIZACION");
        error.Razon.Should().Be("El pedido tiene 2 firmas: guardar un cambio revoca la autorización.");
        p.State.Should().Be(SalesOrderState.Autorizado);

        p.Editar(cambio, revocarAutorizacion: true, Monedas);
        p.State.Should().Be(SalesOrderState.Confirmado);
        p.Signatures.Should().BeEmpty();
        p.Lines.Single().RequestedQty.Should().Be(1500);
        p.TransicionesPendientes.TakeLast(2).Should().Equal(
            new TransicionRegistrada("Autorizado", "Autorizado", "Pedido editado"),
            new TransicionRegistrada("Autorizado", "Confirmado", "Revocada por edición"));
    }

    [Fact]
    public void D147_con_una_firma_el_aviso_lo_dice_y_editar_deja_Confirmado()
    {
        var p = Confirmado();
        p.Firmar(Ana, RolFirma.Comercial, Ahora);

        p.AccionesDisponibles(9, c => c == Permisos.PedidoEditar, false, Monedas).Single(a => a.Accion == "editar").Aviso
            .Should().Be("El pedido tiene 1 firma: guardar un cambio revoca la autorización.");
        p.Editar(Datos(domicilio: 902), true, Monedas);
        p.State.Should().Be(SalesOrderState.Confirmado);
        p.TransicionesPendientes.Last().Nota.Should().Be("Revocada por edición");
    }

    [Theory]
    [InlineData(SalesOrderState.Borrador)]
    [InlineData(SalesOrderState.Confirmado)]
    [InlineData(SalesOrderState.Autorizado)]
    public void FR026_cancelar_con_motivo_desde_cada_estado_y_un_cancelado_no_se_edita(SalesOrderState desde)
    {
        var p = SalesOrder.Crear("PV-1", Datos(domicilio: 901), Monedas);
        if (desde != SalesOrderState.Borrador) p.Confirmar(Monedas);
        if (desde == SalesOrderState.Autorizado)
        {
            p.Firmar(Ana, RolFirma.Comercial, Ahora);
            p.Firmar(Beto, RolFirma.Cobranza, Ahora);
        }

        ((Action)(() => p.Cancelar(""))).Should().Throw<DatosIncompletosException>();
        p.Cancelar("El cliente ya no lo quiere");

        p.State.Should().Be(SalesOrderState.Cancelado);
        p.TransicionesPendientes.Last().Should().Be(new TransicionRegistrada(desde.ToString(), "Cancelado", "Cancelado: El cliente ya no lo quiere"));
        var editar = () => p.Editar(Datos(), true, Monedas);
        editar.Should().Throw<ReglaDeNegocioException>().Which.Razon.Should().Be("Un pedido Cancelado no se edita.");
        ((Action)(() => p.Cancelar("otra vez"))).Should().Throw<ReglaDeNegocioException>();
    }

    [Fact]
    public void Un_producto_archivado_no_entra_en_una_linea_nueva_pero_la_que_lo_tiene_lo_conserva()
    {
        var producto = Producto();
        var p = SalesOrder.Crear("PV-1", Datos(lineas: [new LineaSolicitada(null, producto, 10, 1, null, null)]), Monedas);
        producto.ArchivarPorErp();

        p.Confirmar(Monedas);
        p.State.Should().Be(SalesOrderState.Confirmado);
        var nueva = () => p.Editar(Datos(lineas: [new LineaSolicitada(null, Producto(11, activo: false), 1, 1, null, null)]), false, Monedas);
        nueva.Should().Throw<DatosIncompletosException>().Which.Faltantes.Single().Campo.Should().Be("lineas[0].productoId");
    }

    [Fact]
    public void CT26_sin_permiso_cada_accion_dice_su_razon()
    {
        var p = SalesOrder.Crear("PV-1", Datos(), Monedas);

        var acciones = p.AccionesDisponibles(9, _ => false, false, Monedas);

        acciones.Should().AllSatisfy(a => a.Disponible.Should().BeFalse());
        acciones.Single(a => a.Accion == "confirmar").Razon.Should().Be("Tu grupo no tiene el permiso Ventas › Pedido › Confirmar.");
        acciones.Single(a => a.Accion == "autorizar").Razon.Should().Be("Tu grupo no firma la autorización de pedidos.");
    }

    [Fact]
    public void Solo_se_autoriza_un_pedido_Confirmado_y_el_comercial_ve_que_falta()
    {
        var p = SalesOrder.Crear("PV-1", Datos(domicilio: 901), Monedas);
        ((Action)(() => p.Firmar(Ana, RolFirma.Comercial, Ahora))).Should().Throw<ReglaDeNegocioException>();

        p.Confirmar(Monedas);
        p.RolesPorFirmar(Comercial).Should().Equal(RolFirma.Comercial);
        p.Firmar(Ana, RolFirma.Comercial, Ahora);
        p.RolesPorFirmar(Comercial).Should().BeEmpty();
        p.AccionesDisponibles(9, Comercial, false, Monedas).Single(a => a.Accion == "autorizar").Razon.Should().Be("Ya está firmado por Comercial.");
    }
}
