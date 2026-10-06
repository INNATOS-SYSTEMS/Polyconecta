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

/** `npm run parity -- --routes=/pedidos,/fabricacion` limita la corrida (cada agente verifica las suyas). */
export function rutasSeleccionadas(): readonly string[] {
  const filtro = process.env['PARITY_ROUTES'];
  return filtro ? filtro.split(',').map(r => r.trim()).filter(r => r !== '') : RUTAS;
}
