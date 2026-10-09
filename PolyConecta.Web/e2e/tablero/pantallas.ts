/**
 * Pantallas del tablero de flujo (spec 011): una fila por flujo, en el orden en que el usuario las
 * recorre. `siguiente` dice qué acción lleva a la pantalla de la derecha.
 */
export interface Pantalla {
  ruta: string;
  titulo: string;
  tipo: 'Lista' | 'Formulario' | 'Nuevo' | 'Tablero' | 'Captura';
  siguiente?: string;
}

export interface Flujo {
  id: string;
  nombre: string;
  descripcion: string;
  pantallas: Pantalla[];
}

export const FLUJOS: readonly Flujo[] = [
  {
    id: 'inicio',
    nombre: 'Inicio e inventario',
    descripcion: 'Punto de entrada y consultas de existencias.',
    pantallas: [
      { ruta: '/', titulo: 'Inicio', tipo: 'Tablero', siguiente: 'Menú Inventario' },
      { ruta: '/inventario', titulo: 'Inventario actual', tipo: 'Lista', siguiente: 'Vista de Ventas' },
      { ruta: '/ventas/inventario', titulo: 'Inventario para Ventas', tipo: 'Lista' },
    ],
  },
  {
    id: 'pedidos',
    nombre: 'Pedidos de venta',
    descripcion: 'Captura, confirmación y autorización con dos firmas.',
    pantallas: [
      { ruta: '/ventas/pedidos', titulo: 'Pedidos', tipo: 'Lista', siguiente: 'Abrir un pedido' },
      // Desde F1 el pedido sale de la API y se abre por id (D-154): necesita al menos un pedido (scripts/dev/sembrar-pedidos.sh).
      { ruta: '/ventas/pedidos/1', titulo: 'Pedido', tipo: 'Formulario', siguiente: 'Autorizar → genera OF' },
      { ruta: '/ventas/pedidos/nuevo', titulo: 'Pedido nuevo', tipo: 'Nuevo' },
    ],
  },
  {
    id: 'fabricacion',
    nombre: 'Fabricación',
    descripcion: 'Cadena de órdenes: extrusión, impresión y bolseo; captura e incidencias.',
    pantallas: [
      { ruta: '/produccion/fabricacion', titulo: 'Órdenes de fabricación', tipo: 'Lista', siguiente: 'Abrir una OF' },
      { ruta: '/produccion/fabricacion/BOL-2026-0001', titulo: 'OF de bolseo (raíz)', tipo: 'Formulario', siguiente: 'OF hija: impresión' },
      { ruta: '/produccion/fabricacion/IMP-2026-0001', titulo: 'OF de impresión', tipo: 'Formulario', siguiente: 'OF hija: extrusión' },
      { ruta: '/produccion/fabricacion/EXT-2026-0001', titulo: 'OF de extrusión', tipo: 'Formulario', siguiente: 'Capturar producción' },
      { ruta: '/produccion/captura-masiva', titulo: 'Captura masiva', tipo: 'Captura', siguiente: 'Reportar incidencia' },
      { ruta: '/produccion/incidencias', titulo: 'Incidencias', tipo: 'Lista' },
      { ruta: '/produccion/fabricacion/nuevo', titulo: 'OF nueva', tipo: 'Nuevo' },
    ],
  },
  {
    id: 'recoleccion',
    nombre: 'Recolección de componentes',
    descripcion: 'Surtido de MP y componentes de almacén a WIP para la OF.',
    pantallas: [
      { ruta: '/logistica/recolecciones', titulo: 'Recolecciones', tipo: 'Lista', siguiente: 'Abrir una recolección' },
      { ruta: '/logistica/recolecciones/SC/OUT/48214', titulo: 'Recolección SC/OUT/48214', tipo: 'Formulario' },
      { ruta: '/logistica/recolecciones/nuevo', titulo: 'Recolección nueva', tipo: 'Nuevo' },
    ],
  },
  {
    id: 'calidad',
    nombre: 'Calidad',
    descripcion: 'Control de lotes producidos: liberar o rechazar (hard-stop).',
    pantallas: [
      { ruta: '/calidad', titulo: 'Controles de calidad', tipo: 'Lista', siguiente: 'Abrir un control' },
      { ruta: '/calidad/BOL-2026-0001', titulo: 'Control de BOL-2026-0001', tipo: 'Formulario' },
      { ruta: '/calidad/nuevo', titulo: 'Control nuevo', tipo: 'Nuevo' },
    ],
  },
  {
    id: 'traslados',
    nombre: 'Traslados entre plantas',
    descripcion: 'Salida de lotes liberados hacia tránsito.',
    pantallas: [
      { ruta: '/logistica/traslados', titulo: 'Traslados', tipo: 'Lista', siguiente: 'Abrir un traslado' },
      { ruta: '/logistica/traslados/PIM/OUT/48213', titulo: 'Traslado PIM/OUT/48213', tipo: 'Formulario', siguiente: 'Llega a la planta destino' },
      { ruta: '/logistica/traslados/nuevo', titulo: 'Traslado nuevo', tipo: 'Nuevo' },
    ],
  },
  {
    id: 'recepcion',
    nombre: 'Recepción',
    descripcion: 'Entrada en la planta destino de los lotes en tránsito.',
    pantallas: [
      { ruta: '/logistica/recepcion', titulo: 'Recepciones', tipo: 'Lista', siguiente: 'Abrir una recepción' },
      { ruta: '/logistica/recepcion/SC/IN/50974', titulo: 'Recepción SC/IN/50974', tipo: 'Formulario' },
      { ruta: '/logistica/recepcion/nuevo', titulo: 'Recepción nueva', tipo: 'Nuevo' },
    ],
  },
  {
    id: 'entregas',
    nombre: 'Entregas al cliente',
    descripcion: 'Salida de producto terminado liberado hacia el cliente.',
    pantallas: [
      { ruta: '/logistica/entregas', titulo: 'Entregas', tipo: 'Lista', siguiente: 'Abrir una entrega' },
      { ruta: '/logistica/entregas/SC/OUT/31688', titulo: 'Entrega SC/OUT/31688', tipo: 'Formulario' },
      { ruta: '/logistica/entregas/nuevo', titulo: 'Entrega nueva', tipo: 'Nuevo' },
    ],
  },
];

/** Nombre de archivo de la captura de una ruta. */
export function archivo(ruta: string): string {
  return (ruta === '/' ? 'inicio' : ruta.slice(1).replace(/\//g, '_')) + '.png';
}
