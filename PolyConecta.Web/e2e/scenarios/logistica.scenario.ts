import { guion } from './runner';

/** El modal de lotes: en el prototipo, un div fijo; en la réplica, el marco de pc-odoo-dialog (spec 011). */
const MODAL = ':is(div.position-fixed, .o_dialog_overlay)';

/** US-2, escenario 4: traslado, recepción y entrega, con estados y Contpaq ID en cada paso. */
guion({
  nombre: 'traslado, recepción y entrega',
  pasos: [
    { ir: '/traslados/PIM/OUT/48213' },
    { control: 'traslado inicial', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Comprobar disponibilidad' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    { control: 'traslado paso 1', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    { control: 'traslado listo', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    { control: 'traslado sin lotes', en: 'main' },
    { pulsar: 'main td :is(button.btn-link, button.o_btn_link)' },
    { elegirLote: `${MODAL} input[placeholder^=Escanear]`, lote: 'R001-IV310-26' },
    { elegirLote: `${MODAL} input[placeholder^=Escanear]`, lote: 'NO-EXISTE' },
    { control: 'modal traslado', en: MODAL },
    { pulsar: `${MODAL} button`, texto: 'Cerrar' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    { control: 'traslado hecho', en: 'main' },
    { pulsar: '.o_smart_button', texto: 'Recepción' },
    { control: 'recepción', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    { pulsar: 'main td :is(button.btn-link, button.o_btn_link)' },
    { elegirLote: `${MODAL} input[placeholder^=Escanear]`, lote: 'R001-IV310-26' },
    { elegirLote: `${MODAL} input[placeholder^=Escanear]`, lote: 'R002-IV310-26' },
    { elegirLote: `${MODAL} input[placeholder^=Escanear]`, lote: 'R003-IV310-26' },
    { pulsar: `${MODAL} button`, texto: 'Cerrar' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    // La réplica confirma lo que entra y la regla de D-56 en "Validar recepción" (P6 de la spec 011).
    { pulsar: '[data-dialogo="confirmar"]', soloAngular: true },
    { control: 'recepción hecha', en: 'main' },
    { navegar: '/entregas/SC/OUT/31688' },
    { control: 'entrega', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    { pulsar: 'main td :is(button.btn-link, button.o_btn_link)' },
    { control: 'modal entrega', en: MODAL },
    { pulsar: `${MODAL} button`, texto: 'Cerrar' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    { control: 'entrega sin lotes', en: 'main' },
    { navegar: '/pedidos/IV310-26' },
    { control: 'pedido tras logística', en: 'main' },
    { navegar: '/inventario' },
    { control: 'inventario tras logística', en: 'main table' },
  ],
});
