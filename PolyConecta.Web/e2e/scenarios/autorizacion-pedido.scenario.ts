import { guion } from './runner';

/** US-2, escenario 1: confirmar IV310-26, autorizar dos veces y revisar etapa, firmas y OF del pedido. */
guion({
  nombre: 'autorización del pedido',
  pasos: [
    { ir: '/pedidos/IV310-26' },
    { control: 'borrador', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Confirmar' },
    { control: 'confirmado', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Autorizar' },
    { control: 'una firma', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Autorizar' },
    // El prototipo no vuelve a pintar PocSalesOrderForm al autorizar (no escucha OnChange): se compara al volver a entrar.
    { navegar: '/pedidos' },
    { navegar: '/pedidos/IV310-26' },
    { control: 'autorizado', en: 'main' },
    { pulsar: '.o_smart_button', texto: 'Fabricación' },
    { control: 'OF del pedido', en: 'main' },
    { navegar: '/fabricacion/BOL-2026-0001' },
    { control: 'OF raíz tras autorizar', en: 'main' },
  ],
});

/** Captura de líneas del pedido: agregar con precio precargado, editar y quitar. */
guion({
  nombre: 'líneas del pedido',
  pasos: [
    { ir: '/pedidos/IV310-26' },
    { capturar: '.o_line_capture input[list]', valor: 'PT1113 C567' },
    { control: 'producto resuelto', en: '.o_line_capture' },
    { capturar: '.o_line_capture input[placeholder=Cantidad]', valor: '1500' },
    { pulsar: '.o_line_capture button', texto: 'Agregar' },
    { control: 'línea agregada', en: 'main' },
    { pulsar: 'main table button[title=Editar]' },
    { control: 'en edición', en: 'main' },
    { capturar: '.o_line_capture input[placeholder=Cantidad]', valor: '2500' },
    { pulsar: '.o_line_capture button', texto: 'Agregar' },
    { control: 'línea editada', en: 'main' },
    { pulsar: 'main table button[title=Eliminar]' },
    { control: 'línea quitada', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Confirmar' },
    { pulsar: '.o_statusbar button', texto: 'Autorizar' },
    { pulsar: '.o_statusbar button', texto: 'Autorizar' },
    // El prototipo no vuelve a pintar PocSalesOrderForm al autorizar (no escucha OnChange): se compara al volver a entrar.
    { navegar: '/pedidos' },
    { navegar: '/pedidos/IV310-26' },
    { control: 'líneas bloqueadas tras autorizar', en: 'main' },
    { control: 'editar deshabilitado', habilitado: 'main tbody button[title="El pedido ya fue autorizado"]', esperado: false },
  ],
});
