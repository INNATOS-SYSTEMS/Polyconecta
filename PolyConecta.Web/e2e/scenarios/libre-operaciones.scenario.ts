import { Paso, guion } from './runner';

const GUARDAR: Paso = { pulsar: '.o_statusbar button', texto: 'Guardar' };
const VALIDAR: Paso = { pulsar: '.o_statusbar button', texto: 'Validar' };
const MODAL = 'div.position-fixed';
const CAPTURA = 'main .o_line_capture';
const lote = (nombre: string): Paso => ({ pulsar: `main input[aria-label="${nombre}"]` });

/** Recolección libre (D-55): MP → WIP sin OF; el saldo queda sin asignar. Después, su devolución REC-RET. */
guion({
  nombre: 'recolección y devolución libres',
  soloAngular: true,
  pasos: [
    { ir: '/recolecciones' },
    { pulsar: 'pc-boton-nuevo button' },
    GUARDAR,
    { control: 'exige líneas', en: 'main', esperado: /Agregue al menos una línea\./ },
    { capturar: `${CAPTURA} input[list]`, valor: 'GA502022' },
    { control: 'unidad del producto', campo: `${CAPTURA} input[placeholder=Unidad]`, esperado: /^KGS$/, editable: false },
    { capturar: `${CAPTURA} input[placeholder=Cantidad]`, valor: '100' },
    { pulsar: `${CAPTURA} button`, texto: 'Agregar' },
    GUARDAR,
    { control: 'recolección sin OF', en: 'main', esperado: /PIM\/OUT\/\d+ Orden de Fabricación 0 .*Recolección de materia prima.*PIM\/Stock\/MP.*PIM\/WIP.*GA502022 LINEAL BUTENO 100\.0/ },
    { pulsar: 'main td button.btn-link' },
    { capturar: `${MODAL} input[placeholder^=Escanear]`, valor: 'GA-2608' },
    { capturar: `${MODAL} input[placeholder=Cantidad]`, valor: '100' },
    { pulsar: `${MODAL} .input-group button` },
    { pulsar: `${MODAL} button`, texto: 'Cerrar' },
    VALIDAR,
    { control: 'validada', en: 'main', esperado: /Contpaq ID TR-\d+/ },
    { navegar: '/inventario' },
    { capturar: '.o_search_bar input', valor: 'GA-2608' },
    { control: 'saldo en WIP sin OF', en: 'main', esperado: /PIM\/Stock\/MP \(1\) 300\.0 KGS PIM\/WIP \(1\) 100\.0 KGS/ },
    { navegar: '/recolecciones/nuevo' },
    { elegir: 'main select[name=tipo]', valor: 'devolucion' },
    { capturar: `${CAPTURA} input[list]`, valor: 'GA502022' },
    { capturar: `${CAPTURA} input[placeholder=Cantidad]`, valor: '40' },
    { pulsar: `${CAPTURA} button`, texto: 'Agregar' },
    GUARDAR,
    { control: 'devolución', en: 'main', esperado: /PIM\/IN\/\d+ .*Devolución de recolección.*PIM\/WIP.*PIM\/Stock\/MP/ },
    { pulsar: 'main td button.btn-link' },
    { control: 'ofrece el saldo de WIP', en: MODAL, esperado: /GA-2608 100\.0 KGS/ },
    { pulsar: `${MODAL} button`, texto: 'Tomar' },
    { capturar: `${MODAL} input[placeholder=Cantidad]`, valor: '40' },
    { pulsar: `${MODAL} .input-group button` },
    { pulsar: `${MODAL} button`, texto: 'Cerrar' },
    VALIDAR,
    { navegar: '/inventario' },
    { capturar: '.o_search_bar input', valor: 'GA-2608' },
    { control: 'devuelto a MP', en: 'main', esperado: /PIM\/Stock\/MP \(1\) 340\.0 KGS PIM\/WIP \(1\) 60\.0 KGS/ },
  ],
});

