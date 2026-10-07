import { LotBalance, ProductRef } from '../models/inventario';

/** Semilla de Services/InventoryState.cs, valor por valor (FR-007). */
export const ALMACEN_MATERIA_PRIMA = 'PIM/Stock/MP';
export const WIP_PIM = 'PIM/WIP';
export const WIP_STC = 'SC/WIP';

export const catalogoSemilla = (): ProductRef[] => [
  { clave: 'PT1113 C567', nombre: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]', clasificacion: 'Bolsa', unidad: 'MIL' },
  { clave: 'PT1113 C580', nombre: 'BOLSA CHICA 38X60 C.380 BOL-002', clasificacion: 'Bolsa', unidad: 'MIL' },
  { clave: 'PT3413 C4235', nombre: 'ROLLO TUB 20.5 370 (Impreso)', clasificacion: 'RolloImpreso', unidad: 'KGS' },
  { clave: 'PT3413 C455', nombre: 'ROLLO TUB 20.5 370 (Maestro)', clasificacion: 'RolloMaestro', unidad: 'KGS' },
  { clave: 'PT3413 C460', nombre: 'ROLLO TUB 20.5 380 (Maestro)', clasificacion: 'RolloMaestro', unidad: 'KGS' },
  { clave: 'PT3413 C470', nombre: 'ROLLO TUB 22.0 370 (Liso)', clasificacion: 'RolloLiso', unidad: 'KGS' },
  { clave: 'PACT', nombre: 'PAC TPTE', clasificacion: 'MateriaPrima', unidad: 'KGS' },
  { clave: 'GA502022', nombre: 'LINEAL BUTENO', clasificacion: 'MateriaPrima', unidad: 'KGS' },
  { clave: 'MP0032', nombre: 'DESLIZANTE ANTIBLOCKBC110', clasificacion: 'MateriaPrima', unidad: 'KGS' },
  { clave: 'SCR-BD', nombre: 'Scrap resina residual tpte (BD)', clasificacion: 'Scrap', unidad: 'KGS' },
];

const lote = (l: string, clave: string, ubicacion: string, cantidad: number, extra: Partial<LotBalance> = {}): LotBalance => ({
  lote: l,
  clave,
  ubicacion,
  cantidad,
  estado: 'Libre',
  ...extra,
});

export const lotesSemilla = (): LotBalance[] => [
  // Materia prima — PIM
  lote('PACT-2609A', 'PACT', ALMACEN_MATERIA_PRIMA, 1200),
  lote('PACT-2609B', 'PACT', ALMACEN_MATERIA_PRIMA, 650.5),
  lote('GA-2608', 'GA502022', ALMACEN_MATERIA_PRIMA, 400),
  lote('GA-2609', 'GA502022', ALMACEN_MATERIA_PRIMA, 220),
  lote('MP32-2609', 'MP0032', ALMACEN_MATERIA_PRIMA, 45.75),
  // Rollos maestros — PIM. R006 ya comprometido por otro pedido: demuestra físico != disponible.
  lote('R004-IV310-26', 'PT3413 C455', 'PIM/Stock/PT', 100),
  lote('R005-IV310-26', 'PT3413 C455', 'PIM/Stock/PT', 100),
  lote('R006-IV310-26', 'PT3413 C455', 'PIM/Stock/PT', 110, { estado: 'Reservado', comprometidoPor: 'IV308-26' }),
  // Rollo de otra especificación: candidato a sustitución, decisión de AC.
  lote('R011-IV295-26', 'PT3413 C460', 'PIM/Stock/PT', 145),
  lote('R012-IV295-26', 'PT3413 C460', 'PIM/Stock/PT', 95),
  lote('R003-IV310-26.S', 'PT3413 C455', 'PIM/Stock/Cuarentena', 105, { estado: 'Cuarentena' }),
  // Rollo liso
  lote('R021-IV288-26', 'PT3413 C470', 'PIM/Stock/PT', 260),
  // Rollos impresos — SC
  lote('R001-IV310-26', 'PT3413 C4235', 'SC/Stock/MP', 100),
  lote('R002-IV310-26', 'PT3413 C4235', 'SC/Stock/MP', 80),
  // Producto terminado — SC
  lote('IV310-26-C01', 'PT1113 C567', 'SC/Stock/PT', 2000),
  lote('IV310-26-C02', 'PT1113 C567', 'SC/Stock/PT', 1000),
  lote('IV302-26-C07', 'PT1113 C580', 'SC/Stock/PT', 4500),
];
