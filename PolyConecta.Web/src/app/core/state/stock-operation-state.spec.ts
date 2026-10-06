import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { InventoryState } from './inventory-state';
import { OperationalFlowState } from './operational-flow-state';
import { StockOperationState } from './stock-operation-state';

describe('StockOperationState', () => {
  let ops: StockOperationState;
  let inv: InventoryState;
  beforeEach(() => {
    TestBed.inject(OperationalFlowState); // crea las recolecciones de la semilla
    ops = TestBed.inject(StockOperationState);
    inv = TestBed.inject(InventoryState);
  });

  const recoleccionExt = () => ops.deOf('EXT-2026-0001').find(o => !o.tipo.esDevolucion)!;

  it('cada OF con componentes nace con su recolección en Borrador, con folios consecutivos', () => {
    expect(ops.operaciones.map(o => o.folio).slice(0, 4)).toEqual(['SC/OUT/48214', 'SC/OUT/48215', 'PIM/OUT/48216', 'PIM/OUT/48217']);
    expect(recoleccionExt().state).toBe('Borrador');
    expect(recoleccionExt().lineas.map(l => [l.clave, l.solicitado])).toEqual([['PACT', 340], ['GA502022', 150], ['MP0032', 10]]);
  });

  it('una validación parcial genera backorder con lo pendiente', () => {
    const op = recoleccionExt();
    ops.confirmarRecoleccion('EXT-2026-0001');
    const pact = op.lineas[0];
    ops.asignarLote(op, pact, 'PACT-2609A', 300);
    expect(op.state).toBe('Listo');

    const { backorder, error } = ops.validar(op);

    expect(error).toBeUndefined();
    expect(op.state).toBe('Hecho');
    expect(op.contpaqId).toBe(`TR-${op.folio.split('/').at(-1)}`);
    expect(backorder!.backorderDe).toBe(op.folio);
    expect(backorder!.state).toBe('En espera');
    expect(backorder!.lineas.map(l => [l.clave, l.solicitado])).toEqual([['PACT', 40], ['GA502022', 150], ['MP0032', 10]]);
    expect(op.warning).toBe(`Entrega parcial. Se generó el backorder ${backorder!.folio} por el remanente.`);
    expect(inv.saldoWipTotal('EXT-2026-0001')).toBe(300);
  });

  it('declarar más de lo pendiente da el error exacto del prototipo', () => {
    const op = recoleccionExt();
    ops.asignarLote(op, op.lineas[2], 'MP32-2609', 12.5);
    expect(ops.validar(op).error).toBe('MP0032: lo declarado (12.5) excede lo pendiente (10.0).');
  });

  it('declarar más de lo que tiene el lote da el error exacto y no mueve nada', () => {
    inv.moverAWip('GA-2609', 200, InventoryState.WipPim, 'OTRA');
    const op = recoleccionExt();
    ops.asignarLote(op, op.lineas[0], 'PACT-2609A', 340);
    ops.asignarLote(op, op.lineas[1], 'GA-2609', 150);
    expect(ops.validar(op).error).toBe('Lote GA-2609: se declararon 150.0 pero solo hay 20.0 en PIM/Stock/MP.');
    expect(inv.saldoWipTotal('EXT-2026-0001')).toBe(0);
    expect(op.state).toBe('Listo');
  });

  it('sin lotes declarados no valida', () => {
    expect(ops.validar(recoleccionExt()).error).toBe('Declare al menos un lote antes de validar.');
  });

  it('puedeCerrarOf exige WIP en cero y recolecciones validadas', () => {
    const op = recoleccionExt();
    expect(ops.puedeCerrarOf('EXT-2026-0001').motivo).toBe(`Hay 1 documento(s) de recolección sin validar: ${op.folio}.`);
    ops.asignarLote(op, op.lineas[0], 'PACT-2609A', 340);
    ops.asignarLote(op, op.lineas[1], 'GA-2608', 150);
    ops.asignarLote(op, op.lineas[2], 'MP32-2609', 10);
    ops.validar(op);
    expect(ops.puedeCerrarOf('EXT-2026-0001').motivo).toBe('Quedan 500.0 en WIP sin declarar. Devuelva a almacén o declare como scrap antes de cerrar.');
  });

  it('la devolución toma el saldo de WIP agrupado por producto', () => {
    const op = recoleccionExt();
    ops.asignarLote(op, op.lineas[0], 'PACT-2609A', 340);
    ops.validar(op);
    const dev = ops.emitirDevolucion('EXT-2026-0001');
    expect(dev.tipo.esDevolucion).toBe(true);
    expect(dev.folio).toMatch(/^PIM\/IN\/\d+$/);
    expect(dev.lineas.map(l => [l.clave, l.solicitado, l.producto])).toEqual([['PACT', 340, 'PAC TPTE']]);
  });

  it('comprobar disponibilidad informa faltantes sin mover nada', () => {
    const op = recoleccionExt();
    ops.comprobarDisponibilidad(op);
    expect(op.warning).toBe('Disponibilidad completa en PIM/Stock/MP.');
    inv.moverAWip('GA-2608', 400, InventoryState.WipPim, 'OTRA');
    inv.moverAWip('GA-2609', 100, InventoryState.WipPim, 'OTRA');
    ops.comprobarDisponibilidad(op);
    expect(op.warning).toBe('Disponibilidad parcial — GA502022: hay 120.0 de 150.0 KGS. Puede validar por parcialidades y dejar backorder.');
  });
});
