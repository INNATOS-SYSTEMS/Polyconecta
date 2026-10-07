import { ProductRef } from '../../models/inventario';
import { igualSinMayusculas } from '../estado-base';

/**
 * Línea capturada en modo libre (D-127): producto del catálogo, cantidad y la unidad base del
 * producto en CONTPAQi. La unidad no se captura: sale de `ProductRef.unidad` y no se convierte.
 */
export interface LineaLibre {
  clave: string;
  producto: string;
  cantidad: number;
  unidad: string;
}

/** Devuelve el motivo por el que la línea no se puede guardar, o undefined si es válida. */
export function validarLineaLibre(producto: ProductRef | undefined, cantidad: number, unidad: string): string | undefined {
  if (!producto) return 'Elija un producto del catálogo de CONTPAQi.';
  if (!Number.isFinite(cantidad) || cantidad <= 0) return 'La cantidad debe ser mayor que cero.';
  if (!igualSinMayusculas(unidad, producto.unidad)) return `La unidad de ${producto.clave} es ${producto.unidad}: no se captura otra ni se convierte.`;
  return undefined;
}

/** Arma la línea con la unidad del producto, o devuelve el error de validación. */
export function crearLineaLibre(producto: ProductRef | undefined, cantidad: number): { linea?: LineaLibre; error?: string } {
  const error = validarLineaLibre(producto, cantidad, producto?.unidad ?? '');
  if (error) return { error };
  return { linea: { clave: producto!.clave, producto: producto!.nombre, cantidad, unidad: producto!.unidad } };
}

/** Siguiente consecutivo de folios con forma `PREFIJO-2026-0001` dentro de una lista. */
export function siguienteFolio(prefijo: string, existentes: readonly string[]): string {
  const base = `${prefijo}-2026-`;
  const max = existentes.filter(f => f.startsWith(base)).reduce((m, f) => Math.max(m, Number.parseInt(f.slice(base.length), 10) || 0), 0);
  return base + String(max + 1).padStart(4, '0');
}
