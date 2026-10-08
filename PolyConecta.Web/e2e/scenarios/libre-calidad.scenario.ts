import { Paso, guion } from './runner';

const GUARDAR: Paso = { pulsar: '.o_statusbar button', texto: 'Guardar' };
const CAPTURA = 'main .o_line_capture';

/** US-3: control de calidad libre, solo sobre lotes existentes; mismos efectos que el ligado. */
guion({
  nombre: 'control de calidad libre',
  soloAngular: true,
  pasos: [
    { ir: '/fabricacion/BOL-2026-0001' },
    { pulsar: 'main .nav-link', texto: 'Producción' },
    { capturar: `${CAPTURA} input[placeholder=Lote]`, valor: 'R101-IV310-26' },
    { capturar: `${CAPTURA} input[placeholder=Cantidad]`, valor: '500' },
    { pulsar: `${CAPTURA} button`, texto: 'Agregar' },
    { capturar: `${CAPTURA} input[placeholder=Lote]`, valor: 'R102-IV310-26' },
    { capturar: `${CAPTURA} input[placeholder=Cantidad]`, valor: '450' },
    { pulsar: `${CAPTURA} button`, texto: 'Agregar' },
    { navegar: '/calidad' },
    { pulsar: 'pc-boton-nuevo button' },
    GUARDAR,
    { control: 'exige lotes', en: 'main', esperado: /Elija al menos un lote existente\./ },
    { control: 'solo lotes en revisión, con su OF', en: 'main', esperado: /^(?!.*R001-IV310-26 BOL)(?=.*BOL-2026-0001 500\.0)/ },
    { pulsar: 'main input[aria-label="BOL-2026-0001 R101-IV310-26"]' },
    { pulsar: 'main input[aria-label="BOL-2026-0001 R102-IV310-26"]' },
    GUARDAR,
    { control: 'control creado', en: 'main', esperado: /QC-2026-0001.*— \(control libre\).*BOL-2026-0001 R101-IV310-26.*BOL-2026-0001 R102-IV310-26/ },
    { pulsar: '.o_statusbar button', texto: 'Aprueba' },
    { pulsar: '.o_statusbar button', texto: 'Falla' },
    { pulsar: '[data-dialogo="confirmar"]', soloAngular: true },
    { control: 'aprobado y rechazado con .S', en: 'main tbody', esperado: /R101-IV310-26 .*Aprobado.*R102-IV310-26\.S .*Rechazado/ },
    { control: 'sin pendientes', habilitado: '.o_statusbar button', texto: 'Aprueba', esperado: false },
    { navegar: '/calidad/BOL-2026-0001' },
    { control: 'el control ligado ve el mismo efecto', en: 'main tbody', esperado: /R101-IV310-26 .*Aprobado.*R102-IV310-26\.S .*Rechazado/ },
    { navegar: '/calidad' },
    { control: 'en la lista', en: 'main', esperado: /QC-2026-0001 — — \(libre\) — 2 Parcial/ },
  ],
});
