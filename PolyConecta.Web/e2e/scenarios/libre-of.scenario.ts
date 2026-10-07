import { Paso, guion } from './runner';

const GUARDAR: Paso = { pulsar: '.o_statusbar button', texto: 'Guardar' };
const MODAL = 'div.position-fixed';
const CAPTURA = 'main .o_line_capture';

/** US-3, escenario 1: OF sin pedido; lotes con el folio de la OF raíz (D-54); genera recolección y control. */
guion({
  nombre: 'OF libre',
  soloAngular: true,
  pasos: [
    { ir: '/fabricacion' },
    { pulsar: 'pc-boton-nuevo button' },
    { elegir: 'main select[name=proceso]', valor: 'Bolseo' },
    { capturar: 'main input[name=producto]', valor: 'PT1113 C567' },
    { control: 'unidad del producto', en: 'main', esperado: /Unidad MIL/ },
    // D-136 (aclaración P2): Componentes y Subproductos se capturan antes de guardar; Producción y Planeación, al guardar.
    { control: 'producción al guardar', habilitado: 'main .nav-link', texto: 'Producción', esperado: false },
    { control: 'planeación al guardar', habilitado: 'main .nav-link', texto: 'Planeación', esperado: false },
    { capturar: `${CAPTURA} input[list]`, valor: 'PT3413 C4235' },
    { capturar: `${CAPTURA} input[placeholder=Cantidad]`, valor: '50' },
    { pulsar: `${CAPTURA} button`, texto: 'Agregar' },
    { control: 'componente antes de guardar', en: 'main [data-lineas-nuevas=componentes] tbody', esperado: /PT3413 C4235 ROLLO TUB 20\.5 370 \(Impreso\) 50\.0/ },
    GUARDAR,
    { control: 'sin cantidad', en: 'main', esperado: /La cantidad debe ser mayor que cero\./ },
    { capturar: 'main input[name=cantidad]', valor: '1000' },
    GUARDAR,
    { control: 'OF creada con su componente', en: 'main', esperado: /BOL-2026-0002.*BOLSA MEDIANA 44X84 C\.430 BOL-004 \[77\].*1,000\.0 MIL.*Bolseo.*PT3413 C4235/ },
    { control: 'sin pedido', en: '.o_button_box', esperado: /^Pedido 0/ },
    { pulsar: '.o_statusbar button', texto: 'Confirmar' },
    { control: 'planeada con recolección', en: 'main', esperado: /Recolección 1.*Planeado/ },
    { pulsar: 'main .nav-link', texto: 'Producción' },
    { control: 'lote propuesto', campo: `${CAPTURA} input[placeholder=Lote]`, esperado: /^R001-BOL-2026-0002$/ },
    { capturar: `${CAPTURA} input[placeholder=Cantidad]`, valor: '400' },
    { pulsar: `${CAPTURA} button`, texto: 'Agregar' },
    { capturar: `${CAPTURA} input[placeholder=Cantidad]`, valor: '350' },
    { pulsar: `${CAPTURA} button`, texto: 'Agregar' },
    { control: 'lotes con folio de la OF raíz', en: 'main tbody', esperado: /R001-BOL-2026-0002 .*En revisión.*R002-BOL-2026-0002 .*En revisión/ },
    { pulsar: '.o_smart_button', texto: 'Recolección' },
    { control: 'su recolección', en: 'main', esperado: /SC\/OUT\/\d+ Orden de Fabricación 1 .*Recolección de rollos a conversión.*PT3413 C4235 ROLLO TUB 20\.5 370 \(Impreso\) 50\.0/ },
    { navegar: '/calidad/BOL-2026-0002' },
    { control: 'su control de calidad', en: 'main', esperado: /QC-BOL-2026-0002.*R001-BOL-2026-0002.*R002-BOL-2026-0002/ },
    { pulsar: 'main tbody button', texto: 'Aprueba' },
    { pulsar: 'main tbody button', texto: 'Falla' },
    { control: 'mismos efectos que una ligada', en: 'main tbody', esperado: /R001-BOL-2026-0002 .*Aprobado.*R002-BOL-2026-0002\.S .*Rechazado/ },
    { navegar: '/fabricacion' },
    { control: 'en la lista sin pedido', en: 'main', esperado: /BOL-2026-0002 BOLSA MEDIANA/ },
  ],
});

/** FR-013: el saldo de una recolección libre solo se liga a una OF con "Asignar saldo de WIP". */
guion({
  nombre: 'asignar saldo de WIP',
  soloAngular: true,
  pasos: [
    { ir: '/recolecciones/nuevo' },
    { capturar: `${CAPTURA} input[list]`, valor: 'PACT' },
    { capturar: `${CAPTURA} input[placeholder=Cantidad]`, valor: '200' },
    { pulsar: `${CAPTURA} button`, texto: 'Agregar' },
    GUARDAR,
    { pulsar: 'main td button.btn-link' },
    { pulsar: `${MODAL} button`, texto: 'Tomar' },
    { pulsar: `${MODAL} .input-group button` },
    { pulsar: `${MODAL} button`, texto: 'Cerrar' },
    { pulsar: '.o_statusbar button', texto: 'Validar' },
    { control: 'recolección libre validada', en: 'main', esperado: /Hecho/ },
    { navegar: '/fabricacion/nuevo' },
    { capturar: 'main input[name=producto]', valor: 'PT3413 C455' },
    { capturar: 'main input[name=cantidad]', valor: '300' },
    GUARDAR,
    { control: 'sin componentes no ofrece saldo', en: '.o_statusbar', esperado: /^(?!.*Asignar saldo de WIP)/ },
    { capturar: `${CAPTURA} input[list]`, valor: 'PACT' },
    { capturar: `${CAPTURA} input[placeholder=Cantidad]`, valor: '150' },
    { pulsar: `${CAPTURA} button`, texto: 'Agregar' },
    { control: 'ofrece la acción, no asigna sola', en: '.o_statusbar', esperado: /Asignar saldo de WIP/ },
    { pulsar: '.o_statusbar button', texto: 'Asignar saldo de WIP' },
    { control: 'saldo sin asignar', en: MODAL, esperado: /PACT-2609A PACT 200\.0 KGS/ },
    { capturar: `${MODAL} input[type=number]`, valor: '150' },
    { pulsar: `${MODAL} tbody button`, texto: 'Asignar' },
    { control: 'queda el resto sin asignar', en: MODAL, esperado: /PACT-2609A PACT 50\.0 KGS/ },
    { pulsar: `${MODAL} button`, texto: 'Cerrar' },
    { navegar: '/inventario' },
    { capturar: '.o_search_bar input', valor: 'PACT-2609A' },
    // Dos renglones en WIP: 150 ligados a la OF y 50 todavía sin asignar.
    { control: 'WIP en inventario', en: 'main', esperado: /PIM\/WIP \(2\) 200\.0 KGS/ },
  ],
});
