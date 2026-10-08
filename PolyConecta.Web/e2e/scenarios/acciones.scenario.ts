import { Paso, guion } from './runner';

const CAPTURA = 'main .o_line_capture';
const capturarLinea = (clave: string, cantidad: string, boton = 'Agregar'): Paso[] => [
  { capturar: `${CAPTURA} input[list], ${CAPTURA} input[placeholder=Lote]`, valor: clave },
  { capturar: `${CAPTURA} input[placeholder=Cantidad]`, valor: cantidad },
  { pulsar: `${CAPTURA} button`, texto: boton },
];

/** Acciones de la OF: componentes, subproductos, lotes de producción, pestañas y planeación. */
guion({
  nombre: 'acciones de la OF',
  pasos: [
    { ir: '/fabricacion/BOL-2026-0001' },
    ...capturarLinea('PT1113 C567', '120'),
    { control: 'componente agregado', en: 'main' },
    { pulsar: 'main tbody button[title=Editar]' },
    { control: 'componente en edición', en: 'main' },
    { pulsar: `${CAPTURA} button[title=Cancelar]` },
    { control: 'edición cancelada', en: 'main' },
    { pulsar: 'main tbody button[title=Editar]' },
    { capturar: `${CAPTURA} input[placeholder=Cantidad]`, valor: '77' },
    { pulsar: `${CAPTURA} button`, texto: 'Guardar' },
    { control: 'componente editado', en: 'main' },
    { pulsar: 'main tbody button[title=Eliminar]' },
    { control: 'componente quitado', en: 'main' },
    { pulsar: 'main .nav-link', texto: 'Subproductos' },
    { control: 'subproductos', en: 'main' },
    ...capturarLinea('PT1113 C567', '15'),
    { control: 'subproducto agregado', en: 'main' },
    { pulsar: 'main tbody input[type=checkbox]' },
    { control: 'subproducto producido', en: 'main' },
    { pulsar: 'main .nav-link', texto: 'Producción' },
    ...capturarLinea('R009-BOL-2026-0001', '300'),
    { control: 'lote capturado', en: 'main' },
    { pulsar: 'main tbody button[title=Eliminar]' },
    { control: 'lote quitado', en: 'main' },
    { pulsar: 'main .nav-link', texto: 'Planeación' },
    { control: 'planeación en borrador', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Confirmar' },
    { control: 'planeación tras confirmar', en: 'main' },
    { pulsar: 'main tbody button[title=Editar]' },
    { control: 'planeación en edición', en: 'main' },
    { pulsar: 'main .nav-link', texto: 'Componentes' },
    { control: 'componentes tras confirmar', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Cancelar' },
    { control: 'volver a la lista', en: 'main' },
  ],
});

/** Calidad: aprobar y fallar desde la barra de estado, y la pestaña de notas. */
guion({
  nombre: 'calidad desde la barra de estado',
  pasos: [
    { ir: '/fabricacion/BOL-2026-0001' },
    { pulsar: 'main .nav-link', texto: 'Producción' },
    ...capturarLinea('R001-BOL-2026-0001', '100'),
    ...capturarLinea('R002-BOL-2026-0001', '100'),
    ...capturarLinea('R003-BOL-2026-0001', '100'),
    { pulsar: '.o_smart_button', texto: 'Calidad' },
    { control: 'tres pendientes', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Aprueba' },
    { pulsar: '.o_statusbar button', texto: 'Falla' },
    { pulsar: '[data-dialogo="confirmar"]', soloAngular: true },
    { control: 'barra de estado', en: 'main' },
    { pulsar: 'main tbody button', texto: 'Falla' },
    { pulsar: '[data-dialogo="confirmar"]', soloAngular: true },
    { control: 'sin pendientes', en: 'main' },
    { control: 'aprueba deshabilitado', habilitado: '.o_statusbar button', texto: 'Aprueba', esperado: false },
    { pulsar: 'main .nav-link', texto: 'Notas' },
    { control: 'notas', en: 'main' },
    { navegar: '/calidad' },
    { control: 'lista de calidad', en: 'main' },
    { navegar: '/captura-masiva' },
    { control: 'captura masiva', en: 'main' },
  ],
});

/** Incidencias: alta con y sin los campos obligatorios. */
guion({
  nombre: 'alta de incidencias',
  pasos: [
    { ir: '/incidencias' },
    { capturar: 'main .row .col-md-3 input', valor: 'sin centro' },
    { pulsar: 'main .row button', texto: 'Agregar Incidencia' },
    { control: 'sin centro no agrega', en: 'main' },
    { capturar: 'main .row .col-md-2:nth-child(1) input', valor: 'EXT-05' },
    { capturar: 'main .row .col-md-2:nth-child(2) input', valor: 'Paro' },
    { capturar: 'main .row .col-md-1:nth-child(4) input', valor: '19:00' },
    { capturar: 'main .row .col-md-1:nth-child(5) input', valor: '20:30' },
    { pulsar: 'main .row button', texto: 'Agregar Incidencia' },
    { control: 'incidencia agregada', en: 'main', esperado: /EXT-05/ },
  ],
});

/** Pedido: ficha técnica (abrir, editar, aceptar, volver a abrir) y smart buttons. */
guion({
  nombre: 'ficha técnica y smart buttons del pedido',
  pasos: [
    { ir: '/pedidos/IV310-26' },
    { pulsar: 'main button[title="Ficha técnica"]' },
    { control: 'ficha', en: 'div.position-fixed' },
    { capturar: 'div.position-fixed input', valor: 'PEBD' },
    { pulsar: 'div.position-fixed button', texto: 'Aceptar' },
    { pulsar: 'main button[title="Ficha técnica"]' },
    { control: 'ficha tras editar', en: 'div.position-fixed' },
    { pulsar: 'div.position-fixed button', texto: 'Cancelar' },
    { pulsar: '.o_smart_button', texto: 'Entrega' },
    { control: 'entrega desde el pedido', en: 'main' },
    { pulsar: '.o_smart_button', texto: 'Pedido' },
    { control: 'regreso al pedido', en: 'main' },
  ],
});
