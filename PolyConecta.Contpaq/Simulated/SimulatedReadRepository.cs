using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Infrastructure.Persistence;

namespace Contpaq.Bridge.Simulated
{
    /// <summary>Lecturas del modo simulado: el catálogo semilla más los almacenes dados de alta (D-122).</summary>
    public sealed class SimulatedReadRepository(SimulatedCatalog catalogo, SimulatedStore store) : IReadRepository
    {
        public Task<Pagina<ProductoContrato>> ProductosAsync(string? search, DateTimeOffset? modifiedSince, int limit, string? cursor) =>
            Task.FromResult(Paginar(
                catalogo.Productos.Where(p => Coincide(search, p.Codigo, p.Nombre)), p => p.Codigo, limit, cursor));

        public Task<ProductoContrato?> ProductoAsync(string codigo) =>
            Task.FromResult(catalogo.Productos.FirstOrDefault(p => p.Codigo == codigo));

        public Task<Pagina<ClienteContrato>> ClientesAsync(string? search, DateTimeOffset? modifiedSince, int limit, string? cursor) =>
            Task.FromResult(Paginar(
                catalogo.Clientes.Where(c => Coincide(search, c.Codigo, c.RazonSocial)), c => c.Codigo, limit, cursor));

        public Task<ClienteContrato?> ClienteAsync(string codigo) =>
            Task.FromResult(catalogo.Clientes.FirstOrDefault(c => c.Codigo == codigo));

        public Task<IReadOnlyList<AlmacenContrato>> AlmacenesAsync() =>
            Task.FromResult<IReadOnlyList<AlmacenContrato>>(
                catalogo.Almacenes.Concat(store.AlmacenesCreados()).OrderBy(a => a.Codigo, StringComparer.Ordinal).ToList());

        public Task<AlmacenContrato?> AlmacenAsync(string codigo) =>
            Task.FromResult(catalogo.Almacenes.FirstOrDefault(a => a.Codigo == codigo) ?? store.AlmacenCreado(codigo));

        public Task<IReadOnlyList<ExistenciaContrato>> ExistenciasAsync(IReadOnlyCollection<string> productos, string? almacen)
        {
            var unidades = catalogo.Productos.ToDictionary(p => p.Codigo, p => p.UnidadBase);
            IReadOnlyList<ExistenciaContrato> lista = catalogo.Existencias
                .Where(e => productos.Contains(e.Producto) && (almacen is null || e.Almacen == almacen))
                .Select(e => new ExistenciaContrato
                {
                    Producto = e.Producto,
                    Almacen = e.Almacen,
                    Unidad = unidades.GetValueOrDefault(e.Producto, string.Empty),
                    Cantidad = e.Cantidad,
                    Lote = e.Lote,
                })
                .ToList();
            return Task.FromResult(lista);
        }

        public Task<Pagina<RecepcionCompraContrato>> RecepcionesCompraAsync(DateTimeOffset? modifiedSince, int limit, string? cursor) =>
            Task.FromResult(Paginar(catalogo.RecepcionesCompra.AsEnumerable(), r => r.Folio, limit, cursor));

        private static bool Coincide(string? search, params string[] campos) =>
            string.IsNullOrWhiteSpace(search) || campos.Any(c => c.Contains(search, StringComparison.OrdinalIgnoreCase));

        /// <summary>El cursor es la última clave de la página anterior; las claves van en orden ordinal.</summary>
        private static Pagina<T> Paginar<T>(IEnumerable<T> items, Func<T, string> clave, int limit, string? cursor)
        {
            var ordenados = items.OrderBy(clave, StringComparer.Ordinal)
                .Where(i => cursor is null || string.CompareOrdinal(clave(i), cursor) > 0)
                .Take(limit + 1)
                .ToList();
            var hayMas = ordenados.Count > limit;
            var pagina = ordenados.Take(limit).ToList();
            return new Pagina<T> { Items = pagina, NextCursor = hayMas ? clave(pagina[^1]) : null };
        }
    }
}
