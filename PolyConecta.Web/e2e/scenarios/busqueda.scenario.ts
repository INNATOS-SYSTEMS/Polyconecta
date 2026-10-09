import { Paso, guion } from './runner';

const MENU = '.o_search_menu_item';
const abrirMenu: Paso = { pulsar: 'button[title="Filtros del modelo"]' };
const filtro = (nombre: string): Paso => ({ pulsar: MENU, texto: nombre });
const buscar = (texto: string): Paso => ({ capturar: '.o_search_bar input', valor: texto });


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
    // El menú de Fabricación también agrupa y guarda favoritos (spec 011, D-135): se comparan la lista y las facetas.
    { control: 'dos procesos', en: 'main .p-4' },
    filtro('Borrador'),
    { control: 'proceso y estado', en: 'main .p-4' },
    { control: 'facetas de proceso y estado', en: '.o_search_bar' },
    { pulsar: '.o_search_facet_remove' },
    { control: 'primera faceta quitada', en: 'main .p-4' },
    { control: 'barra sin la primera faceta', en: '.o_search_bar' },
    { pulsar: MENU, texto: 'Quitar todos los filtros' },
    { control: 'sin filtros', en: 'main .p-4' },
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
    // Agrupada, la lista nueva pagina grupos, como Odoo ("1-1 / 1"); el prototipo contaba existencias. Se compara la tabla.
    { control: 'texto', en: 'main table' },
    buscar(''),
    abrirMenu,
    filtro('PIM'),
    filtro('Santa Cruz'),
    { control: 'dos plantas', en: 'main table' },
    { pulsar: MENU, texto: 'Ubicación' },
    { control: 'agrupado', en: 'main table' },
    { pulsar: '.o_search_facet_remove' },
    { control: 'faceta quitada', en: 'main table' },
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
    // El menú también agrupa y guarda favoritos (spec 011, D-135): se comparan la lista y las facetas.
    { control: 'recolecciones filtradas', en: 'main .p-4' },
    { control: 'facetas de recolecciones', en: '.o_search_bar' },
    { navegar: '/incidencias' },
    buscar('EXT'),
    { control: 'incidencias por texto', en: 'main' },
    buscar(''),
    abrirMenu,
    filtro('Hoy'),
    filtro('Bolseo'),
    // El menú de Incidencias también agrupa y guarda favoritos (spec 011, D-135): se comparan la lista y las facetas.
    { control: 'incidencias filtradas', en: 'main .p-4' },
    { control: 'facetas de incidencias', en: '.o_search_bar' },
  ],
});
