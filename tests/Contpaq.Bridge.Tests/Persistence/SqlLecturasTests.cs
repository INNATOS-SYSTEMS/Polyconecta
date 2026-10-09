using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Configuration;
using Contpaq.Bridge.Infrastructure.Persistence;
using Contpaq.Bridge.Simulated;
using Contpaq.Bridge.Tests.Soporte;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Contpaq.Bridge.Tests.Persistence
{
    /// <summary>
    /// Las lecturas reales de 1.1 (L1-T006 y L1-T007) sin SQL Server: el SQL se revisa contra la
    /// referencia de la base (Principio VII) y la lógica de C# se prueba con lo que no necesita conexión.
    /// La corrida contra CONTPAQi es de L1-T008, en el VPS.
    /// </summary>
    public partial class SqlLecturasTests
    {
        private static readonly string Referencia = File.ReadAllText(Path.Combine(Entorno.Raiz, "docs", "contpaq", "Referencia_BD_CONTPAQi.md"));

        // La referencia parte algunos nombres largos en dos renglones: se compara sin espacios.
        private static readonly string ReferenciaSinEspacios = Regex.Replace(Referencia, @"\s+", "");

        [GeneratedRegex(@"\bC[A-Z0-9]{3,}\b")]
        private static partial Regex Columna();

        [GeneratedRegex(@"\badm[A-Za-z]+\b")]
        private static partial Regex Tabla();

        private static IEnumerable<string> TodoElSql()
        {
            yield return SqlLecturas.Productos(null);
            for (var n = 1; n <= 6; n++) yield return SqlLecturas.Productos(n);
            yield return SqlLecturas.Clientes;
            yield return SqlLecturas.Domicilios;
            yield return SqlLecturas.Agentes;
            yield return SqlLecturas.Almacenes;
            yield return SqlLecturas.ExistenciasPorLote;
            yield return SqlLecturas.ExistenciasPorProducto;
        }

        [Fact]
        public void Toda_tabla_y_columna_del_sql_esta_en_la_referencia_de_la_base()
        {
            var noSonColumnas = new HashSet<string> { "CAST", "CASE" };
            var faltantes = new SortedSet<string>();
            foreach (var sql in TodoElSql())
            {
                foreach (Match m in Tabla().Matches(sql))
                    if (!Referencia.Contains($"### `{m.Value}`", StringComparison.Ordinal)) faltantes.Add("tabla " + m.Value);
                foreach (Match m in Columna().Matches(sql))
                    if (!noSonColumnas.Contains(m.Value) && !ReferenciaSinEspacios.Contains($"`{m.Value}`", StringComparison.Ordinal))
                        faltantes.Add("columna " + m.Value);
            }
            Assert.Empty(faltantes);
        }

        [Fact]
        public void Las_lecturas_solo_leen_nada_escribe()
        {
            foreach (var sql in TodoElSql())
                Assert.DoesNotMatch(@"\b(INSERT|UPDATE|DELETE|MERGE|DROP|ALTER|EXEC)\b", sql);
        }

        [Fact]
        public void Las_existencias_separan_lotes_de_producto_y_almacen()
        {
            // Los que llevan lote (bit 16 de CCONTROLEXISTENCIA) salen de las capas; los demás, de admExistenciaCosto.
            Assert.Contains("(p.CCONTROLEXISTENCIA & 16) <> 0", SqlLecturas.ExistenciasPorLote);
            Assert.Contains("(p.CCONTROLEXISTENCIA & 16) = 0", SqlLecturas.ExistenciasPorProducto);
            Assert.Contains("admCapasProducto", SqlLecturas.ExistenciasPorLote);
            Assert.Contains("CENTRADASPERIODO12 - e.CSALIDASPERIODO12", SqlLecturas.ExistenciasPorProducto);
            // Los productos van parametrizados, nunca concatenados.
            Assert.Contains("IN @Productos", SqlLecturas.ExistenciasPorLote);
            Assert.Contains("IN @Productos", SqlLecturas.ExistenciasPorProducto);
        }

        [Fact]
        public void El_ejercicio_de_F01_es_solo_el_que_contiene_hoy_sin_respaldo()
        {
            Assert.Contains("BETWEEN x.CFECINIPERIODO1 AND x.CFECHAFINAL", SqlLecturas.ExistenciasPorProducto);
            Assert.DoesNotContain("ISNULL(", SqlLecturas.EjercicioVigente);
            Assert.Single(Regex.Matches(SqlLecturas.ExistenciasPorProducto, "admEjercicios"));
        }

        [Fact]
        public async Task Sin_ejercicio_vigente_la_lectura_de_existencias_falla_con_SDK_ERROR_y_su_motivo()
        {
            var repo = new Moq.Mock<IReadRepository>();
            repo.Setup(r => r.ExistenciasAsync(Moq.It.IsAny<IReadOnlyCollection<string>>(), Moq.It.IsAny<string?>()))
                .Returns(Task.FromException<IReadOnlyList<Contpaq.Bridge.Core.Contract.ExistenciaContrato>>(new EjercicioVigenteException()));
            var controlador = new Contpaq.Bridge.Api.Controllers.LecturasController(repo.Object);

            var r = Assert.IsType<Microsoft.AspNetCore.Mvc.ObjectResult>(await controlador.Existencias("P1", null));

            Assert.Equal(500, r.StatusCode);
            var error = Assert.IsType<Contpaq.Bridge.Core.Contract.ErrorContrato>(r.Value);
            Assert.Equal("SDK_ERROR", error.Code);
            Assert.Equal("SIN_EJERCICIO_VIGENTE", error.Detail["motivo"]);
        }

        [Fact]
        public void La_clasificacion_se_une_por_el_numero_configurado()
        {
            Assert.DoesNotContain("admClasificacionesValores", SqlLecturas.Productos(null));
            Assert.Contains("p.CIDVALORCLASIFICACION3", SqlLecturas.Productos(3));
            Assert.Contains("admClasificacionesValores", SqlLecturas.Productos(3));
        }

        [Fact]
        public void Los_domicilios_son_los_de_clientes_en_una_sola_consulta_por_pagina()
        {
            Assert.Contains("CTIPOCATALOGO = 1", SqlLecturas.Domicilios);
            Assert.Contains("CIDCATALOGO IN @Ids", SqlLecturas.Domicilios);
        }

        [Fact]
        public void Un_numero_de_clasificacion_fuera_de_rango_se_rechaza_al_arrancar()
        {
            Assert.Throws<InvalidOperationException>(() => new SqlContractReadRepository("Server=x", null, 7));
            Assert.Throws<InvalidOperationException>(() => new SqlContractReadRepository("Server=x", null, 0));
            _ = new SqlContractReadRepository("Server=x", null, 6);
            _ = new SqlContractReadRepository("Server=x", null, null);
        }

        [Fact]
        public void Los_productos_se_parten_en_lotes_de_cien()
        {
            var codigos = Enumerable.Range(1, 250).Select(i => $"P{i}").ToList();
            var lotes = SqlContractReadRepository.EnLotes(codigos, SqlContractReadRepository.ProductosPorConsulta).ToList();
            Assert.Equal([100, 100, 50], lotes.Select(l => l.Length).ToArray());
            Assert.Equal(codigos, lotes.SelectMany(l => l).ToList());
        }

        [Fact]
        public void La_moneda_se_traduce_de_id_a_iso_con_la_misma_configuracion_del_alta_de_pedido()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BridgeConfig:Monedas:MXN"] = "1",
                ["BridgeConfig:Monedas:USD"] = "2",
            }).Build();
            var conceptos = new ConfiguracionConceptos(config);
            Assert.Equal("USD", conceptos.CodigoIso(2));
            Assert.Equal(2, conceptos.IdMoneda("USD"));
            Assert.Null(conceptos.CodigoIso(9));
        }

        [Fact]
        public async Task Las_existencias_de_muchos_productos_en_una_consulta_responden_solo_los_pedidos()
        {
            // Sobre el repositorio simulado (L1-T007): 250 códigos en una consulta, de los que solo 3 existen.
            var catalogo = SimulatedCatalog.Cargar(Path.Combine(Entorno.Raiz, "PolyConecta.Contpaq", "Simulated", "seed.json"));
            var store = new SimulatedStore($"Data Source={Path.Combine(Path.GetTempPath(), $"bridge_{Guid.NewGuid():N}.db")}");
            store.Inicializar();
            var repo = new SimulatedReadRepository(catalogo, store);

            var pedido = Enumerable.Range(1, 247).Select(i => $"NO-{i}").Concat(["R-IV310", "PEBD-001", "PT1113 C567"]).ToList();
            var existencias = await repo.ExistenciasAsync(pedido, null);

            Assert.Equal(["PEBD-001", "PT1113 C567", "R-IV310"], existencias.Select(e => e.Producto).Distinct().Order().ToArray());
            Assert.All(existencias, e => Assert.Equal(catalogo.Productos.First(p => p.Codigo == e.Producto).UnidadBase, e.Unidad));
            Assert.Equal(2, existencias.Count(e => e.Producto == "R-IV310" && e.Lote is not null));
            Assert.Contains(existencias, e => e.Producto == "PEBD-001" && e.Lote is null);
        }

        [Fact]
        public async Task Un_cursor_que_no_es_numero_se_rechaza_como_peticion_invalida()
        {
            var catalogo = SimulatedCatalog.Cargar(Path.Combine(Entorno.Raiz, "PolyConecta.Contpaq", "Simulated", "seed.json"));
            var store = new SimulatedStore($"Data Source={Path.Combine(Path.GetTempPath(), $"bridge_{Guid.NewGuid():N}.db")}");
            store.Inicializar();
            var repo = new SimulatedReadRepository(catalogo, store);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.ProductosAsync(null, null, 10, "abc"));
            Assert.Equal("cursor", ex.ParamName);
        }
    }
}
