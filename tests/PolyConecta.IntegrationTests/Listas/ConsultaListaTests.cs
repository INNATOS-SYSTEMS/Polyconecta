using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PolyConecta.Application.Common;
using PolyConecta.Application.Common.Listas;
using PolyConecta.Application.Plataforma.Seguridad;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Domain.Ventas;
using PolyConecta.Infrastructure;
using PolyConecta.Infrastructure.Persistence;
using PolyConecta.Infrastructure.Plataforma.Listas;
using PolyConecta.IntegrationTests.Plataforma;
using PolyConecta.IntegrationTests.Soporte;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.IntegrationTests.Listas;

/// <summary>
/// Pruebas de integración de la consulta de listas y modo híbrido (L2-T028, US4, contracts/api-listas.md, D-151).
/// </summary>
public class ConsultaListaTests(SqlServerFixture sql)
{
    private static async Task<JsonNode> JsonAsync(HttpResponseMessage r)
    {
        var texto = await r.Content.ReadAsStringAsync();
        r.IsSuccessStatusCode.Should().BeTrue(texto);
        return JsonNode.Parse(texto)!;
    }

    private static async Task<(ApiDePrueba Api, Entorno Entorno, Catalogo Cat, HttpClient Admin, HttpClient Ac, HttpClient Calidad)> PrepararApiAsync(
        SqlServerFixture sql, IDictionary<string, string?>? ajustes = null)
    {
        var entorno = await Entorno.CrearAsync(sql);
        var cat = await CatalogoDePrueba.SembrarAsync(entorno);
        var api = new ApiDePrueba(entorno, ajustes);
        await api.CrearUsuarioAsync("ac1", "Celia Villarreal", (GruposIniciales.AtencionClientes, "PIM", false));
        await api.CrearUsuarioAsync("calidad1", "Inspector Calidad", (GruposIniciales.Calidad, "PIM", false));
        return (api, entorno, cat, await api.ClienteAsync("admin"), await api.ClienteAsync("ac1"), await api.ClienteAsync("calidad1"));
    }

