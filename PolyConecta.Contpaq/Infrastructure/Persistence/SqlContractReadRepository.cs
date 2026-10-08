using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Configuration;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Core.Services;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Contpaq.Bridge.Infrastructure.Persistence
{
    /// <summary>
    /// Lecturas del contrato (1.1) contra las tablas adm* de CONTPAQi, con el login de solo lectura
    /// (CT-30). El SQL está en <see cref="SqlLecturas"/> y sus columnas, en
    /// docs/contpaq/Referencia_BD_CONTPAQi.md (Principio VII).
    ///
    /// Productos, clientes y agentes se leen completos y paginados por id (el cursor es el último id);
    /// modified_since responde 501 porque CTIMESTAMP no es una fecha de modificación (D-150).
    /// Pendiente de F3: recepciones de compra (D-102), que responden 501.
    /// </summary>
    /// <param name="connectionString">Cadena del login de solo lectura.</param>
    /// <param name="conceptos">Para traducir CIDMONEDA a ISO con BridgeConfig__Monedas__{ISO}; sin ella, la moneda va null.</param>
    /// <param name="clasificacionProductos">Número (1 a 6) de CIDVALORCLASIFICACION{n} de "TIPO DE PRODUCTOS" (BridgeConfig__Clasificacion__Productos); sin él, la clasificación va null.</param>
    public sealed class SqlContractReadRepository(string connectionString, ConfiguracionConceptos? conceptos = null, int? clasificacionProductos = null) : IReadRepository
    {
        /// <summary>Productos por consulta de existencias (L1-T007): el IN de SQL Server se parametriza en lotes de este tamaño.</summary>
        public const int ProductosPorConsulta = 100;

        private readonly string _productos = SqlLecturas.Productos(Clasificacion(clasificacionProductos));

        private static int? Clasificacion(int? n) =>
            n is null or (>= 1 and <= 6)
                ? n
                : throw new InvalidOperationException($"BridgeConfig:Clasificacion:Productos debe ser un número de 1 a 6, no {n}.");

        private sealed record FilaProducto(long IdErp, string Codigo, string Nombre, string UnidadBase, bool LlevaLote, bool Activo, string? ClasificacionCodigo, string? ClasificacionNombre)
        {
            public ProductoContrato Contrato() => new()
            {
                IdErp = IdErp, Codigo = Codigo, Nombre = Nombre, UnidadBase = UnidadBase, LlevaLote = LlevaLote, Activo = Activo,
                Clasificacion = string.IsNullOrEmpty(ClasificacionCodigo) ? null : new ClasificacionContrato { Codigo = ClasificacionCodigo, Nombre = ClasificacionNombre ?? string.Empty },
            };
        }

        private sealed record FilaCliente(long IdErp, string Codigo, string RazonSocial, string? Rfc, bool Activo, int? MonedaId);

        private sealed record FilaDomicilio(long ClienteId, long IdErp, int TipoDireccion, string Calle, string NumeroExterior, string NumeroInterior,
            string Colonia, string CodigoPostal, string Ciudad, string Municipio, string Estado, string Pais, string Sucursal);

        private sealed record FilaAgente(long IdErp, string Codigo, string Nombre, int TipoAgente)
        {
            public AgenteContrato Contrato() => new()
            {
                IdErp = IdErp, Codigo = Codigo, Nombre = Nombre,
                Tipo = TipoAgente switch { 2 => "venta_cobro", 3 => "cobro", _ => "venta" },
            };
        }

        public async Task<Pagina<ProductoContrato>> ProductosAsync(string? search, DateTimeOffset? modifiedSince, int limit, string? cursor)
        {
            if (modifiedSince is not null) throw new LecturaNoDisponibleException(SinModifiedSince);
            var sql = $@"SELECT TOP (@Take) * FROM ({_productos}) x
                WHERE (@Search IS NULL OR x.Codigo LIKE '%' + @Search + '%' OR x.Nombre LIKE '%' + @Search + '%')
                  AND (@Cursor IS NULL OR x.IdErp > @Cursor)
                ORDER BY x.IdErp;";
            var filas = await ConsultarAsync<FilaProducto>(sql, new { Take = limit + 1, Search = search, Cursor = Cursor(cursor) });
            return Paginar(filas.Select(f => f.Contrato()).ToList(), p => p.IdErp, limit);
        }

        public async Task<ProductoContrato?> ProductoAsync(string codigo) =>
            (await ConsultarAsync<FilaProducto>($"SELECT * FROM ({_productos}) x WHERE x.Codigo = @codigo;", new { codigo }))
                .Select(f => f.Contrato()).FirstOrDefault();

        public async Task<Pagina<ClienteContrato>> ClientesAsync(string? search, DateTimeOffset? modifiedSince, int limit, string? cursor)
        {
            if (modifiedSince is not null) throw new LecturaNoDisponibleException(SinModifiedSince);
            var sql = $@"SELECT TOP (@Take) * FROM ({SqlLecturas.Clientes}) x
                WHERE (@Search IS NULL OR x.Codigo LIKE '%' + @Search + '%' OR x.RazonSocial LIKE '%' + @Search + '%')
                  AND (@Cursor IS NULL OR x.IdErp > @Cursor)
                ORDER BY x.IdErp;";
            var clientes = await ClientesConDomiciliosAsync(sql, new { Take = limit + 1, Search = search, Cursor = Cursor(cursor) });
            return Paginar(clientes, c => c.IdErp, limit);
        }

        public async Task<ClienteContrato?> ClienteAsync(string codigo) =>
            (await ClientesConDomiciliosAsync($"SELECT * FROM ({SqlLecturas.Clientes}) x WHERE x.Codigo = @codigo;", new { codigo })).FirstOrDefault();

        /// <summary>Lee los clientes y, en una sola consulta más, los domicilios de todos los de la página.</summary>
        private async Task<IReadOnlyList<ClienteContrato>> ClientesConDomiciliosAsync(string sql, object parametros)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await using var conn = await AbrirAsync();
            var filas = (await conn.QueryAsync<FilaCliente>(sql, parametros)).ToList();
            var ids = filas.Select(f => f.IdErp).ToArray();
            var domicilios = ids.Length == 0
                ? new List<FilaDomicilio>()
                : (await conn.QueryAsync<FilaDomicilio>(SqlLecturas.Domicilios, new { Ids = ids })).ToList();
            PerformanceMetrics.RecordReadQuery(sw.ElapsedMilliseconds);

            var porCliente = domicilios.ToLookup(d => d.ClienteId);
            return filas.Select(f => new ClienteContrato
            {
                IdErp = f.IdErp, Codigo = f.Codigo, RazonSocial = f.RazonSocial, Rfc = f.Rfc, Activo = f.Activo,
                Moneda = f.MonedaId is { } id ? conceptos?.CodigoIso(id) : null,
                Domicilios = porCliente[f.IdErp].Select(d => new DomicilioContrato
                {
                    IdErp = d.IdErp, Tipo = d.TipoDireccion == 0 ? "fiscal" : "envio",
                    Calle = d.Calle, NumeroExterior = d.NumeroExterior, NumeroInterior = d.NumeroInterior, Colonia = d.Colonia,
                    CodigoPostal = d.CodigoPostal, Ciudad = d.Ciudad, Municipio = d.Municipio, Estado = d.Estado, Pais = d.Pais, Sucursal = d.Sucursal,
                }).ToList(),
            }).ToList();
        }

        public async Task<Pagina<AgenteContrato>> AgentesAsync(int limit, string? cursor)
        {
            var sql = $@"SELECT TOP (@Take) * FROM ({SqlLecturas.Agentes}) x
                WHERE (@Cursor IS NULL OR x.IdErp > @Cursor)
                ORDER BY x.IdErp;";
            var filas = await ConsultarAsync<FilaAgente>(sql, new { Take = limit + 1, Cursor = Cursor(cursor) });
            return Paginar(filas.Select(f => f.Contrato()).ToList(), a => a.IdErp, limit);
        }

        public async Task<AgenteContrato?> AgenteAsync(string codigo) =>
            (await ConsultarAsync<FilaAgente>($"SELECT * FROM ({SqlLecturas.Agentes}) x WHERE x.Codigo = @codigo;", new { codigo }))
                .Select(f => f.Contrato()).FirstOrDefault();

        public async Task<IReadOnlyList<AlmacenContrato>> AlmacenesAsync() =>
            await ConsultarAsync<AlmacenContrato>($"{SqlLecturas.Almacenes} ORDER BY CCODIGOALMACEN;", null);

        public async Task<AlmacenContrato?> AlmacenAsync(string codigo) =>
            (await ConsultarAsync<AlmacenContrato>($"{SqlLecturas.Almacenes} WHERE CCODIGOALMACEN = @codigo;", new { codigo })).FirstOrDefault();

        /// <summary>
        /// Existencias de varios productos, en la unidad base: por lote para los que llevan lote (F-02) y
        /// por producto y almacén, con lote null, para los demás (F-01). Los productos van al SQL en
        /// lotes de <see cref="ProductosPorConsulta"/>.
        /// </summary>
        public async Task<IReadOnlyList<ExistenciaContrato>> ExistenciasAsync(IReadOnlyCollection<string> productos, string? almacen)
        {
            var resultado = new List<ExistenciaContrato>();
            if (!(await ConsultarAsync<long>(SqlLecturas.EjercicioVigente, null)).Any())
                throw new EjercicioVigenteException();
            foreach (var grupo in EnLotes(productos.Distinct(StringComparer.Ordinal), ProductosPorConsulta))
            {
                resultado.AddRange(await ConsultarAsync<ExistenciaContrato>(SqlLecturas.ExistenciasPorLote, new { Productos = grupo, Almacen = almacen }));
                resultado.AddRange(await ConsultarAsync<ExistenciaContrato>(SqlLecturas.ExistenciasPorProducto, new { Productos = grupo, Almacen = almacen }));
            }
            return resultado;
        }

        /// <summary>Parte una secuencia en grupos de a lo más <paramref name="tamano"/>.</summary>
        public static IEnumerable<string[]> EnLotes(IEnumerable<string> codigos, int tamano) => codigos.Chunk(tamano);

        public Task<Pagina<RecepcionCompraContrato>> RecepcionesCompraAsync(DateTimeOffset? modifiedSince, int limit, string? cursor) =>
            throw new LecturaNoDisponibleException("Las recepciones de compra (D-102) llegan en F3.");

        private const string SinModifiedSince =
            "modified_since está obsoleto en productos y clientes (D-150): CTIMESTAMP no es una fecha de modificación.";

        /// <summary>El cursor es el id de la última fila de la página anterior; si no es un número, la petición es inválida.</summary>
        private static long? Cursor(string? cursor) =>
            cursor is null ? null
            : long.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id
            : throw new ArgumentException("cursor: Debe ser el next_cursor de la página anterior.", nameof(cursor));

        private async Task<SqlConnection> AbrirAsync()
        {
            var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;");
            return conn;
        }

        private async Task<IReadOnlyList<T>> ConsultarAsync<T>(string sql, object? parametros)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await using var conn = await AbrirAsync();
            var filas = (await conn.QueryAsync<T>(sql, parametros)).ToList();
            PerformanceMetrics.RecordReadQuery(sw.ElapsedMilliseconds);
            return filas;
        }

        private static Pagina<T> Paginar<T>(IReadOnlyList<T> filas, Func<T, long> id, int limit)
        {
            var pagina = filas.Take(limit).ToList();
            return new Pagina<T> { Items = pagina, NextCursor = filas.Count > limit ? id(pagina[^1]).ToString(CultureInfo.InvariantCulture) : null };
        }
    }
}
