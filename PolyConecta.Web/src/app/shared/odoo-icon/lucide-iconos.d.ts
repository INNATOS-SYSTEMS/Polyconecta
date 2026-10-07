/**
 * Cada ícono de `lucide` vive en su propio módulo (`lucide/dist/esm/icons/<nombre>.mjs`) y exporta su
 * dibujo como lista de elementos SVG. El paquete no trae tipos para esa ruta; este es el suyo.
 */
declare module 'lucide/dist/esm/icons/*.mjs' {
  const icono: [string, Record<string, string | number>][];
  export default icono;
}
