import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { InventoryState } from '../inventory-state';
import { OperationalFlowState } from '../operational-flow-state';
import { StockOperationState } from '../stock-operation-state';
import { AsignarSaldoWip } from './asignar-saldo-wip';
import { OfLibre } from './of-libre';
import { OperacionesLibres } from './operaciones-libres';

describe('asignar saldo de WIP (FR-013)', () => {
  let inv: InventoryState;
  let asignar: AsignarSaldoWip;
  let folio: string;
  beforeEach(() => {
    const flow = TestBed.inject(OperationalFlowState);
    const ops = TestBed.inject(StockOperationState);
    inv = TestBed.inject(InventoryState);
    asignar = TestBed.inject(AsignarSaldoWip);
    // Recolección libre validada: 200 de PACT en PIM/WIP sin OF.
    const { op } = TestBed.inject(OperacionesLibres).crearRecoleccion('PIM', [{ clave: 'PACT', producto: 'PAC TPTE', cantidad: 200, unidad: 'KGS' }]);
    ops.asignarLote(op!, op!.lineas[0], 'PACT-2609A', 200);
    ops.validar(op!);
    folio = TestBed.inject(OfLibre).crear('Extrusion', 'PT3413 C455', 300).of!.folio;
    flow.agregarComponente(folio, 'PACT', 'PAC TPTE', 150, 'KGS');
  });

  it('nada se asigna sin la acción explícita', () => {
    expect(inv.saldoWipTotal(folio)).toBe(0);
    expect(inv.saldoSinAsignar('PIM/WIP').map(l => [l.lote, l.cantidad])).toEqual([['PACT-2609A', 200]]);
  });

  it('solo ofrece saldo sin asignar de los componentes de la OF', () => {
    expect(asignar.disponibles(folio).map(l => l.lote)).toEqual(['PACT-2609A']);
    expect(asignar.disponibles('BOL-2026-0001')).toEqual([]);
  });

  it('la acción liga la cantidad pedida y deja el resto sin asignar', () => {
    expect(asignar.asignar(folio, 'PACT-2609A', 500)).toBe('Cantidad no válida: hay 200 sin asignar.');
    expect(asignar.asignar(folio, 'PACT-2609A', 150)).toBeUndefined();
    expect(inv.saldoWipTotal(folio)).toBe(150);
    expect(inv.saldoSinAsignar('PIM/WIP').map(l => l.cantidad)).toEqual([50]);
  });
});
