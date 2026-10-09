using System.Text.Json;
using AwesomeAssertions;
using PolyConecta.Application.Common;
using PolyConecta.Application.Common.Listas;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Domain.Ventas;
using Xunit;

namespace PolyConecta.Application.Tests.Listas;

public class VistasDeBusquedaTests
{
    private sealed class UsuarioPrueba(string usuario) : ICurrentUser
    {
        public string UserName => usuario;
        public long? UserId => 1;
        public string NombreVisible => usuario;
        public string? GrupoEjercido => "ATENCION_CLIENTES";
        public bool EsSuplente => false;
        public void EjercerGrupo(string? grupo, bool esSuplente) { }
    }

    [Fact]
    public void VistasDeF1_declara_las_cinco_listas_con_sus_permisos_y_llaves()
    {
        VistasDeF1.Pedidos.Llave.Should().Be("ventas.pedidos");
        VistasDeF1.Pedidos.PermisoLectura.Should().Be(Permisos.PedidoLeer);
        VistasDeF1.Pedidos.Modulo.Should().Be("ventas");
        VistasDeF1.Pedidos.Lista.Should().Be("pedidos");

        VistasDeF1.Productos.Llave.Should().Be("inventario.productos");
        VistasDeF1.Productos.PermisoLectura.Should().Be(Permisos.ProductoLeer);

        VistasDeF1.Clientes.Llave.Should().Be("ventas.clientes");
        VistasDeF1.Clientes.PermisoLectura.Should().Be(Permisos.ClienteLeer);

        VistasDeF1.Usuarios.Llave.Should().Be("plataforma.usuarios");
        VistasDeF1.Usuarios.PermisoLectura.Should().Be(Permisos.UsuariosLeer);

        VistasDeF1.Grupos.Llave.Should().Be("plataforma.grupos");
        VistasDeF1.Grupos.PermisoLectura.Should().Be(Permisos.GruposLeer);
    }

    [Fact]
    public void DescribirVista_de_pedidos_cumple_el_contrato_de_api_listas()
    {
        var dto = VistasDeF1.Pedidos.DescribirVista();

        dto.Lista.Should().Be("ventas.pedidos");
        dto.Columnas.Select(c => c.Campo).Should().Equal("folio", "cliente", "fechaPromesa", "estado");
        dto.Campos.Select(c => c.Campo).Should().Equal("folio", "cliente");
        dto.Filtros.Select(f => f.Nombre).Should().Equal("Borrador", "Confirmado", "Mis pedidos", "Por autorizar");
        dto.Agrupaciones.Select(a => a.Campo).Should().Equal("estado", "cliente");
    }

    [Fact]
    public void Validar_acepta_consulta_valida()
    {
        var c = new ConsultaLista(
            Pagina: 0,
            Tamano: 80,
            Orden: [new("folio", Desc: true)],
            Filtros: [new("estado", "igual", "Confirmado")],
            Nombrados: ["Confirmado", "Mis pedidos"],
            Busqueda: "EMM",
            AgruparPor: ["cliente"],
            Grupo: [new("cliente", "EMM-001")]
        );

        var accion = () => VistasDeF1.Pedidos.Validar(c);
        accion.Should().NotThrow();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validar_rechaza_tamano_de_pagina_invalido(int tamanoInvalido)
    {
        var c = new ConsultaLista(Tamano: tamanoInvalido);
        var ex = Assert.Throws<ValidacionException>(() => VistasDeF1.Pedidos.Validar(c));
        ex.Errores.Should().Contain(e => e.Campo == "tamano");
    }

    [Fact]
    public void Validar_rechaza_pagina_negativa()
    {
        var c = new ConsultaLista(Pagina: -1);
        var ex = Assert.Throws<ValidacionException>(() => VistasDeF1.Pedidos.Validar(c));
        ex.Errores.Should().Contain(e => e.Campo == "pagina");
    }

    [Fact]
    public void Validar_rechaza_columna_de_orden_desconocida()
    {
        var c = new ConsultaLista(Orden: [new("desconocido", Desc: false)]);
        var ex = Assert.Throws<ValidacionException>(() => VistasDeF1.Pedidos.Validar(c));
        ex.Errores.Should().Contain(e => e.Campo == "orden");
    }

    [Fact]
    public void Validar_rechaza_operador_desconocido()
    {
        var c = new ConsultaLista(Filtros: [new("estado", "mayorQue", "1")]);
        var ex = Assert.Throws<ValidacionException>(() => VistasDeF1.Pedidos.Validar(c));
        ex.Errores.Should().Contain(e => e.Campo == "filtros");
    }

    [Fact]
    public void Validar_rechaza_filtro_nombrado_desconocido()
    {
        var c = new ConsultaLista(Nombrados: ["Inexistente"]);
        var ex = Assert.Throws<ValidacionException>(() => VistasDeF1.Pedidos.Validar(c));
        ex.Errores.Should().Contain(e => e.Campo == "nombrados");
    }

    [Fact]
    public void Validar_rechaza_agrupacion_desconocida()
    {
        var c = new ConsultaLista(AgruparPor: ["vendedor"]);
        var ex = Assert.Throws<ValidacionException>(() => VistasDeF1.Pedidos.Validar(c));
        ex.Errores.Should().Contain(e => e.Campo == "agruparPor");
    }

    [Fact]
    public void EvaluarFiltros_en_pedido_detecta_filtros_nombrados()
    {
        var cliente = Customer.DesdeErp(new(1, "CLI-01", "Cliente SA", "RFC01", true, "MXN", null));
        var monedas = ReglasDeMoneda.PorOmision;
        var p = SalesOrder.Crear("PV-2026-0001", new(cliente, null, null, DateOnly.FromDateTime(DateTime.Today), null, null, "MXN", 1m, []), monedas);
        p.MarcarCreado(DateTimeOffset.UtcNow, "cvillarreal");

        var uCelia = new UsuarioPrueba("cvillarreal");
        var uDiana = new UsuarioPrueba("diana");

        var filtrosCelia = VistasDeF1.Pedidos.EvaluarFiltros(p, uCelia);
        filtrosCelia.Should().Contain("Borrador");
        filtrosCelia.Should().Contain("Mis pedidos");
        filtrosCelia.Should().NotContain("Confirmado");

        var filtrosDiana = VistasDeF1.Pedidos.EvaluarFiltros(p, uDiana);
        filtrosDiana.Should().Contain("Borrador");
        filtrosDiana.Should().NotContain("Mis pedidos");
    }

    [Fact]
    public void Deserializacion_de_ids_acepta_numeros_y_cadenas()
    {
        var jsonNumeros = """{ "pagina": 0, "tamano": 80, "ids": [15, 20] }""";
        var jsonCadenas = """{ "pagina": 0, "tamano": 80, "ids": ["15", "20"] }""";

        var c1 = JsonSerializer.Deserialize<ConsultaLista>(jsonNumeros, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var c2 = JsonSerializer.Deserialize<ConsultaLista>(jsonCadenas, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        c1!.Ids.Should().Equal("15", "20");
        c2!.Ids.Should().Equal("15", "20");
    }
}
