using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Erp;
using PolyConecta.Application.Plataforma.Sincronizacion;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Ventas;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.Application.Tests.Sincronizacion;

/// <summary>Sincronización de catálogos con un bridge falso (L2-T015, R-05, D-150, SC-004).</summary>
public class SincronizacionTests(SqlServerFixture sql)
{
    /// <summary>Bridge falso: páginas de su lista, con la opción de fallar en una página.</summary>
    private sealed class BridgeFalso : IBridgeLecturas
    {
        public List<ProductoLeido> Productos { get; } = [];
        public List<DatosErpCliente> Clientes { get; } = [];
        public List<AgenteLeido> Agentes { get; } = [];
        public List<AlmacenLeido> Almacenes { get; } = [];
        public int Pagina { get; set; } = 2;
        public int? FallarEnPagina { get; set; }

        private Task<PaginaLectura<T>> Paginar<T>(List<T> lista, string? cursor)
        {
            var desde = cursor is null ? 0 : int.Parse(cursor, System.Globalization.CultureInfo.InvariantCulture);
            if (FallarEnPagina is { } n && desde / Pagina + 1 == n) throw new LecturaBridgeException("El bridge se reinició a media lectura.");
            var items = lista.Skip(desde).Take(Pagina).ToList();
            var siguiente = desde + Pagina < lista.Count ? (desde + Pagina).ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
            return Task.FromResult(new PaginaLectura<T>(items, siguiente));
        }

        public Task<PaginaLectura<ProductoLeido>> ProductosAsync(int limite, string? cursor, CancellationToken cancellationToken = default) => Paginar(Productos, cursor);
        public Task<PaginaLectura<DatosErpCliente>> ClientesAsync(int limite, string? cursor, CancellationToken cancellationToken = default) => Paginar(Clientes, cursor);
        public Task<PaginaLectura<AgenteLeido>> AgentesAsync(int limite, string? cursor, CancellationToken cancellationToken = default) => Paginar(Agentes, cursor);
        public Task<IReadOnlyList<AlmacenLeido>> AlmacenesAsync(CancellationToken cancellationToken = default) =>
            FallarEnPagina is null ? Task.FromResult<IReadOnlyList<AlmacenLeido>>(Almacenes) : throw new LecturaBridgeException("Bridge caído.");
    }

    private static ProductoLeido Producto(long id, string codigo, bool activo = true, string? clasificacion = "PT") =>
        new(new DatosErpProducto(id, codigo, "Producto " + codigo, "PZA", id % 2 == 0, activo),
            clasificacion is null ? null : new ClasificacionLeida(clasificacion, "PRODUCTO TERMINADO"));

    private static BridgeFalso Semilla()
    {
        var b = new BridgeFalso();
        for (var i = 1; i <= 5; i++) b.Productos.Add(Producto(i, $"P-{i}"));
        b.Clientes.Add(new DatosErpCliente(57, "EMM-001", "EMPRESA MEXICANA", "EMM010101AAA", true, "USD",
            [new DatosDomicilio(901, TipoDomicilio.Fiscal, "Av. Industrial", "120", null, null, "66600", "Apodaca", null, "NL", "México", null),
             new DatosDomicilio(902, TipoDomicilio.Envio, "Carretera", "Km 18", null, null, "66600", "Apodaca", null, "NL", "México", "Planta Norte")]));
        b.Clientes.Add(new DatosErpCliente(58, "CLI-002", "COMERCIALIZADORA", null, true, "MXN", []));
        b.Agentes.Add(new AgenteLeido(3, "AG-01", "Celia Villarreal", TipoAgente.Venta));
        b.Almacenes.Add(new AlmacenLeido(1, "MP-PIM", "Materia prima PIM"));
        b.Almacenes.Add(new AlmacenLeido(2, "PT-SC", "Producto terminado SC"));
        return b;
    }

    private async Task<(Entorno Entorno, ServiceProvider Servicios)> PrepararAsync(BridgeFalso bridge)
    {
        var entorno = await Entorno.CrearAsync(sql);
        return (entorno, ServiciosDePrueba.Crear(entorno, configurar: s => s.AddSingleton<IBridgeLecturas>(bridge)));
    }

    private static async Task<EstadoCatalogo> SincronizarAsync(ServiceProvider sp, string catalogo)
    {
        await using var scope = sp.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IUseCase<SincronizarCatalogo, EstadoCatalogo>>()
            .ExecuteAsync(new SincronizarCatalogo(catalogo));
    }

    [Fact]
    public async Task La_primera_corrida_trae_todo_y_una_sin_cambios_no_cambia_nada()
    {
        var bridge = Semilla();
        var (entorno, sp) = await PrepararAsync(bridge);
        await using var _ = sp;

        await using (var scope = sp.CreateAsyncScope())
        {
            var todo = await scope.ServiceProvider.GetRequiredService<IUseCase<SincronizarTodo, IReadOnlyList<EstadoCatalogo>>>().ExecuteAsync(new SincronizarTodo());
            todo.Select(e => (e.Catalogo, e.Resultado, e.Leidos, e.Cambiados))
                .Should().Equal(("almacenes", "Exito", 2, 2), ("agentes", "Exito", 1, 1), ("clientes", "Exito", 2, 2), ("productos", "Exito", 5, 5));
        }

        await using (var db = entorno.Contexto())
        {
            (await db.Productos.CountAsync()).Should().Be(5);
            var emm = await db.Clientes.Include(c => c.Addresses).SingleAsync(c => c.ErpCode == "EMM-001");
            emm.Currency.Should().Be("USD");
            emm.Addresses.Should().HaveCount(2);
            (await db.Productos.Include(p => p.PackagingUnits).FirstAsync()).PackagingUnits.Should().ContainSingle(u => u.IsErpBaseUnit);
        }

        // SC-004: sin cambios en CONTPAQi, cero modificados y ningún registro tocado.
        var segunda = await SincronizarAsync(sp, "productos");
        segunda.Cambiados.Should().Be(0);
        segunda.Archivados.Should().Be(0);
        (await SincronizarAsync(sp, "clientes")).Cambiados.Should().Be(0);
        await using var verificar = entorno.Contexto();
        (await verificar.Productos.CountAsync(p => p.ModifiedAt != null)).Should().Be(0);
    }

