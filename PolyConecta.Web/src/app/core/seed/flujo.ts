import { Delivery, InterplantTransfer, Reception } from '../models/logistica';
import { Incidencia, ManufacturingOrder, ProductionLot } from '../models/produccion';
import { SalesOrder } from '../models/ventas';

/** Semilla de Services/OperationalFlowState.cs, valor por valor (FR-007). */
export const PEDIDO_FOLIO = 'IV310-26';
/** Clave del producto vendido en el pedido de la demo. */
export const CLAVE_VENDIDA = 'PT1113 C567';

/** Lista de precios de prueba: el precio se precarga al capturar la clave. */
export const LISTA_PRECIOS: Readonly<Record<string, number>> = {
  'PT1113 C567': 5.7,
  'PT1113 C580': 4.2,
  'PT3413 C4235': 62.5,
  'PT3413 C455': 48.0,
  'PT3413 C460': 49.5,
  'PT3413 C470': 45.0,
};

export const pedidoSemilla = (): SalesOrder =>
  new SalesOrder({
    folio: PEDIDO_FOLIO,
    cliente: 'EMPRESA MEXICANA DE MANUFACTURA',
    stage: 'Borrador',
    ordenCompraCliente: '3893',
    agente: 'Celia Villarreal',
    contpaqId: '26200',
    cantidadPedido: 5500,
    lineas: [{ clave: 'PT1113 C567', producto: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]', cantidad: 5500, unidad: 'PZA', precioUnitario: 5.7 }],
    procesos: [
      { proceso: 'Extrusion', activo: true, origen: 'PIM', producto: 'PT3413 C455' },
      { proceso: 'Impresion', activo: true, origen: 'SC', producto: 'PT3413 C4235' },
      { proceso: 'Bolseo', activo: true, origen: 'SC', producto: 'PT3413 C4236' },
    ],
  });

const lote = (l: string, real: number, unidad: string, estado: ProductionLot['estado']): ProductionLot => ({ lote: l, real, unidad, estado });
const fecha = (y: number, m: number, d: number, h = 0, min = 0) => new Date(y, m - 1, d, h, min);

export const ordenesSemilla = (): ManufacturingOrder[] => [
  new ManufacturingOrder({
    folio: 'BOL-2026-0001',
    processType: 'Bolseo',
    processLabel: 'Bolseo',
    producto: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]',
    cantidad: 20000,
    unidad: 'Millares',
    tiempoEstimadoHrs: 8,
    numeroRollos: 0,
    almacenFalla: 'SC/Scrap',
    fechaEsperada: fecha(2026, 9, 23),
    componentes: [{ clave: 'PT3413 C4235', producto: 'ROLLO TUB 20.5 370 (Impreso)', cantidad: 500, unidad: 'kgs' }],
    subproductos: [{ clave: '', producto: 'Scrap resina residual tpte (BD) de rollo impreso', cantidad: 10, unidad: 'KGS', producido: false, almacenDestino: 'SC/Scrap' }],
    produccion: [lote('R001-IV310-26', 10000, 'Millares', 'Aprobado'), lote('R002-IV310-26', 10000, 'Millares', 'Aprobado')],
    planeacion: [
      {
        centroTrabajo: 'BOLS-001',
        producto: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]',
        cantidad: 20000,
        unidad: 'Millares',
        horasAsignadas: 8,
        fechaInicio: fecha(2026, 9, 22, 8),
        fechaFin: fecha(2026, 9, 22, 16),
        operador: 'Alfonso Ortega',
      },
    ],
    sequenceCounter: 2,
  }),
  new ManufacturingOrder({
    folio: 'IMP-2026-0001',
    processType: 'Impresion',
    processLabel: 'Impresión',
    producto: 'ROLLO TUB 20.5 370 (Impreso)',
    cantidad: 500,
    unidad: 'KGS',
    tiempoEstimadoHrs: 8,
    numeroRollos: 5,
    almacenFalla: 'SC/Scrap',
    fechaEsperada: fecha(2026, 9, 23),
    originFolio: 'BOL-2026-0001',
    // Consume el producto terminado de la OF anterior (Extrusión): misma clave de PT.
    componentes: [{ clave: 'PT3413 C455', producto: 'ROLLO TUB 20.5 370 (Maestro)', cantidad: 500, unidad: 'KGS' }],
    subproductos: [{ clave: '', producto: 'Scrap resina residual tpte (BD)', cantidad: 10, unidad: 'KGS', producido: false, almacenDestino: 'SC/Scrap' }],
    produccion: produccionDeCinco(),
    planeacion: [
      planeacionDia('IMP-001', 'ROLLO TUB 20.5 370 (Impreso)', fecha(2026, 9, 19)),
      planeacionDia('IMP-002', 'ROLLO TUB 20.5 370 (Impreso)', fecha(2026, 9, 20)),
    ],
    sequenceCounter: 5,
  }),
  new ManufacturingOrder({
    folio: 'EXT-2026-0001',
    processType: 'Extrusion',
    processLabel: 'Extrusión',
    producto: 'ROLLO TUB 20.5 370 (Maestro)',
    cantidad: 500,
    unidad: 'KGS',
    tiempoEstimadoHrs: 24,
    numeroRollos: 5,
    almacenFalla: 'SC/Scrap',
    fechaEsperada: fecha(2026, 9, 23),
    originFolio: 'IMP-2026-0001',
    componentes: [
      { clave: 'PACT', producto: 'PAC TPTE', cantidad: 340, unidad: 'KGS' },
      { clave: 'GA502022', producto: 'LINEAL BUTENO', cantidad: 150, unidad: 'KGS' },
      { clave: 'MP0032', producto: 'DESLIZANTE ANTIBLOCKBC110', cantidad: 10, unidad: 'KGS' },
    ],
    subproductos: [{ clave: '', producto: 'Scrap resina residual tpte (BD)', cantidad: 10, unidad: 'KGS', producido: false, almacenDestino: 'PIM/Cuarentena' }],
    produccion: produccionDeCinco(),
    planeacion: [
      planeacionDia('COEXT-001', 'ROLLO TUB 20.5 370', fecha(2026, 9, 19)),
      planeacionDia('COEXT-002', 'ROLLO TUB 20.5 370', fecha(2026, 9, 20)),
    ],
    sequenceCounter: 5,
  }),
];

