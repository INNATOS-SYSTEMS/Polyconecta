using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Contpaq.Bridge.Api.Controllers;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Infrastructure.Persistence;
using Contpaq.Bridge.Simulated;
using Contpaq.Bridge.Tests.Soporte;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Contpaq.Bridge.Tests.Simulated
{
    /// <summary>El catálogo simulado en la forma 1.1 (C-T005) y su cambio en caliente (C-T006).</summary>
    public class CatalogoSimuladoTests
    {
        private static SimulatedCatalog Semilla() =>
            SimulatedCatalog.Cargar(Path.Combine(Entorno.Raiz, "PolyConecta.Contpaq", "Simulated", "seed.json"));

        private static SimulatedReadRepository Repositorio(SimulatedCatalog catalogo)
        {
            var store = new SimulatedStore($"Data Source={Path.Combine(Path.GetTempPath(), $"bridge_{Guid.NewGuid():N}.db")}");
            store.Inicializar();
            return new SimulatedReadRepository(catalogo, store);
        }

        private static CatalogoSimuladoController Controlador(SimulatedCatalog? catalogo)
        {
            var servicios = new ServiceCollection();
            if (catalogo is not null) servicios.AddSingleton(catalogo);
            return new CatalogoSimuladoController(servicios.BuildServiceProvider());
        }

        [Fact]
        public void La_semilla_trae_cinco_clientes_con_moneda_y_domicilios_validos()
        {
            var clientes = Semilla().Clientes;
            Assert.Equal(5, clientes.Count);
            Assert.All(clientes, c => Assert.Contains(c.Moneda, new[] { "MXN", "USD" }));
            Assert.Contains(clientes, c => c.Moneda == "USD");
            Assert.Contains(clientes, c => c.Moneda == "MXN");
            Assert.Contains(clientes, c => c.Domicilios.Count == 0);
            Assert.All(clientes.Where(c => c.Domicilios.Count > 0), c =>
            {
                Assert.Single(c.Domicilios, d => d.Tipo == "fiscal");
                Assert.InRange(c.Domicilios.Count(d => d.Tipo == "envio"), 0, 3);
            });
            Assert.Equal(clientes.Count, clientes.Select(c => c.IdErp).Distinct().Count());
        }

        [Fact]
        public void La_semilla_trae_veinte_productos_con_clasificacion_unidades_y_dos_inactivos()
        {
            var productos = Semilla().Productos;
            Assert.Equal(20, productos.Count);
            Assert.Equal(2, productos.Count(p => !p.Activo));
            Assert.Equal(new[] { "KG", "MIL", "PZA" }, productos.Select(p => p.UnidadBase).Distinct().Order().ToArray());
            Assert.Contains(productos, p => p.LlevaLote);
            Assert.Contains(productos, p => !p.LlevaLote);
            Assert.Contains(productos, p => p.Clasificacion?.Codigo == "MP");
            Assert.Contains(productos, p => p.Clasificacion?.Codigo == "PT");
            Assert.Equal(productos.Count, productos.Select(p => p.IdErp).Distinct().Count());
            Assert.All(productos, p => Assert.True(p.IdErp > 0));
        }

        [Fact]
        public void La_semilla_trae_cuatro_agentes_y_los_almacenes_de_las_dos_plantas()
        {
            var catalogo = Semilla();
            Assert.Equal(4, catalogo.Agentes.Count);
            Assert.Contains(catalogo.Agentes, a => a.Tipo == "venta");
            Assert.Contains(catalogo.Agentes, a => a.Tipo == "cobro");
            Assert.Contains(catalogo.Almacenes, a => a.Codigo.EndsWith("-PIM", StringComparison.Ordinal));
            Assert.Contains(catalogo.Almacenes, a => a.Codigo.EndsWith("-SC", StringComparison.Ordinal));
        }

        [Fact]
        public async Task La_paginacion_por_id_recorre_todo_sin_repetir()
        {
            var repo = Repositorio(Semilla());
            var vistos = new List<string>();
            string? cursor = null;
            do
            {
                var pagina = await repo.ProductosAsync(null, null, 7, cursor);
                vistos.AddRange(pagina.Items.Select(p => p.Codigo));
                cursor = pagina.NextCursor;
            }
            while (cursor is not null);
            Assert.Equal(20, vistos.Count);
            Assert.Equal(20, vistos.Distinct().Count());

            var agentes = await repo.AgentesAsync(3, null);
            Assert.Equal(3, agentes.Items.Count);
            Assert.NotNull(agentes.NextCursor);
            Assert.Single((await repo.AgentesAsync(3, agentes.NextCursor)).Items);
        }

        [Fact]
        public async Task Modified_since_en_productos_y_clientes_no_esta_disponible()
        {
            var repo = Repositorio(Semilla());
            await Assert.ThrowsAsync<LecturaNoDisponibleException>(() => repo.ProductosAsync(null, DateTimeOffset.UtcNow, 10, null));
            await Assert.ThrowsAsync<LecturaNoDisponibleException>(() => repo.ClientesAsync(null, DateTimeOffset.UtcNow, 10, null));
        }

        [Fact]
        public async Task Un_producto_se_cambia_se_agrega_y_se_quita_en_caliente()
        {
            var catalogo = Semilla();
            var repo = Repositorio(catalogo);
            var controlador = Controlador(catalogo);

            // Cambiar: lo inactiva y conserva su id_erp.
            var antes = (await repo.ProductoAsync("PEBD-001"))!;
            Assert.IsType<OkObjectResult>(controlador.GuardarProducto("PEBD-001",
                new ProductoContrato { Nombre = antes.Nombre, UnidadBase = "KG", Activo = false }));
            var despues = (await repo.ProductoAsync("PEBD-001"))!;
            Assert.False(despues.Activo);
            Assert.Equal(antes.IdErp, despues.IdErp);

            // Agregar: toma el siguiente id.
            var maximo = catalogo.Productos.Max(p => p.IdErp);
            Assert.IsType<OkObjectResult>(controlador.GuardarProducto("NUEVO-1",
                new ProductoContrato { Nombre = "Nuevo", UnidadBase = "PZA", LlevaLote = true }));
            Assert.Equal(maximo + 1, (await repo.ProductoAsync("NUEVO-1"))!.IdErp);
            Assert.Equal(21, catalogo.Productos.Count);

            // Quitar.
            Assert.IsType<NoContentResult>(controlador.QuitarProducto("NUEVO-1"));
            Assert.Null(await repo.ProductoAsync("NUEVO-1"));
            Assert.IsType<NotFoundResult>(controlador.QuitarProducto("NUEVO-1"));
        }

        [Fact]
        public async Task Un_cliente_cambia_de_moneda_y_de_domicilios_y_se_valida_el_fiscal()
        {
            var catalogo = Semilla();
            var repo = Repositorio(catalogo);
            var controlador = Controlador(catalogo);

            var previo = (await repo.ClienteAsync("CLI-002"))!;
            var envio = new DomicilioContrato { IdErp = 950, Tipo = "envio", Calle = "Calle 1" };
            var fiscal = new DomicilioContrato { IdErp = 951, Tipo = "fiscal", Calle = "Calle 2" };
            Assert.IsType<OkObjectResult>(controlador.GuardarCliente("CLI-002", new ClienteContrato
            {
                RazonSocial = previo.RazonSocial,
                Moneda = "USD",
                Domicilios = [fiscal, envio],
            }));
            var cambiado = (await repo.ClienteAsync("CLI-002"))!;
            Assert.Equal("USD", cambiado.Moneda);
            Assert.Equal(2, cambiado.Domicilios.Count);
            Assert.Equal(previo.IdErp, cambiado.IdErp);

            // Sin domicilio fiscal, o con dos, se rechaza.
            Assert.IsType<BadRequestObjectResult>(controlador.GuardarCliente("CLI-002",
                new ClienteContrato { RazonSocial = "X", Domicilios = [envio] }));
            Assert.IsType<BadRequestObjectResult>(controlador.GuardarCliente("CLI-002",
                new ClienteContrato { RazonSocial = "X", Domicilios = [fiscal, fiscal] }));

            Assert.IsType<NoContentResult>(controlador.QuitarCliente("CLI-002"));
            Assert.Null(await repo.ClienteAsync("CLI-002"));
        }

        [Fact]
        public void En_modo_real_las_rutas_de_operacion_del_catalogo_responden_404()
        {
            var controlador = Controlador(null);
            Assert.IsType<NotFoundResult>(controlador.GuardarProducto("X", new ProductoContrato { Nombre = "n", UnidadBase = "KG" }));
            Assert.IsType<NotFoundResult>(controlador.GuardarCliente("X", new ClienteContrato { RazonSocial = "n" }));
            Assert.IsType<NotFoundResult>(controlador.QuitarProducto("X"));
            Assert.IsType<NotFoundResult>(controlador.QuitarCliente("X"));
        }
    }
}
