/**
 * Nombre para mostrar de un producto (D-141): "Clave - Nombre", en todo lugar donde se ve un producto.
 * La clave sigue siendo el identificador: los datos y las búsquedas la usan tal cual.
 */
export const nombreProducto = (clave: string, nombre?: string): string => (clave && nombre ? `${clave} - ${nombre}` : clave || nombre || '');