function produccionDeCinco(): ProductionLot[] {
  return [
    lote('R001-IV310-26', 100, 'KGS', 'Aprobado'),
    lote('R002-IV310-26', 95, 'KGS', 'Aprobado'),
    lote('R003-IV310-26', 105, 'KGS', 'Aprobado'),
    lote('R004-IV310-26', 100, 'KGS', 'En revisión'),
    lote('R005-IV310-26', 100, 'KGS', 'En revisión'),
  ];
}

function planeacionDia(centro: string, producto: string, dia: Date) {
  return { centroTrabajo: centro, producto, cantidad: 250, unidad: 'KGS', horasAsignadas: 0, fechaInicio: dia, fechaFin: dia, operador: 'Alejandro Varela' };
}

/** Math.Round(decimal) de .NET redondea al par en los puntos medios. */
export function redondearAlPar(x: number): number {
  const piso = Math.floor(x);
  const resto = x - piso;
  if (Math.abs(resto - 0.5) < 1e-9) return piso % 2 === 0 ? piso : piso + 1;
  return Math.round(x);
}

/**
 * Cartera de órdenes de Extrusión en piso, la que alimenta la Captura Masiva de Producción. Datos
 * fijos para que la pantalla sea reproducible. Cada orden cuelga de su propio pedido.
 */