    private static async Task SembrarPedidosAsync(Entorno entorno, Catalogo cat, int cantidad = 5)
    {
        await using var db = entorno.Contexto();
        var clienteEmm = await db.Clientes.SingleAsync(c => c.Id == cat.Emm);
        var clienteCli = await db.Clientes.SingleAsync(c => c.Id == cat.Cli002);
        var producto = await db.Productos.Include(p => p.PackagingUnits).SingleAsync(p => p.Id == cat.Bolsa);
        var monedas = ReglasDeMoneda.PorOmision;

        for (int i = 1; i <= cantidad; i++)
        {
            var cliente = i % 2 == 0 ? clienteCli : clienteEmm;
            var moneda = i % 2 == 0 ? "MXN" : "USD";
            var folio = $"PV-2026-{i:D4}";
            var datos = new DatosPedido(
                cliente, $"OC-{i}", null,
                DateOnly.FromDateTime(DateTime.Today),
                DateOnly.FromDateTime(DateTime.Today.AddDays(i)),
                null, moneda, 1m,
                [new LineaSolicitada(null, producto, 100 * i, 15.5m, null, null)]);

            var pedido = SalesOrder.Crear(folio, datos, monedas);
            var creador = i % 2 == 1 ? "ac1" : "admin";
            pedido.MarcarCreado(DateTimeOffset.UtcNow, creador);

            if (i >= 2) pedido.Confirmar(monedas);

            db.Pedidos.Add(pedido);
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GET_vista_responde_la_descripcion_declarada_de_pedidos()
    {
        var (api, _, _, admin, _, _) = await PrepararApiAsync(sql);
        await using var _ = api;

        var r = await admin.GetAsync("/api/v1/ventas/pedidos/vista");
        var vista = await JsonAsync(r);

        vista["lista"]!.GetValue<string>().Should().Be("ventas.pedidos");
        var columnas = vista["columnas"]!.AsArray().Select(c => c!["campo"]!.GetValue<string>()).ToList();
        columnas.Should().Contain(["folio", "cliente", "fechaPromesa", "estado"]);

        var filtros = vista["filtros"]!.AsArray().Select(f => f!["nombre"]!.GetValue<string>()).ToList();
        filtros.Should().Equal("Borrador", "Confirmado", "Mis pedidos", "Por autorizar");

        var agrupaciones = vista["agrupaciones"]!.AsArray().Select(a => a!["campo"]!.GetValue<string>()).ToList();
        agrupaciones.Should().Equal("estado", "cliente");
    }

    [Fact]
    public async Task POST_conjunto_bajo_el_umbral_entrega_completo_con_filtros_evaluados_por_usuario()
    {
        var (api, entorno, cat, admin, ac, _) = await PrepararApiAsync(sql);
        await using var _ = api;

        await SembrarPedidosAsync(entorno, cat, cantidad: 4);

        // Llamada como AC1 (creó pedidos impares: PV-2026-0001 y PV-2026-0003)
        var rAc = await ac.PostAsJsonAsync("/api/v1/ventas/pedidos/conjunto", new { });
        var conjuntoAc = await JsonAsync(rAc);

        conjuntoAc["completo"]!.GetValue<bool>().Should().BeTrue();
        conjuntoAc["total"]!.GetValue<int>().Should().Be(4);
        var filasAc = conjuntoAc["filas"]!.AsArray();
        filasAc.Count.Should().Be(4);

        // PV-2026-0001 (impar, creado por ac1, en Borrador)
        var p1 = filasAc.Single(f => f!["folio"]!.GetValue<string>() == "PV-2026-0001");
        var filtrosP1 = p1!["_filtros"]!.AsArray().Select(x => x!.GetValue<string>()).ToList();
        filtrosP1.Should().Contain("Borrador");
        filtrosP1.Should().Contain("Mis pedidos"); // Creado por ac1

        // Llamada como admin (no creó PV-2026-0001)
        var rAdmin = await admin.PostAsJsonAsync("/api/v1/ventas/pedidos/conjunto", new { });
        var conjuntoAdmin = await JsonAsync(rAdmin);
        var filasAdmin = conjuntoAdmin["filas"]!.AsArray();
        var p1Admin = filasAdmin.Single(f => f!["folio"]!.GetValue<string>() == "PV-2026-0001");
        var filtrosP1Admin = p1Admin!["_filtros"]!.AsArray().Select(x => x!.GetValue<string>()).ToList();
        filtrosP1Admin.Should().Contain("Borrador");
        filtrosP1Admin.Should().NotContain("Mis pedidos"); // Para admin no es "Mis pedidos"
    }

    [Fact]
    public async Task POST_conjunto_sobre_el_umbral_devuelve_completo_false_y_sin_filas()
    {
        // Configuramos umbral bajo: 2
        var ajustes = new Dictionary<string, string?> { ["Listas:Umbral"] = "2" };
        var (api, entorno, cat, admin, _, _) = await PrepararApiAsync(sql, ajustes);
        await using var _ = api;

        await SembrarPedidosAsync(entorno, cat, cantidad: 5);

        var r = await admin.PostAsJsonAsync("/api/v1/ventas/pedidos/conjunto", new { });
        var json = await JsonAsync(r);

        json["completo"]!.GetValue<bool>().Should().BeFalse();
        json["total"]!.GetValue<int>().Should().Be(5);
        json["filas"].Should().BeNull();
    }

    [Fact]
    public async Task Endpoints_de_lista_exigen_sesion_permiso_y_validan_parametros()
    {
        var (api, _, _, admin, _, calidad) = await PrepararApiAsync(sql);
        await using var _ = api;

        // 1. Sin sesión -> 401
        var clienteAnonimo = api.CreateClient();
        clienteAnonimo.DefaultRequestHeaders.Add("X-Requested-With", "PolyConecta");
        (await clienteAnonimo.GetAsync("/api/v1/ventas/pedidos/vista")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await clienteAnonimo.PostAsJsonAsync("/api/v1/ventas/pedidos/conjunto", new { })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 2. Sin permiso de lectura de la lista (Calidad no lee pedidos) -> 403 con razón
        var r403 = await calidad.GetAsync("/api/v1/ventas/pedidos/vista");
        r403.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var json403 = JsonNode.Parse(await r403.Content.ReadAsStringAsync())!;
        json403["code"]!.GetValue<string>().Should().Be("PERMISO_DENEGADO");
        json403["razon"].Should().NotBeNull();

        // 3. Lista desconocida -> 404
        (await admin.GetAsync("/api/v1/ventas/inexistente/vista")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 4. Parámetros inválidos en consulta -> 400 VALIDACION
        var rTamanoInvalido = await admin.PostAsJsonAsync("/api/v1/ventas/pedidos/consulta", new { tamano = 99 });
        rTamanoInvalido.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var rFiltroInvalido = await admin.PostAsJsonAsync("/api/v1/ventas/pedidos/consulta", new { nombrados = new[] { "FiltroFalso" } });
        rFiltroInvalido.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_consulta_filtra_con_O_dentro_del_campo_y_con_Y_entre_campos_US4_escenario_1()
    {
        var (api, entorno, cat, _, ac, _) = await PrepararApiAsync(sql);
        await using var _ = api;

        await SembrarPedidosAsync(entorno, cat, cantidad: 6);

        // Estado: Borrador (pedido 1) o Confirmado (pedidos 2, 3, 4, 5, 6) -> 6 pedidos
        // Responsable: "Mis pedidos" (pedidos 1, 3, 5 creados por ac1)
        // Intersección (Y): pedidos creados por ac1 que están en Borrador o Confirmado -> pedidos 1, 3, 5
        var r = await ac.PostAsJsonAsync("/api/v1/ventas/pedidos/consulta", new
        {
            pagina = 0,
            tamano = 80,
            nombrados = new[] { "Borrador", "Confirmado", "Mis pedidos" },
        });

        var res = await JsonAsync(r);
        res["total"]!.GetValue<int>().Should().Be(3);
        var folios = res["filas"]!.AsArray().Select(f => f!["folio"]!.GetValue<string>()).ToList();
        folios.Should().Equal("PV-2026-0005", "PV-2026-0003", "PV-2026-0001");
    }

    [Fact]
    public async Task POST_consulta_busqueda_orden_y_seleccion_por_ids()
    {
        var (api, entorno, cat, admin, _, _) = await PrepararApiAsync(sql);
        await using var _ = api;

        await SembrarPedidosAsync(entorno, cat, cantidad: 6);

        // Búsqueda por "0002"
        var rBusqueda = await admin.PostAsJsonAsync("/api/v1/ventas/pedidos/consulta", new
        {
            pagina = 0,
            tamano = 80,
            busqueda = "0002",
        });
        var resBusqueda = await JsonAsync(rBusqueda);
        resBusqueda["total"]!.GetValue<int>().Should().Be(1);
        resBusqueda["filas"]!.AsArray()[0]!["folio"]!.GetValue<string>().Should().Be("PV-2026-0002");

        // Orden ascendente por folio
        var rOrden = await admin.PostAsJsonAsync("/api/v1/ventas/pedidos/consulta", new
        {
            pagina = 0,
            tamano = 80,
            orden = new[] { new { campo = "folio", desc = false } },
        });
        var resOrden = await JsonAsync(rOrden);
        var primeros = resOrden["filas"]!.AsArray().Select(f => f!["folio"]!.GetValue<string>()).Take(2).ToList();
        primeros.Should().Equal("PV-2026-0001", "PV-2026-0002");

        // Selección por ids: devuelve solo los solicitados
        var id1 = resOrden["filas"]!.AsArray()[0]!["id"]!.GetValue<long>();
        var id3 = resOrden["filas"]!.AsArray()[2]!["id"]!.GetValue<long>();

        var rIds = await admin.PostAsJsonAsync("/api/v1/ventas/pedidos/consulta", new
        {
            pagina = 0,
            tamano = 80,
            ids = new[] { id1.ToString(), id3.ToString() },
        });
        var resIds = await JsonAsync(rIds);
        resIds["filas"]!.AsArray().Count.Should().Be(2);
        resIds["filas"]!.AsArray().Select(f => f!["id"]!.GetValue<long>()).Should().BeEquivalentTo([id1, id3]);
    }

    [Fact]
    public async Task POST_consulta_agrupacion_y_paginacion_de_grupos_D_140_y_US4_escenario_2()
    {
        var (api, entorno, cat, admin, _, _) = await PrepararApiAsync(sql);
        await using var _ = api;

        await SembrarPedidosAsync(entorno, cat, cantidad: 6);

        // Agrupado por cliente (hay 2 clientes: EMM-001 y CLI-002)
        var r = await admin.PostAsJsonAsync("/api/v1/ventas/pedidos/consulta", new
        {
            pagina = 0,
            tamano = 80,
            agruparPor = new[] { "cliente" },
        });

        var res = await JsonAsync(r);
        res["filas"]!.AsArray().Should().BeEmpty();
        res["grupos"].Should().NotBeNull();
        var grupos = res["grupos"]!.AsArray();
        grupos.Count.Should().Be(2);
        res["total"]!.GetValue<int>().Should().Be(2, "el total cuenta grupos (D-140)");

        // Abrir un grupo (ruta de grupo): trae las filas de ese cliente
        var rGrupoAbierto = await admin.PostAsJsonAsync("/api/v1/ventas/pedidos/consulta", new
        {
            pagina = 0,
            tamano = 80,
            agruparPor = new[] { "cliente" },
            grupo = new[] { new { campo = "cliente", valor = "EMM-001" } },
        });

        var resAbierto = await JsonAsync(rGrupoAbierto);
        resAbierto["grupos"].Should().BeNull();
        resAbierto["filas"]!.AsArray().Count.Should().Be(3);
        resAbierto["total"]!.GetValue<int>().Should().Be(3);
    }

    private sealed class SoloPedidosMxn : IReglaDeFila<SalesOrder>
    {
        public Expression<Func<SalesOrder, bool>> Filtro(ContextoDeReglas contexto) =>
            o => o.Currency == "MXN";
    }

    [Fact]
    public async Task Regla_de_fila_se_aplica_en_conjunto_y_consulta_y_quitar_filtros_no_la_amplia_US4_escenario_6()
    {
        var entorno = await Entorno.CrearAsync(sql);
        var cat = await CatalogoDePrueba.SembrarAsync(entorno);
        await SembrarPedidosAsync(entorno, cat, cantidad: 4); // 2 MXN y 2 USD

        var usuario = new UsuarioFijo("agente_mxn", id: 99);
        var asignaciones = new AsignacionEfectiva(1, "ATENCION_CLIENTES", "Atención", 1, "PIM", false, new HashSet<string> { Permisos.PedidoLeer });

        var sp = ServiciosDePrueba.Crear(entorno, configurar: s =>
        {
            s.AddSingleton<ICurrentUser>(usuario);
            s.AddSingleton<IPermisosDelUsuario>(new PermisosFijos(asignaciones));
            s.AddSingleton<IReglaDeFila<SalesOrder>, SoloPedidosMxn>();
        });

        await using var scope = sp.CreateAsyncScope();
        var consultaLista = scope.ServiceProvider.GetRequiredService<IConsultaDeLista<SalesOrder>>();

        // 1. En el conjunto: solo vienen los pedidos MXN
        var conjunto = await consultaLista.ConjuntoAsync();
        conjunto.Completo.Should().BeTrue();
        conjunto.Total.Should().Be(2);
        conjunto.Filas!.All(f => (string)f["folio"]! is "PV-2026-0002" or "PV-2026-0004").Should().BeTrue();

        // 2. En consulta sin filtros: sigue viendo solo 2
        var resSinFiltros = await consultaLista.ConsultarAsync(new());
        resSinFiltros.Total.Should().Be(2);

        // 3. Quitar todos los filtros o intentar buscar pedidos en Borrador no amplía lo que la regla oculta
        var resConFiltro = await consultaLista.ConsultarAsync(new(
            Filtros: [new("estado", "igual", "Borrador")]
        ));
        resConFiltro.Total.Should().Be(0);
    }

    private sealed class PermisosFijos(params AsignacionEfectiva[] asignaciones) : IPermisosDelUsuario
    {
        public Task<IReadOnlyList<AsignacionEfectiva>> AsignacionesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AsignacionEfectiva>>(asignaciones);
    }

    private sealed class InterceptorContadorConsultas : DbCommandInterceptor
    {
        public int Conteo { get; set; }

        public override InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
            System.Data.Common.DbCommand command,
            CommandEventData eventData,
            InterceptionResult<System.Data.Common.DbDataReader> result)
        {
            Conteo++;
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
            System.Data.Common.DbCommand command,
            CommandEventData eventData,
            InterceptionResult<System.Data.Common.DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Conteo++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    [Fact]
    public async Task Modo_servidor_ejecuta_exactamente_dos_consultas_SC_005()
    {
        var entorno = await Entorno.CrearAsync(sql);
        var cat = await CatalogoDePrueba.SembrarAsync(entorno);
        await SembrarPedidosAsync(entorno, cat, cantidad: 5);

        var interceptor = new InterceptorContadorConsultas();
        var sp = ServiciosDePrueba.Crear(entorno, configurar: s =>
        {
            s.AddSingleton<ICurrentUser>(new UsuarioFijo("admin", id: 1));
            s.AddSingleton<IPermisosDelUsuario>(new PermisosFijos(
                new AsignacionEfectiva(1, "ADMINISTRADOR", "Admin", 1, "PIM", false, new HashSet<string> { Permisos.PedidoLeer })));
            s.AddScoped<PolyDbContext>(sp2 => new PruebasDbContext(
                new DbContextOptionsBuilder<PolyDbContext>()
                    .UseSqlServer(entorno.Conexion)
                    .AddInterceptors(interceptor)
                    .Options));
        });

        await using var scope = sp.CreateAsyncScope();
        var consultaLista = scope.ServiceProvider.GetRequiredService<IConsultaDeLista<SalesOrder>>();

        interceptor.Conteo = 0;
        var res = await consultaLista.ConsultarAsync(new(Pagina: 0, Tamano: 80));
        res.Total.Should().Be(5);
        res.Filas.Count.Should().Be(5);
        interceptor.Conteo.Should().Be(2, "una por filas o grupos, más una de total (SC-005)");
    }

    [Fact]
    public async Task Con_500_pedidos_cada_consulta_responde_en_menos_de_1_segundo_SC_005()
    {
        var entorno = await Entorno.CrearAsync(sql);
        var cat = await CatalogoDePrueba.SembrarAsync(entorno);

        // Sembrar 500 pedidos
        await using (var db = entorno.Contexto())
        {
            var cliente = await db.Clientes.SingleAsync(c => c.Id == cat.Emm);
            var producto = await db.Productos.Include(p => p.PackagingUnits).SingleAsync(p => p.Id == cat.Bolsa);
            var monedas = ReglasDeMoneda.PorOmision;
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var lista = new List<SalesOrder>(500);
            for (int i = 1; i <= 500; i++)
            {
                var p = SalesOrder.Crear($"PV-2026-{i:D4}", new(
                    cliente, $"OC-{i}", null, hoy, hoy.AddDays(i % 30), null, "MXN", 1m,
                    [new LineaSolicitada(null, producto, 50, 10m, null, null)]
                ), monedas);
                p.MarcarCreado(DateTimeOffset.UtcNow, i % 2 == 0 ? "ac1" : "admin");
                if (i % 2 == 0) p.Confirmar(monedas);
                lista.Add(p);
            }
            db.Pedidos.AddRange(lista);
            await db.SaveChangesAsync();
        }

        var sp = ServiciosDePrueba.Crear(entorno, configurar: s =>
        {
            s.AddSingleton<ICurrentUser>(new UsuarioFijo("admin", id: 1));
            s.AddSingleton<IPermisosDelUsuario>(new PermisosFijos(
                new AsignacionEfectiva(1, "ADMINISTRADOR", "Admin", 1, "PIM", false, new HashSet<string> { Permisos.PedidoLeer })));
        });

        await using var scope = sp.CreateAsyncScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IConsultaDeLista<SalesOrder>>();

        // Medir tiempo de consulta con filtros y orden
        var cronometro = Stopwatch.StartNew();
        var resultado = await servicio.ConsultarAsync(new(
            Pagina: 0,
            Tamano: 80,
            Nombrados: ["Confirmado"],
            Orden: [new("folio", Desc: true)]
        ));
        cronometro.Stop();

        resultado.Total.Should().Be(250);
        resultado.Filas.Count.Should().Be(80);
        cronometro.ElapsedMilliseconds.Should().BeLessThan(1000, "SC-005 exige menos de 1 segundo");
    }

    [Fact]
    public async Task Total_por_unidad_comun_muestra_unidad_si_es_homogenea_y_no_la_muestra_si_se_mezcla_D_140()
    {
        var entorno = await Entorno.CrearAsync(sql);
        await using (var db = entorno.Contexto())
        {
            var p1 = new DocumentoDePrueba("D-01", "PEBD", 100m, "KG", origen: "PIM");
            var p2 = new DocumentoDePrueba("D-02", "PEBD", 200m, "KG", origen: "PIM");
            var p3 = new DocumentoDePrueba("D-03", "BOLSA", 50m, "PZA", origen: "SC");
            db.Documentos.AddRange(p1, p2, p3);
            await db.SaveChangesAsync();
        }

        // Vista con columna sumable y unidad
        var vistaPrueba = new VistaDeBusqueda<DocumentoDePrueba>
        {
            Modulo = "pruebas",
            Lista = "documentos",
            Llave = "pruebas.documentos",
            PermisoLectura = Permisos.PedidoLeer,
            Columnas =
            [
                new() { Campo = "folio", Etiqueta = "Folio", Ordenable = true, Selector = d => d.Folio },
                new() { Campo = "cantidad", Etiqueta = "Cantidad", Sumable = true, Suma = d => d.Cantidad, Selector = d => d.Cantidad },
                new() { Campo = "unidad", Etiqueta = "Unidad", Selector = d => d.Unidad, Unidad = d => d.Unidad },
                new() { Campo = "origen", Etiqueta = "Origen", Selector = d => d.Origen },
            ],
            Campos = [new() { Campo = "folio", Etiqueta = "Folio", Expresion = d => d.Folio }],
            Filtros = [],
            Agrupaciones = [new() { Campo = "origen", Etiqueta = "Origen", Clave = d => d.Origen }],
            Proyector = d => new Dictionary<string, object?> { ["folio"] = d.Folio },
        };

        var sp = ServiciosDePrueba.Crear(entorno, configurar: s =>
        {
            s.AddSingleton<ICurrentUser>(new UsuarioFijo("admin", id: 1));
            s.AddSingleton<IPermisosDelUsuario>(new PermisosFijos(
                new AsignacionEfectiva(1, "ADMINISTRADOR", "Admin", 1, "PIM", false, new HashSet<string> { Permisos.PedidoLeer })));
            s.AddScoped<VistaDeBusqueda<DocumentoDePrueba>>(_ => vistaPrueba);
            s.AddScoped<ConsultaDeListaEf<DocumentoDePrueba>>();
        });

        await using var scope = sp.CreateAsyncScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ConsultaDeListaEf<DocumentoDePrueba>>();

        var res = await servicio.ConsultarAsync(new(AgruparPor: ["origen"]));
        res.Grupos.Should().NotBeNull();
        res.Grupos!.Count.Should().Be(2);

        // Grupo PIM: tiene 2 items con KG -> comparte unidad, muestra totales y textos
        var grupoPim = res.Grupos.Single(g => g.Valor == "PIM");
        grupoPim.Cantidad.Should().Be(2);
        grupoPim.Totales.Should().ContainKey("cantidad");
        grupoPim.Totales["cantidad"].Should().Be(300m);
        grupoPim.Textos.Should().NotBeNull();
        grupoPim.Textos!["unidad"].Should().Be("KG");

        // Grupo SC: tiene 1 item con PZA -> comparte unidad
        var grupoSc = res.Grupos.Single(g => g.Valor == "SC");
        grupoSc.Totales["cantidad"].Should().Be(50m);
        grupoSc.Textos!["unidad"].Should().Be("PZA");

        // El total general sobre todo el filtro tiene unidades mezcladas (KG y PZA) -> no muestra total de cantidad
        res.Totales.Should().NotContainKey("cantidad", "unidades mezcladas en todo el filtro ocultan el total general (D-140)");
    }
}
