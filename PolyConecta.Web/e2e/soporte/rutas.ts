/** Las 19 rutas de FR-004, con folios de la semilla leídos del prototipo corriendo. */
export const RUTAS: readonly string[] = [
  '/',
  '/pedidos',
  '/pedidos/IV310-26',
  '/fabricacion',
  '/fabricacion/BOL-2026-0001',
  '/calidad',
  '/calidad/BOL-2026-0001',
  '/recolecciones',
  '/recolecciones/SC/OUT/48214',
  '/traslados',
  '/traslados/PIM/OUT/48213',
  '/recepcion',
  '/recepcion/SC/IN/50974',
  '/entregas',
  '/entregas/SC/OUT/31688',
  '/inventario',
  '/ventas/inventario',
  '/captura-masiva',
  '/incidencias',
];

/** `PARITY_ROUTES=/pedidos,/fabricacion npm run audit` limita las rutas (cada agente verifica las suyas). */
export function rutasSeleccionadas(): readonly string[] {
  const filtro = process.env['PARITY_ROUTES'];
  return filtro ? filtro.split(',').map(r => r.trim()).filter(r => r !== '') : RUTAS;
}

/**
 * Rutas con prefijo de módulo (D-155): la réplica vive bajo su módulo y el prototipo no. Los guiones y el
 * auditor siguen escritos con las rutas del prototipo; al abrir Angular se traducen, y la URL de Angular se
 * vuelve a la forma del prototipo para comparar.
 */
const PREFIJOS: readonly (readonly [string, string])[] = [
  ['/pedidos', '/ventas/pedidos'],
  ['/fabricacion', '/produccion/fabricacion'],
  ['/captura-masiva', '/produccion/captura-masiva'],
  ['/incidencias', '/produccion/incidencias'],
  ['/recolecciones', '/logistica/recolecciones'],
  ['/traslados', '/logistica/traslados'],
  ['/recepcion', '/logistica/recepcion'],
  ['/entregas', '/logistica/entregas'],
];

const empieza = (ruta: string, prefijo: string) =>
  ruta.startsWith(prefijo) && (ruta.length === prefijo.length || '/?#'.includes(ruta[prefijo.length]));

/** Ruta del prototipo → ruta de la réplica. Una ruta ya traducida no cambia. */
export function rutaAngular(ruta: string): string {
  const par = PREFIJOS.find(([viejo]) => empieza(ruta, viejo));
  return par ? par[1] + ruta.slice(par[0].length) : ruta;
}

/** Ruta de la réplica → ruta del prototipo, para comparar URLs entre las dos aplicaciones. */
export function rutaPrototipo(ruta: string): string {
  const par = PREFIJOS.find(([, nuevo]) => empieza(ruta, nuevo));
  return par ? par[0] + ruta.slice(par[1].length) : ruta;
}
