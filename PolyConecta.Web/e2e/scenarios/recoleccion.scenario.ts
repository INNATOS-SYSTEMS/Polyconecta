import { guion } from './runner';

/** El modal de lotes: en el prototipo, un div fijo; en la réplica, el marco de pc-odoo-dialog (spec 011). */
const MODAL = ':is(div.position-fixed, .o_dialog_overlay)';

/** Recolección: validación parcial con backorder, comprobar disponibilidad y cancelar. */
guion({
  nombre: 'recolección parcial con backorder',
  pasos: [
    { ir: '/fabricacion/BOL-2026-0001' },
    { pulsar: '.o_statusbar button', texto: 'Confirmar' },
    { pulsar: '.o_smart_button', texto: 'Recolección' },
    { control: 'recolección', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Comprobar disponibilidad' },
    { control: 'tras comprobar', en: 'main' },
    { pulsar: 'main td :is(button.btn-link, button.o_btn_link)' },
    { pulsar: `${MODAL} button`, texto: 'Tomar' },
    { capturar: `${MODAL} input[placeholder=Cantidad]`, valor: '10' },
    { pulsar: `${MODAL} :is(.input-group button, [data-lote-agregar])` },
    { control: 'asignación parcial', en: MODAL },
    { pulsar: `${MODAL} tbody :is(button.text-danger, [data-lote-quitar])` },
    { control: 'asignación quitada', en: MODAL },
    { pulsar: `${MODAL} button`, texto: 'Tomar' },
    { capturar: `${MODAL} input[placeholder=Cantidad]`, valor: '10' },
    { pulsar: `${MODAL} :is(.input-group button, [data-lote-agregar])` },
    { pulsar: `${MODAL} button`, texto: 'Cerrar' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    // La réplica confirma la validación parcial en un diálogo (aclaración P3 de la spec 011).
    { pulsar: '[data-dialogo="confirmar"]', soloAngular: true },
    { control: 'backorder abierto', en: 'main' },
    { navegar: '/recolecciones' },
    { control: 'lista con backorder', en: 'main' },
    { pulsar: 'main tbody tr' },
    { control: 'primer documento de la lista', en: 'main' },
  ],
});

/** Lote inexistente en el escáner del modal: mismo error. */
guion({
  nombre: 'recolección con lote inexistente',
  pasos: [
    { ir: '/fabricacion/BOL-2026-0001' },
    { pulsar: '.o_statusbar button', texto: 'Confirmar' },
    { pulsar: '.o_smart_button', texto: 'Recolección' },
    { pulsar: 'main td :is(button.btn-link, button.o_btn_link)' },
    { capturar: `${MODAL} input[placeholder^=Escanear]`, valor: 'NO-EXISTE', enter: true },
    { control: 'error de lote', en: MODAL },
  ],
});
