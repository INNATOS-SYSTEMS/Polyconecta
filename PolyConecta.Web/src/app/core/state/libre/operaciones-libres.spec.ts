import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { InventoryState } from '../inventory-state';
import { OperationalFlowState } from '../operational-flow-state';
import { StockOperationState } from '../stock-operation-state';
import { OperacionesLibres } from './operaciones-libres';

describe('operaciones libres (FR-012)', () => {
  let flow: OperationalFlowState;
  let ops: StockOperationState;
  let inv: InventoryState;
  let libres: OperacionesLibres;
  beforeEach(() => {
    flow = TestBed.inject(OperationalFlowState);
    ops = TestBed.inject(StockOperationState);
    inv = TestBed.inject(InventoryState);
    libres = TestBed.inject(OperacionesLibres);
  });

  const validarHastaHecho = (validar: () => void, veces: number) => {
    for (let i = 0; i < veces; i++) validar();
  };

  it('recolección libre: MP → WIP sin OF y el saldo queda sin asignar (D-55)', () => {
    expect(libres.crearRecoleccion('PIM', []).error).toBe('Agregue al menos una línea.');
    const { op } = libres.crearRecoleccion('PIM', [{ clave: 'GA502022', producto: 'LINEAL BUTENO', cantidad: 100, unidad: 'KGS' }]);
    expect(op).toMatchObject({ ofFolio: '', libre: true, origen: 'PIM/Stock/MP', destino: 'PIM/WIP', state: 'En espera' });
    ops.asignarLote(op!, op!.lineas[0], 'GA-2608', 100);
    expect(ops.validar(op!).error).toBeUndefined();
    expect(inv.saldoSinAsignar('PIM/WIP').map(l => [l.lote, l.cantidad])).toEqual([['GA-2608', 100]]);
    expect(inv.disponibleDeLote('GA-2608', 'PIM/Stock/MP')).toBe(300);
  });

  it('devolución REC-RET libre: regresa saldo sin asignar de WIP con la cantidad capturada', () => {
    const rec = libres.crearRecoleccion('PIM', [{ clave: 'GA502022', producto: 'LINEAL BUTENO', cantidad: 100, unidad: 'KGS' }]).op!;
    ops.asignarLote(rec, rec.lineas[0], 'GA-2608', 100);
    ops.validar(rec);
    const dev = libres.crearDevolucion('PIM', [{ clave: 'GA502022', producto: 'LINEAL BUTENO', cantidad: 40, unidad: 'KGS' }]).op!;
    expect(dev.tipo.codigo).toBe('PIM-REC-RET');
    expect(dev.folio.startsWith('PIM/IN/')).toBe(true);
    ops.asignarLote(dev, dev.lineas[0], 'GA-2608', 40);
    expect(ops.validar(dev).error).toBeUndefined();
    expect(inv.saldoSinAsignar('PIM/WIP').map(l => l.cantidad)).toEqual([60]);
    expect(inv.disponibleDeLote('GA-2608', 'PIM/Stock/MP')).toBe(340);
  });

  it('traslado libre: solo lotes liberados por Calidad; los de cuarentena y reservados no se ofrecen', () => {
    const ofrecidos = libres.lotesLiberados('PIM').map(l => l.lote);
    expect(ofrecidos).toContain('R004-IV310-26');
    expect(ofrecidos).not.toContain('R003-IV310-26.S');
    expect(ofrecidos).not.toContain('R006-IV310-26');
    expect(libres.crearTraslado('PIM', ['R003-IV310-26.S']).error).toBe('El lote R003-IV310-26.S no está liberado por Calidad en PIM.');
    expect(libres.crearTraslado('PIM', []).error).toBe('Elija al menos un lote.');
  });

  it('traslado libre validado deja los lotes en tránsito y la recepción libre solo recibe lotes en TRANS/* (D-56)', () => {
    expect(libres.crearRecepcion('SC', ['R004-IV310-26']).error).toBe('El lote R004-IV310-26 no está en tránsito hacia SC.');
    const traslado = libres.crearTraslado('PIM', ['R004-IV310-26', 'R005-IV310-26']).doc!;
    expect(traslado.lineas).toEqual([{ clave: 'PT3413 C455', producto: 'ROLLO TUB 20.5 370 (Maestro)', demanda: 200, entregado: 0, unidad: 'KGS', lotesSeleccionados: ['R004-IV310-26', 'R005-IV310-26'] }]);
    validarHastaHecho(() => flow.validarTraslado(traslado.folio), 4);
    expect(traslado.state).toBe('Hecho');
    expect(traslado.contpaqId).toBe(`TR-${traslado.folio.split('/').at(-1)}`);
    expect(libres.lotesEnTransito('SC').map(l => l.lote)).toEqual(['R004-IV310-26', 'R005-IV310-26']);

    const recepcion = libres.crearRecepcion('SC', ['R004-IV310-26']).doc!;
    validarHastaHecho(() => flow.validarRecepcion(recepcion.folio), 3);
    expect(recepcion.state).toBe('Hecho');
    expect(inv.disponibleDeLote('R004-IV310-26', 'SC/Stock/MP')).toBe(100);
    expect(libres.lotesEnTransito('SC').map(l => l.lote)).toEqual(['R005-IV310-26']);
  });

  it('entrega libre: cliente y lotes liberados, sin pedido; al validar salen del inventario', () => {
    expect(libres.crearEntrega('SC', '', ['IV310-26-C01']).error).toBe('Capture el cliente.');
    const entrega = libres.crearEntrega('SC', 'CLIENTE X', ['IV310-26-C01']).doc!;
    expect(entrega).toMatchObject({ cliente: 'CLIENTE X', destino: 'CLIENTE X', libre: true });
    expect(entrega.lineas[0]).toMatchObject({ demanda: 2000, unidad: 'MIL' });
    validarHastaHecho(() => flow.validarEntrega(entrega.folio), 3);
    expect(entrega.state).toBe('Hecho');
    expect(entrega.contpaqId).toBe(`REM-${entrega.folio.split('/').at(-1)}`);
    expect(inv.lotes.some(l => l.lote === 'IV310-26-C01')).toBe(false);
  });

  it('los documentos semilla no cambian de comportamiento', () => {
    expect(flow.traslado().libre).toBe(false);
    expect(flow.traslados).toHaveLength(1);
  });
});
