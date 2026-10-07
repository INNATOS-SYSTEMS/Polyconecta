using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Configuration;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Infrastructure.Persistence;

namespace Contpaq.Bridge.Core.Validation
{
    /// <summary>
    /// Validar antes de escribir (CT-39, D-81, D-112): producto existe y está activo, la unidad es la
    /// base del producto (D-127), almacenes y cliente existen, Σ lotes = cantidad y hay existencia
    /// por lote en el origen. Corre igual en modo real y simulado, por encima del gateway (D-122).
    /// </summary>
    public sealed class ValidadorComandos(IReadRepository lecturas, ConfiguracionConceptos conceptos)
    {
        public async Task<ErrorContrato?> ValidarAsync(ComandoLeido comando)
        {
            var sinConcepto = conceptos.RolesSinConcepto(comando.CommandType, comando.Variant);
            if (sinConcepto.Count > 0)
                return ErrorContrato.De(CodigosError.VarianteSinConcepto,
                    $"No hay concepto configurado para {comando.CommandType} {comando.Variant} ({string.Join(", ", sinConcepto)}).",
                    new() { ["command_type"] = comando.CommandType, ["variant"] = comando.Variant, ["roles"] = sinConcepto });

            return comando.Carga switch
            {
                CargaTraspaso t => await Primero(
                    () => Almacen(t.AlmacenOrigen), () => Almacen(t.AlmacenDestino),
                    () => Lineas(t.Lineas!), () => Existencia(t.Lineas!, t.AlmacenOrigen)),
                CargaAltaPedido p => await Primero(
                    () => Cliente(p.Cliente), () => Task.FromResult(Moneda(p.Moneda)), () => LineasPedido(p.Lineas!)),
                CargaAltaAlmacen => null,
                CargaCierreProduccion c => await Primero(
                    () => Almacen(c.AlmacenWip),
                    () => Lineas(c.Consumos!), () => Lineas(c.Entradas!), () => Lineas(c.Subproductos!),
                    () => Destinos(c.Entradas!.Concat(c.Subproductos!)),
                    () => Existencia(c.Consumos!, c.AlmacenWip)),
                CargaRemision m => await Primero(
                    () => Cliente(m.Cliente), () => Almacen(m.Almacen),
                    () => Lineas(m.Lineas!), () => Existencia(m.Lineas!, m.Almacen)),
                _ => ErrorContrato.De(CodigosError.CargaInvalida, "Carga desconocida."),
            };
        }

        private static async Task<ErrorContrato?> Primero(params System.Func<Task<ErrorContrato?>>[] reglas)
        {
            foreach (var regla in reglas)
            {
                var error = await regla();
                if (error is not null) return error;
            }
            return null;
        }

        private async Task<ErrorContrato?> Almacen(string codigo) =>
            await lecturas.AlmacenAsync(codigo) is null
                ? ErrorContrato.De(CodigosError.AlmacenNoExiste, $"El almacén {codigo} no existe en CONTPAQi.", new() { ["almacen"] = codigo })
                : null;

        private async Task<ErrorContrato?> Destinos(IEnumerable<LineaConDestino> lineas)
        {
            foreach (var destino in lineas.Select(l => l.AlmacenDestino).Distinct())
            {
                var error = await Almacen(destino);
                if (error is not null) return error;
            }
            return null;
        }

        private async Task<ErrorContrato?> Cliente(string codigo) =>
            await lecturas.ClienteAsync(codigo) is null
                ? ErrorContrato.De(CodigosError.ClienteNoExiste, $"El cliente {codigo} no existe en CONTPAQi.", new() { ["cliente"] = codigo })
                : null;

        private ErrorContrato? Moneda(string codigo) =>
            conceptos.IdMoneda(codigo) is null
                ? ErrorContrato.De(CodigosError.MonedaNoSoportada, $"La moneda {codigo} no está configurada en el bridge.", new() { ["moneda"] = codigo })
                : null;