/** Traslado libre (solo liberados) que deja lotes en tránsito, y recepción libre (solo TRANS/*, D-56). */
guion({
  nombre: 'traslado y recepción libres',
  soloAngular: true,
  pasos: [
    { ir: '/recepcion/nuevo' },
    { elegir: 'main select[name=planta]', valor: 'SC' },
    { control: 'nada en tránsito', en: 'main', esperado: /No hay lotes que cumplan la regla en SC/ },
    { navegar: '/traslados' },
    { pulsar: 'pc-boton-nuevo button' },
    { control: 'solo lotes liberados', en: 'main', esperado: /^(?!.*R003-IV310-26\.S)(?!.*R006-IV310-26)(?=.*R004-IV310-26 ROLLO TUB 20\.5 370 \(Maestro\) PIM\/Stock\/PT 100\.0 KGS)/ },
    lote('R004-IV310-26'),
    lote('R005-IV310-26'),
    GUARDAR,
    { control: 'traslado creado', en: 'main', esperado: /PIM\/OUT\/\d+ .*Traspaso PIM a SC.*PIM\/Stock.*SC\/Stock\/MP.*PT3413 C455 .* 200\.0 0\.0 KGS 2 lote\(s\)/ },
    { control: 'sin origen', en: '.o_button_box', esperado: /Orden de Fabricación 0 Recepción 0/ },
    VALIDAR, VALIDAR, VALIDAR, VALIDAR,
    { control: 'traslado hecho', en: 'main', esperado: /Contpaq ID TR-\d+.*200\.0 200\.0 KGS/ },
    { navegar: '/recepcion' },
    { pulsar: 'pc-boton-nuevo button' },
    { elegir: 'main select[name=planta]', valor: 'SC' },
    { control: 'ahora en tránsito', en: 'main', esperado: /R004-IV310-26 .* TRANS\/SC 100\.0 KGS.*R005-IV310-26/ },
    lote('R004-IV310-26'),
    GUARDAR,
    VALIDAR, VALIDAR, VALIDAR,
    { control: 'recepción hecha', en: 'main', esperado: /SC\/IN\/\d+.*TRANS\/SC.*SC\/Stock\/MP.*Contpaq ID TR-\d+/ },
    { navegar: '/inventario' },
    { capturar: '.o_search_bar input', valor: 'R00' },
    { control: 'inventario', en: 'main', esperado: /SC\/Stock\/MP \(3\).*TRANS\/SC \(1\) 100\.0 KGS/ },
  ],
});

/** Entrega libre: cliente y lotes liberados, sin pedido; al validar salen del inventario. */
guion({
  nombre: 'entrega libre',
  soloAngular: true,
  pasos: [
    { ir: '/entregas' },
    { pulsar: 'pc-boton-nuevo button' },
    { elegir: 'main select[name=planta]', valor: 'SC' },
    lote('IV310-26-C01'),
    GUARDAR,
    { control: 'exige cliente', en: 'main', esperado: /Capture el cliente\./ },
    { capturar: 'main input[name=cliente]', valor: 'CLIENTE MOSTRADOR' },
    GUARDAR,
    { control: 'entrega creada', en: 'main', esperado: /SC\/OUT\/\d+ Pedido de Venta 0 .*Cliente: CLIENTE MOSTRADOR.*PT1113 C567 .* 2,000\.0 0\.0 MIL 1 lote\(s\)/ },
    { pulsar: 'main td button.btn-link' },
    { elegirLote: `${MODAL} input[placeholder^=Escanear]`, lote: 'R003-IV310-26.S' },
    { control: 'no acepta lotes no liberados', en: MODAL, esperado: /Lote R003-IV310-26\.S no encontrado o no aprobado\./ },
    { pulsar: `${MODAL} button`, texto: 'Cerrar' },
    VALIDAR, VALIDAR, VALIDAR,
    { control: 'entrega hecha', en: 'main', esperado: /Contpaq ID REM-\d+.*2,000\.0 2,000\.0 MIL/ },
    { navegar: '/inventario' },
    { capturar: '.o_search_bar input', valor: 'IV310-26-C0' },
    { control: 'salió del inventario', en: 'main', esperado: /SC\/Stock\/PT \(1\) 1,000\.0 MIL/ },
    { navegar: '/entregas' },
    { control: 'en la lista', en: 'main', esperado: /SC\/OUT\/\d+ Entrega a cliente CLIENTE MOSTRADOR Hecho/ },
  ],
});
