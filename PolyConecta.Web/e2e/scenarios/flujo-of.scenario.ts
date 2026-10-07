import { Paso, guion } from './runner';

const MODAL = 'div.position-fixed';

/** Captura un lote de producción en la pestaña Producción de la OF abierta. */
const capturarLote = (lote: string, cantidad: string): Paso[] => [
  { capturar: 'main .o_line_capture input[placeholder=Lote]', valor: lote },
  { capturar: 'main .o_line_capture input[placeholder=Cantidad]', valor: cantidad },
  { pulsar: 'main .o_line_capture button', texto: 'Agregar' },
];

/**
 * US-2, escenario 2: confirmar una OF, validar su recolección con lotes, capturar rollos, aprobar uno,
 * rechazar otro (queda con .S) y cerrar. Compara estados, saldo en WIP, lotes e inventario.
 */
guion({
  nombre: 'flujo completo de la OF',
  pasos: [
    { ir: '/fabricacion/BOL-2026-0001' },
    { control: 'OF en borrador', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Confirmar' },
    { control: 'OF planeada', en: 'main' },
    { pulsar: '.o_smart_button', texto: 'Recolección' },
    { control: 'recolección liberada', en: 'main' },
    { pulsar: 'main td button.btn-link' },
    { control: 'modal de lotes', en: MODAL },
    { pulsar: `${MODAL} button`, texto: 'Tomar' },
    { pulsar: `${MODAL} .input-group button` },
    { control: 'lote asignado', en: MODAL },
    { pulsar: `${MODAL} button`, texto: 'Cerrar' },
    { control: 'recolección con lotes', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    { control: 'recolección validada', en: 'main', esperado: /Hecho/ },
    { navegar: '/inventario' },
    { control: 'inventario tras recolección (WIP)', en: 'main' },
    { navegar: '/fabricacion/BOL-2026-0001' },
    { pulsar: 'main .nav-link', texto: 'Producción' },
    ...capturarLote('R001-BOL-2026-0001', '5000'),
    ...capturarLote('R002-BOL-2026-0001', '4000'),
    { control: 'rollos capturados', en: 'main' },
    { navegar: '/calidad/BOL-2026-0001' },
    { control: 'calidad pendiente', en: 'main' },
    { pulsar: 'main button', texto: 'Aprueba' },
    { pulsar: 'main button', texto: 'Falla' },
    { control: 'uno aprobado y otro rechazado', en: 'main', esperado: /R002-BOL-2026-0001\.S/ },
    { navegar: '/fabricacion/BOL-2026-0001' },
    { pulsar: 'main .nav-link', texto: 'Producción' },
    { control: 'OF con calidad resuelta', en: 'main' },
    // El prototipo no ofrece devolución en la interfaz: con saldo en WIP el cierre queda bloqueado.
    { control: 'cierre bloqueado por WIP', habilitado: '.o_statusbar button', texto: 'Cerrar Producción', esperado: false },
  ],
});

/** Cierre de la OF: sin recolección pendiente ni saldo en WIP, con un rollo aprobado y otro rechazado. */
guion({
  nombre: 'cierre de la OF',
  pasos: [
    { ir: '/fabricacion/BOL-2026-0001' },
    { pulsar: '.o_statusbar button', texto: 'Confirmar' },
    { pulsar: '.o_smart_button', texto: 'Recolección' },
    { pulsar: '.o_statusbar button', texto: 'Cancelar' },
    { control: 'recolecciones tras cancelar', en: 'main' },
    { navegar: '/fabricacion/BOL-2026-0001' },
    { pulsar: 'main .nav-link', texto: 'Producción' },
    ...capturarLote('R001-BOL-2026-0001', '5000'),
    ...capturarLote('R002-BOL-2026-0001', '4000'),
    { navegar: '/calidad/BOL-2026-0001' },
    { pulsar: 'main button', texto: 'Aprueba' },
    { pulsar: 'main button', texto: 'Falla' },
    { control: 'calidad resuelta', en: 'main' },
    { navegar: '/fabricacion/BOL-2026-0001' },
    { control: 'cerrar habilitado', habilitado: '.o_statusbar button', texto: 'Cerrar Producción', esperado: true },
    { pulsar: '.o_statusbar button', texto: 'Cerrar Producción' },
    { control: 'OF cerrada', en: '.o_status_pipeline, main', esperado: /Hecho/ },
    { navegar: '/fabricacion' },
    { control: 'lista tras cerrar', en: 'main' },
    { navegar: '/inventario' },
    { control: 'inventario final', en: 'main' },
  ],
});
