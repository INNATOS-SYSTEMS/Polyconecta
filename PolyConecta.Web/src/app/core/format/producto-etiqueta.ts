import { ProductRef } from '../models/inventario';
import { nombreProducto } from './producto';

/**
 * "Clave - Nombre" de un producto (D-141) a partir de su clave (o de su nombre, si el dato solo trae ese)
 * y el catálogo. Las plantillas la usan como `producto(clave, nombre)` y las columnas de `pc-odoo-list`
 * en su `texto`. No es un pipe: el primer pipe de la app sube su código a la carga inicial. Si el
 * dato trae su nombre, se respeta (una línea puede llevar una descripción propia); si no, sale del catálogo.
 */
export function etiquetaProducto(catalogo: readonly ProductRef[], claveONombre: string | null | undefined, nombre?: string): string {
  if (nombre !== undefined) return nombreProducto(claveONombre ?? '', nombre);
  if (!claveONombre) return '';
  const c = claveONombre.toLowerCase();
  const p = catalogo.find(x => x.clave.toLowerCase() === c) ?? catalogo.find(x => x.nombre === claveONombre);
  return p ? nombreProducto(p.clave, p.nombre) : claveONombre;
}
