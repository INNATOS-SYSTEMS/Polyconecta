import { Paso, guion } from './runner';

const MENU = '.o_search_menu_item';
const abrirMenu: Paso = { pulsar: 'button[title="Filtros del modelo"]' };
const filtro = (nombre: string): Paso => ({ pulsar: MENU, texto: nombre });
const buscar = (texto: string): Paso => ({ capturar: '.o_search_bar input', valor: texto });

/**
 * US-2, escenario 5: buscar texto, aplicar dos filtros del mismo campo (se unen con "o") y dos de
 * campos distintos (se cruzan), agrupar y quitar una faceta.
 */
guion({
  nombre: 'búsqueda en pedidos',
  pasos: [
    { ir: '/pedidos' },
    buscar('IV310'),
    { control: 'texto', en: 'main' },
    buscar(''),
    abrirMenu,
    filtro('Borrador'),
    filtro('Confirmado'),
    { control: 'dos estados', en: 'main' },
    { control: 'menú', en: '.o_search_menu' },
    { pulsar: '.o_search_facet_remove' },
    { control: 'faceta quitada', en: 'main' },
  ],
});

guion({
  nombre: 'búsqueda en fabricación',
  pasos: [
    { ir: '/fabricacion' },
    buscar('BOL'),
    { control: 'texto', en: 'main' },
    buscar(''),
    abrirMenu,
    filtro('Extrusión'),
    filtro('Impresión'),
    { control: 'dos procesos', en: 'main' },
    filtro('Borrador'),
    { control: 'proceso y estado', en: 'main' },
    { pulsar: '.o_search_facet_remove' },
    { control: 'primera faceta quitada', en: 'main' },
    { pulsar: MENU, texto: 'Quitar todos los filtros' },
    { control: 'sin filtros', en: 'main' },
  ],
});

guion({
  nombre: 'fabricación filtrada por pedido',
  pasos: [
    { ir: '/fabricacion?pedido=IV310-26' },
    { control: 'faceta de contexto', en: 'main' },
    { pulsar: '.o_search_facet_remove' },
    { control: 'sin faceta de contexto', en: 'main' },
  ],
});

guion({
  nombre: 'búsqueda en inventario',
  pasos: [
    { ir: '/inventario' },
    buscar('PT1113'),
    { control: 'texto', en: 'main' },
    buscar(''),
    abrirMenu,
    filtro('PIM'),
    filtro('Santa Cruz'),
    { control: 'dos plantas', en: 'main' },
    { pulsar: MENU, texto: 'Ubicación' },
    { control: 'agrupado', en: 'main' },
    { pulsar: '.o_search_facet_remove' },
    { control: 'faceta quitada', en: 'main' },
  ],
});

guion({
  nombre: 'búsqueda en recolecciones e incidencias',
  pasos: [
    { ir: '/recolecciones' },
    abrirMenu,
    filtro('Borrador'),
    filtro('Hecho'),
    filtro('Recolecciones'),
    { control: 'recolecciones filtradas', en: 'main' },
    { navegar: '/incidencias' },
    buscar('EXT'),
    { control: 'incidencias por texto', en: 'main' },
    buscar(''),
    abrirMenu,
    filtro('Hoy'),
    filtro('Bolseo'),
    { control: 'incidencias filtradas', en: 'main' },
  ],
});