    [Fact]
    public async Task Un_producto_inactivo_o_ausente_se_archiva_y_uno_que_vuelve_se_restaura()
    {
        var bridge = Semilla();
        var (entorno, sp) = await PrepararAsync(bridge);
        await using var _ = sp;
        await SincronizarAsync(sp, "productos");

        bridge.Productos[0] = Producto(1, "P-1", activo: false);
        bridge.Productos.RemoveAt(4);
        var r = await SincronizarAsync(sp, "productos");
        r.Archivados.Should().Be(2);

        await using (var db = entorno.Contexto())
        {
            (await db.Productos.Select(p => p.ErpCode).ToListAsync()).Should().BeEquivalentTo(["P-2", "P-3", "P-4"]);
            (await db.Productos.IgnoreQueryFilters().CountAsync()).Should().Be(5, "se archiva, no se borra");
        }

        bridge.Productos[0] = Producto(1, "P-1");
        (await SincronizarAsync(sp, "productos")).Cambiados.Should().Be(1);
        await using var otra = entorno.Contexto();
        (await otra.Productos.CountAsync()).Should().Be(4);
    }

    [Fact]
    public async Task Si_la_lectura_falla_a_la_mitad_queda_en_Error_y_no_archiva_nada()
    {
        var bridge = Semilla();
        var (entorno, sp) = await PrepararAsync(bridge);
        await using var _ = sp;
        await SincronizarAsync(sp, "productos");

        bridge.FallarEnPagina = 2;
        var r = await SincronizarAsync(sp, "productos");

        r.Resultado.Should().Be("Error");
        r.Error.Should().Contain("se reinició");
        r.Leidos.Should().Be(5, "conserva los conteos de la última exitosa");
        await using var db = entorno.Contexto();
        (await db.Productos.CountAsync()).Should().Be(5);

        bridge.FallarEnPagina = null;
        (await SincronizarAsync(sp, "productos")).Resultado.Should().Be("Exito");
    }

    [Fact]
    public async Task D86_la_clasificacion_de_CONTPAQi_es_el_valor_inicial_y_la_editada_no_se_pisa()
    {
        var bridge = Semilla();
        var (entorno, sp) = await PrepararAsync(bridge);
        await using var _ = sp;
        await SincronizarAsync(sp, "productos");

        long propia;
        await using (var db = entorno.Contexto())
        {
            var pt = await db.Clasificaciones.SingleAsync();
            pt.ErpValue.Should().Be("PT");
            (await db.Productos.AllAsync(p => p.ClassificationId == pt.Id)).Should().BeTrue();

            var nueva = new ProductClassification("BOLSA", "Bolsa");
            db.Clasificaciones.Add(nueva);
            await db.SaveChangesAsync();
            propia = nueva.Id;
            (await db.Productos.SingleAsync(p => p.ErpCode == "P-1")).Clasificar(propia);
            await db.SaveChangesAsync();
        }

        bridge.Productos[0] = Producto(1, "P-1", clasificacion: "MP");
        await SincronizarAsync(sp, "productos");

        await using var verificar = entorno.Contexto();
        (await verificar.Productos.SingleAsync(p => p.ErpCode == "P-1")).ClassificationId.Should().Be(propia);
    }

    [Fact]
    public async Task Un_domicilio_que_ya_no_viene_se_archiva_y_el_cliente_inactivo_tambien()
    {
        var bridge = Semilla();
        var (entorno, sp) = await PrepararAsync(bridge);
        await using var _ = sp;
        await SincronizarAsync(sp, "clientes");

        var emm = bridge.Clientes[0];
        bridge.Clientes[0] = emm with { Domicilios = [emm.Domicilios![0]] };
        bridge.Clientes[1] = bridge.Clientes[1] with { Activo = false };
        var r = await SincronizarAsync(sp, "clientes");

        r.Cambiados.Should().Be(1);
        r.Archivados.Should().Be(1);
        await using var db = entorno.Contexto();
        (await db.Clientes.Include(c => c.Addresses).SingleAsync()).Addresses.Should().ContainSingle(d => d.Kind == TipoDomicilio.Fiscal);
    }

    [Fact]
    public async Task Una_segunda_sincronizacion_del_mismo_catalogo_a_la_vez_da_SINCRONIZACION_EN_CURSO()
    {
        var bridge = Semilla();
        var (_, sp) = await PrepararAsync(bridge);
        await using var _ = sp;

        await using var primera = sp.CreateAsyncScope();
        var uow = primera.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await uow.BeginTransactionAsync();
        (await primera.ServiceProvider.GetRequiredService<ICandadoDeSincronizacion>().TomarAsync("productos")).Should().NotBeNull();

        var segunda = () => SincronizarAsync(sp, "productos");

        (await segunda.Should().ThrowAsync<ReglaDeNegocioException>()).Which.Codigo.Should().Be("SINCRONIZACION_EN_CURSO");
        (await SincronizarAsync(sp, "clientes")).Resultado.Should().Be("Exito", "el candado es por catálogo");
        await uow.RollbackAsync();
    }
}
