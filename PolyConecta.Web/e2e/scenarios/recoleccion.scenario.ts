import { guion } from './runner';

const MODAL = 'div.position-fixed';

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
    { pulsar: 'main td button.btn-link' },
    { pulsar: `${MODAL} button`, texto: 'Tomar' },
    { capturar: `${MODAL} input[placeholder=Cantidad]`, valor: '10' },
    { pulsar: `${MODAL} .input-group button` },
    { control: 'asignación parcial', en: MODAL },
    { pulsar: `${MODAL} tbody button.text-danger` },
    { control: 'asignación quitada', en: MODAL },
    { pulsar: `${MODAL} button`, texto: 'Tomar' },
    { capturar: `${MODAL} input[placeholder=Cantidad]`, valor: '10' },
    { pulsar: `${MODAL} .input-group button` },
    { pulsar: `${MODAL} button`, texto: 'Cerrar' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
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
    { pulsar: 'main td button.btn-link' },
    { capturar: `${MODAL} input[placeholder^=Escanear]`, valor: 'NO-EXISTE', enter: true },
    { control: 'error de lote', en: MODAL },
  ],
});
