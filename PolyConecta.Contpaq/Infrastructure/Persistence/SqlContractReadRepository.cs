using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Core.Services;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Contpaq.Bridge.Infrastructure.Persistence
{
    /// <summary>
    /// Lecturas del contrato contra las tablas adm* de CONTPAQi, con el login de solo lectura
    /// (CT-30). Las columnas están en docs/contpaq/Referencia_BD_CONTPAQi.md (Principio VII).
    ///
    /// Pendiente de la tarea 1.4 (F1, L1): `modified_since`, la existencia de productos sin lote
    /// (que no tienen capas) y su verificación contra el laboratorio. Pendiente de F3:
    /// recepciones de compra (D-102). Hasta entonces responden 501.
    /// </summary>
    /// <remarks>CONTPAQi guarda códigos y nombres como texto de ancho fijo: las lecturas quitan los espacios del final
    /// (RTRIM) para que PolyConecta compare códigos exactos. Las comparaciones con = de SQL Server ya los ignoran.</remarks>
    public sealed class SqlContractReadRepository(string connectionString) : IReadRepository
    {
        private const string Productos = @"
            SELECT RTRIM(p.CCODIGOPRODUCTO) AS Codigo,
                   RTRIM(p.CNOMBREPRODUCTO) AS Nombre,
                   RTRIM(ISNULL(u.CABREVIATURA, '')) AS UnidadBase,
                   CAST(CASE WHEN (p.CCONTROLEXISTENCIA & 16) <> 0 THEN 1 ELSE 0 END AS bit) AS LlevaLote,
                   CAST(CASE WHEN p.CSTATUSPRODUCTO = 1 THEN 1 ELSE 0 END AS bit) AS Activo
            FROM admProductos p WITH (NOLOCK)
            LEFT JOIN admUnidadesMedidaPeso u WITH (NOLOCK) ON u.CIDUNIDAD = p.CIDUNIDADBASE";

        public async Task<Pagina<ProductoContrato>> ProductosAsync(string? search, DateTimeOffset? modifiedSince, int limit, string? cursor)
        {
            if (modifiedSince is not null) throw new LecturaNoDisponibleException("modified_since llega con la tarea 1.4 (F1).");
            var sql = $@"SELECT TOP (@Take) * FROM ({Productos}) x
                WHERE (@Search IS NULL OR x.Codigo LIKE '%' + @Search + '%' OR x.Nombre LIKE '%' + @Search + '%')
                  AND (@Cursor IS NULL OR x.Codigo > @Cursor)
                ORDER BY x.Codigo;";
            var filas = await ConsultarAsync<ProductoContrato>(sql, new { Take = limit + 1, Search = search, Cursor = cursor });
            return Paginar(filas, p => p.Codigo, limit);
        }

        public async Task<ProductoContrato?> ProductoAsync(string codigo) =>
            (await ConsultarAsync<ProductoContrato>($"{Productos} WHERE p.CCODIGOPRODUCTO = @codigo;", new { codigo })).FirstOrDefault();

        private const string Clientes = @"
            SELECT RTRIM(CCODIGOCLIENTE) AS Codigo, RTRIM(CRAZONSOCIAL) AS RazonSocial, RTRIM(CRFC) AS Rfc
            FROM admClientes WITH (NOLOCK)";

        public async Task<Pagina<ClienteContrato>> ClientesAsync(string? search, DateTimeOffset? modifiedSince, int limit, string? cursor)
        {
            if (modifiedSince is not null) throw new LecturaNoDisponibleException("modified_since llega con la tarea 1.4 (F1).");
            var sql = $@"SELECT TOP (@Take) * FROM ({Clientes}) x
                WHERE (@Search IS NULL OR x.Codigo LIKE '%' + @Search + '%' OR x.RazonSocial LIKE '%' + @Search + '%')
                  AND (@Cursor IS NULL OR x.Codigo > @Cursor)
                ORDER BY x.Codigo;";
            var filas = await ConsultarAsync<ClienteContrato>(sql, new { Take = limit + 1, Search = search, Cursor = cursor });
            return Paginar(filas, c => c.Codigo, limit);
        }

        public async Task<ClienteContrato?> ClienteAsync(string codigo) =>
            (await ConsultarAsync<ClienteContrato>($"{Clientes} WHERE CCODIGOCLIENTE = @codigo;", new { codigo })).FirstOrDefault();

        private const string Almacenes = @"
            SELECT RTRIM(CCODIGOALMACEN) AS Codigo, RTRIM(CNOMBREALMACEN) AS Nombre, CAST(CIDALMACEN AS bigint) AS IdErp
            FROM admAlmacenes WITH (NOLOCK)";

        public async Task<IReadOnlyList<AlmacenContrato>> AlmacenesAsync() =>
            await ConsultarAsync<AlmacenContrato>($"{Almacenes} ORDER BY CCODIGOALMACEN;", null);

        public async Task<AlmacenContrato?> AlmacenAsync(string codigo) =>
            (await ConsultarAsync<AlmacenContrato>($"{Almacenes} WHERE CCODIGOALMACEN = @codigo;", new { codigo })).FirstOrDefault();

        /// <summary>Solo capas (productos con lote, pedimento o PEPS). Los productos sin capas llegan con la tarea 1.4.</summary>
        public async Task<IReadOnlyList<ExistenciaContrato>> ExistenciasAsync(IReadOnlyCollection<string> productos, string? almacen)
        {
            const string sql = @"
                SELECT RTRIM(p.CCODIGOPRODUCTO) AS Producto, RTRIM(a.CCODIGOALMACEN) AS Almacen, RTRIM(ISNULL(u.CABREVIATURA, '')) AS Unidad,
                       CAST(cp.CEXISTENCIA AS decimal(18,4)) AS Cantidad, NULLIF(RTRIM(cp.CNUMEROLOTE), '') AS Lote
                FROM admCapasProducto cp WITH (NOLOCK)
                JOIN admProductos p WITH (NOLOCK) ON cp.CIDPRODUCTO = p.CIDPRODUCTO
                JOIN admAlmacenes a WITH (NOLOCK) ON cp.CIDALMACEN = a.CIDALMACEN
                LEFT JOIN admUnidadesMedidaPeso u WITH (NOLOCK) ON u.CIDUNIDAD = p.CIDUNIDADBASE
                WHERE p.CCODIGOPRODUCTO IN @Productos
                  AND (@Almacen IS NULL OR a.CCODIGOALMACEN = @Almacen)
                  AND cp.CEXISTENCIA > 0;";
            return await ConsultarAsync<ExistenciaContrato>(sql, new { Productos = productos, Almacen = almacen });
        }

        public Task<Pagina<RecepcionCompraContrato>> RecepcionesCompraAsync(DateTimeOffset? modifiedSince, int limit, string? cursor) =>
            throw new LecturaNoDisponibleException("Las recepciones de compra (D-102) llegan en F3.");

        private async Task<IReadOnlyList<T>> ConsultarAsync<T>(string sql, object? parametros)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;");
            var filas = (await conn.QueryAsync<T>(sql, parametros)).ToList();
            PerformanceMetrics.RecordReadQuery(sw.ElapsedMilliseconds);
            return filas;
        }

        private static Pagina<T> Paginar<T>(IReadOnlyList<T> filas, Func<T, string> clave, int limit)
        {
            var pagina = filas.Take(limit).ToList();
            return new Pagina<T> { Items = pagina, NextCursor = filas.Count > limit ? clave(pagina[^1]) : null };
        }
    }
}
