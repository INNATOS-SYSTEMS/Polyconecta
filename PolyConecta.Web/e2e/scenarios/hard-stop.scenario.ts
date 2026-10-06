import { guion } from './runner';

/** US-2, escenario 3: con un lote en revisión, la OF no se puede cerrar. */
guion({
  nombre: 'hard-stop de calidad',
  pasos: [
    { ir: '/fabricacion/BOL-2026-0001' },
    { pulsar: '.o_statusbar button', texto: 'Confirmar' },
    { pulsar: 'main .nav-link', texto: 'Producción' },
    { capturar: 'main .o_line_capture input[placeholder=Lote]', valor: 'R001-BOL-2026-0001' },
    { capturar: 'main .o_line_capture input[placeholder=Cantidad]', valor: '5000' },
    { pulsar: 'main .o_line_capture button', texto: 'Agregar' },
    { control: 'lote en revisión', en: 'main' },
    { control: 'cerrar bloqueado', habilitado: '.o_statusbar button', texto: 'Cerrar Producción', esperado: false },
  ],
});