        private async Task<ErrorContrato?> Producto(string codigo, string unidad)
        {
            var producto = await lecturas.ProductoAsync(codigo);
            if (producto is null)
                return ErrorContrato.De(CodigosError.ProductoNoExiste, $"El producto {codigo} no existe en CONTPAQi.", new() { ["producto"] = codigo });
            if (!producto.Activo)
                return ErrorContrato.De(CodigosError.ProductoInactivo, $"El producto {codigo} está dado de baja.", new() { ["producto"] = codigo });
            if (!string.Equals(producto.UnidadBase, unidad, System.StringComparison.OrdinalIgnoreCase))
                return ErrorContrato.De(CodigosError.UnidadNoAdmitida,
                    $"La unidad {unidad} no es la unidad base de {codigo} ({producto.UnidadBase}).",
                    new() { ["producto"] = codigo, ["unidad"] = unidad, ["unidad_base"] = producto.UnidadBase });
            return null;
        }

        private async Task<ErrorContrato?> Lineas(IEnumerable<LineaConLotes> lineas)
        {
            foreach (var linea in lineas)
            {
                var error = await Producto(linea.Producto, linea.Unidad);
                if (error is not null) return error;

                var producto = (await lecturas.ProductoAsync(linea.Producto))!;
                var lotes = linea.Lotes ?? new List<Lote>();
                if (!producto.LlevaLote && lotes.Count > 0)
                    return ErrorContrato.De(CodigosError.CargaInvalida, $"El producto {linea.Producto} no lleva lote.",
                        new() { ["campo"] = "lotes", ["producto"] = linea.Producto });
                if (producto.LlevaLote && lotes.Sum(l => l.Cantidad) != linea.Cantidad)
                    return ErrorContrato.De(CodigosError.LotesNoCuadran,
                        $"Los lotes de {linea.Producto} suman {lotes.Sum(l => l.Cantidad)} y la línea dice {linea.Cantidad}.",
                        new() { ["producto"] = linea.Producto, ["cantidad"] = linea.Cantidad, ["suma_lotes"] = lotes.Sum(l => l.Cantidad) });
            }
            return null;
        }

        private async Task<ErrorContrato?> LineasPedido(IEnumerable<LineaPedido> lineas)
        {
            foreach (var linea in lineas)
            {
                var error = await Producto(linea.Producto, linea.Unidad);
                if (error is not null) return error;
            }
            return null;
        }

        /// <summary>Existencia en el origen, sumando lo que pidan varias líneas del mismo producto y lote.</summary>
        private async Task<ErrorContrato?> Existencia(IEnumerable<LineaConLotes> lineas, string almacen)
        {
            var lista = lineas.ToList();
            var existencias = await lecturas.ExistenciasAsync(lista.Select(l => l.Producto).Distinct().ToList(), almacen);

            var porLote = lista
                .SelectMany(l => (l.Lotes ?? new List<Lote>()).Select(lote => (l.Producto, Lote: lote.Numero, lote.Cantidad, l.Unidad)))
                .GroupBy(x => (x.Producto, x.Lote));
            foreach (var g in porLote)
            {
                var disponible = existencias.Where(e => e.Producto == g.Key.Producto && e.Lote == g.Key.Lote).Sum(e => e.Cantidad);
                var solicitado = g.Sum(x => x.Cantidad);
                if (disponible < solicitado)
                    return ErrorContrato.De(CodigosError.ExistenciaInsuficienteLote,
                        $"El lote {g.Key.Lote} no tiene existencia suficiente en {almacen}.",
                        new() { ["producto"] = g.Key.Producto, ["almacen"] = almacen, ["lote"] = g.Key.Lote, ["unidad"] = g.First().Unidad, ["disponible"] = disponible, ["solicitado"] = solicitado });
            }

            var sinLote = lista.Where(l => (l.Lotes ?? new List<Lote>()).Count == 0).GroupBy(l => l.Producto);
            foreach (var g in sinLote)
            {
                var disponible = existencias.Where(e => e.Producto == g.Key && e.Lote is null).Sum(e => e.Cantidad);
                var solicitado = g.Sum(l => l.Cantidad);
                if (disponible < solicitado)
                    return ErrorContrato.De(CodigosError.ExistenciaInsuficiente,
                        $"El producto {g.Key} no tiene existencia suficiente en {almacen}.",
                        new() { ["producto"] = g.Key, ["almacen"] = almacen, ["unidad"] = g.First().Unidad, ["disponible"] = disponible, ["solicitado"] = solicitado });
            }
            return null;
        }
    }
}