export function carteraExtrusion(): ManufacturingOrder[] {
  const semilla: [string, string, string, number, ManufacturingOrder['state'], number[]][] = [
    ['EXT-2026-0002', 'IV311-26', 'ROLLO TUB 18.0 300 (Maestro)', 450, 'En progreso', [100, 95, 105, 100, 98, 102, 97, 103]],
    ['EXT-2026-0003', 'IV312-26', 'ROLLO TUB 22.0 400 (Maestro)', 600, 'Planeado', [120, 118, 121]],
    ['EXT-2026-0004', 'IV313-26', 'ROLLO TUB 20.5 370 (Maestro)', 500, 'En progreso', [100, 99, 101, 100, 96, 104, 100, 98, 102, 100, 97, 103]],
    ['EXT-2026-0005', 'IV314-26', 'ROLLO TUB 16.5 260 (Maestro)', 380, 'Planeado', [95, 92]],
    ['EXT-2026-0006', 'IV315-26', 'ROLLO TUB 24.0 440 (Maestro)', 720, 'En progreso', [110, 108, 112, 109, 111, 107, 113, 110, 106, 114, 110, 109, 111, 108, 112]],
    ['EXT-2026-0007', 'IV316-26', 'ROLLO TUB 20.5 370 (Maestro)', 500, 'Planeado', [100, 98, 102, 99]],
    ['EXT-2026-0008', 'IV317-26', 'ROLLO TUB 19.0 320 (Maestro)', 420, 'En progreso', [105, 103, 107, 104, 106, 102, 108, 105, 101, 109]],
    ['EXT-2026-0009', 'IV318-26', 'ROLLO TUB 21.5 390 (Maestro)', 550, 'Planeado', [92, 90, 94, 91, 93, 89]],
    ['EXT-2026-0010', 'IV319-26', 'ROLLO TUB 17.5 280 (Maestro)', 340, 'En progreso', [85, 83, 87, 84, 86, 82, 88, 85, 81, 89, 85, 84, 86]],
    ['EXT-2026-0011', 'IV320-26', 'ROLLO TUB 23.0 420 (Maestro)', 680, 'Planeado', [115, 113, 117, 114, 116]],
  ];
  return semilla.map(([folio, pedido, producto, cantidad, state, rollos], idx) => {
    const dia = (n: number) => fecha(2026, 9, 24 + n);
    return new ManufacturingOrder({
      folio,
      pedidoFolio: pedido,
      processType: 'Extrusion',
      processLabel: 'Extrusión',
      producto,
      cantidad,
      unidad: 'KGS',
      tiempoEstimadoHrs: 24,
      numeroRollos: rollos.length,
      almacenFalla: 'SC/Scrap',
      fechaEsperada: dia(idx),
      state,
      componentes: [
        { clave: 'PACT', producto: 'PAC TPTE', cantidad: redondearAlPar(cantidad * 0.68), unidad: 'KGS' },
        { clave: 'GA502022', producto: 'LINEAL BUTENO', cantidad: redondearAlPar(cantidad * 0.3), unidad: 'KGS' },
        { clave: 'MP0032', producto: 'DESLIZANTE ANTIBLOCKBC110', cantidad: redondearAlPar(cantidad * 0.02), unidad: 'KGS' },
      ],
      subproductos: [{ clave: '', producto: 'Scrap resina residual tpte (BD)', cantidad: 10, unidad: 'KGS', producido: false, almacenDestino: 'SC/Scrap' }],
      produccion: rollos.map((kg, i) => lote(`R${String(i + 1).padStart(3, '0')}-${pedido}`, kg, 'KGS', 'Aprobado')),
      planeacion: [
        {
          centroTrabajo: `COEXT-${String((idx % 3) + 1).padStart(3, '0')}`,
          producto,
          cantidad,
          unidad: 'KGS',
          horasAsignadas: 24,
          fechaInicio: dia(idx),
          fechaFin: dia(idx + 1),
          operador: 'Alejandro Varela',
        },
      ],
      sequenceCounter: rollos.length,
    });
  });
}

export const trasladoSemilla = (): InterplantTransfer =>
  new InterplantTransfer({
    folio: 'PIM/OUT/48213',
    operacion: 'Traspaso PIM a SC',
    origen: 'PIM/Stock/PT',
    destino: 'SC/Stock/MP',
    lineas: [{ clave: 'PT3413 C4235', producto: 'ROLLO TUB 20.5 370 (Maestro)', demanda: 500, entregado: 0, unidad: 'KGS', lotesSeleccionados: [] }],
  });

export const recepcionSemilla = (): Reception =>
  new Reception({
    folio: 'SC/IN/50974',
    operacion: 'Recepción en almacén',
    origen: 'SC/Stock/MP',
    destino: 'SC/Stock/PT',
    lineas: [{ clave: 'PT3413 C4235', producto: 'ROLLO TUB 20.5 370 (Maestro)', demanda: 500, entregado: 0, unidad: 'KGS', lotesSeleccionados: [] }],
  });

export const entregaSemilla = (): Delivery =>
  new Delivery({
    folio: 'SC/OUT/31688',
    operacion: 'Entrega a cliente',
    origen: 'SC/Stock/PT',
    destino: 'EMPRESA MEXICANA DE MANUFACTURA',
    cliente: 'EMPRESA MEXICANA DE MANUFACTURA',
    lineas: [{ clave: 'PT1113 C567', producto: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]', demanda: 5500, entregado: 0, unidad: 'PZA', lotesSeleccionados: [] }],
  });

export const incidenciasSemilla = (): Incidencia[] => [
  { fecha: fecha(2026, 5, 5), centroTrabajo: 'EXT-001', tipo: 'Reventon', comentarios: 'Cambio de mallas', horaInicio: '19:00', horaFin: '07:00' },
  { fecha: fecha(2026, 5, 5), centroTrabajo: 'EXT-011', tipo: 'Falta de operador', comentarios: 'No hubo operador en horario', horaInicio: '20:00', horaFin: '07:00' },
];
