import { Paso, guion } from './runner';

const GUARDAR: Paso = { pulsar: '.o_statusbar button', texto: 'Guardar' };
const CAPTURA = 'main .o_line_capture';

/** US-3: pedido libre (FR-012, D-53, D-74) con la estructura completa de D-136. Solo Angular: Blazor no tiene "Nuevo". */
guion({
  nombre: 'pedido libre',
  soloAngular: true,
  pasos: [
    { ir: '/pedidos' },
    { pulsar: 'pc-boton-nuevo button' },
    // D-136: el "Nuevo" tiene la estructura completa: etapas, maestro, pestaña Detalle y chatter inactivo.
    { control: 'hoja nueva', en: 'main', esperado: /Pedidos Nuevo .*Guardar Descartar Libre .*Borrador.*Pedido Nuevo.*Detalle.*Las notas se habilitan al guardar\./ },
    { control: 'chatter inactivo', habilitado: 'main pc-odoo-chatter-drawer button', texto: 'Enviar', esperado: false },
    GUARDAR,
    { control: 'sin cliente', en: 'main', esperado: /Capture el cliente\./ },
    // Las líneas se capturan antes de guardar y se guardan junto con el maestro.
    { capturar: `${CAPTURA} input[list]`, valor: 'PT1113 C567' },
    { control: 'unidad del producto, no editable', campo: `${CAPTURA} input[placeholder=Unidad]`, esperado: /^MIL$/, editable: false },
    { capturar: `${CAPTURA} input[placeholder=Cantidad]`, valor: '100' },
    // "Agregar" se habilita con el precio: la regla del precio está en pedido-libre.spec.ts.
    { control: 'sin precio no agrega', habilitado: `${CAPTURA} button`, texto: 'Agregar', esperado: false },
    { capturar: `${CAPTURA} input[placeholder="Precio unitario"]`, valor: '7.5' },
    { elegir: `${CAPTURA} select`, valor: 'USD' },
    { pulsar: `${CAPTURA} button`, texto: 'Agregar' },
    { control: 'línea antes de guardar', en: 'main [data-lineas-nuevas] tbody', esperado: /PT1113 C567 - BOLSA MEDIANA 44X84 C\.430 BOL-004 \[77\] 100\.0 MIL \$7\.50 USD \$750\.00/ },
    { capturar: 'main input[name=cliente]', valor: 'CLIENTE LIBRE SA' },
    { capturar: 'main input[name=ordenCompra]', valor: 'OC-77' },
    GUARDAR,
    { control: 'pedido creado con su línea', en: 'main', esperado: /PV-2026-0001.*CLIENTE LIBRE SA.*OC-77.*Contpaq ID — \(se asigna al confirmar\).*PT1113 C567/ },
    { control: 'smart buttons sin origen', en: '.o_button_box', esperado: /Entregas 0 Órdenes de fabricación 0/ },
    { control: 'chatter activo', habilitado: 'main pc-odoo-chatter-drawer button', texto: 'Enviar', esperado: true },
    { control: 'línea con precio y moneda', en: 'main tbody', esperado: /PT1113 C567 - BOLSA MEDIANA 44X84 C\.430 BOL-004 \[77\] 100\.0 .* MIL \$7\.50 USD \$750\.00/ },
    { control: 'totales en CONTPAQi', en: 'main', esperado: /IVA, descuentos y totales los calcula CONTPAQi/ },
    { pulsar: '.o_statusbar button', texto: 'Confirmar' },
    { control: 'Contpaq ID simulado', en: 'main', esperado: /Confirmado.*Contpaq ID 26201/ },
    { pulsar: '.o_statusbar button', texto: 'Autorizar' },
    { pulsar: '.o_statusbar button', texto: 'Autorizar' },
    { control: 'dos firmas', en: 'main', esperado: /Autorización de Comercial firmada por Administrator.*Autorización de Cobranza firmada por Crédito y Cobranza/ },
    { control: 'autorizado', en: '.o_statusbar + div pc-odoo-status-pipeline, main', esperado: /Autorizado/ },
    { navegar: '/pedidos' },
    { control: 'en la lista', en: 'main', esperado: /PV-2026-0001 CLIENTE LIBRE SA/ },
    { navegar: '/pedidos/IV310-26' },
    { control: 'la semilla no cambia', en: 'main', esperado: /Confirmar.*Contpaq ID 26200/ },
  ],
});
