import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { InventoryState } from './inventory-state';

describe('InventoryState', () => {
  let inv: InventoryState;
  beforeEach(() => (inv = TestBed.inject(InventoryState)));

  it('la disponibilidad excluye cuarentena y lo comprometido; el físico no', () => {
    // PT3413 C455: R004 100 + R005 100 libres, R006 110 reservado, R003.S 105 en cuarentena.
    expect(inv.fisico('PT3413 C455')).toBe(415);
    expect(inv.reservado('PT3413 C455')).toBe(110);
    expect(inv.disponible('PT3413 C455')).toBe(200);
    expect(inv.lotesDisponibles('PT3413 C455').map(l => l.lote)).toEqual(['R004-IV310-26', 'R005-IV310-26']);
  });

  it('esVendible excluye Cuarentena y Scrap', () => {
    expect(InventoryState.esVendible('PIM/Stock/Cuarentena')).toBe(false);
    expect(InventoryState.esVendible('SC/Scrap')).toBe(false);
    expect(InventoryState.esVendible('SC/Stock/PT')).toBe(true);
  });

  it('reservar es por lote y parte el lote cuando la cantidad es menor', () => {
    expect(inv.reservar('PACT-2609A', 'IV310-26', 200)).toBe(true);
    const partes = inv.lotes.filter(l => l.lote === 'PACT-2609A');
    expect(partes.map(l => [l.cantidad, l.estado])).toEqual([[200, 'Reservado'], [1000, 'Libre']]);
    expect(inv.reservar('R006-IV310-26', 'IV310-26')).toBe(false);

    inv.liberarReservasDe('IV310-26');
    expect(inv.disponible('PACT', InventoryState.AlmacenMateriaPrima)).toBe(1850.5);
  });

  it('mover a WIP y devolver conserva el total y lleva el saldo por OF', () => {
    const total = inv.fisico('PACT');
    expect(inv.moverAWip('PACT-2609A', 300, InventoryState.WipPim, 'EXT-2026-0001')).toBe(true);
    expect(inv.moverAWip('PACT-2609A', 50, InventoryState.WipPim, 'EXT-2026-0001')).toBe(true);
    expect(inv.saldoWipTotal('EXT-2026-0001')).toBe(350);
    expect(inv.enWip('PACT')).toBe(350);
    expect(inv.disponible('PACT')).toBe(total - 350);

    expect(inv.devolverDeWip('PACT-2609A', 400, InventoryState.WipPim, 'EXT-2026-0001', InventoryState.AlmacenMateriaPrima)).toBe(false);
    expect(inv.devolverDeWip('PACT-2609A', 350, InventoryState.WipPim, 'EXT-2026-0001', InventoryState.AlmacenMateriaPrima)).toBe(true);
    expect(inv.saldoWip('EXT-2026-0001')).toEqual([]);
    expect(inv.fisico('PACT')).toBe(total);
  });

  it('no mueve más de lo que tiene el lote ni cantidades no positivas', () => {
    expect(inv.moverAWip('GA-2609', 221, InventoryState.WipPim, 'X')).toBe(false);
    expect(inv.moverAWip('GA-2609', 0, InventoryState.WipPim, 'X')).toBe(false);
  });

  it('existencias da un renglón por lote con su producto', () => {
    const e = inv.existencias();
    expect(e).toHaveLength(17);
    expect(e.find(q => q.lote === 'IV302-26-C07')).toMatchObject({ ubicacion: 'SC/Stock/PT', cantidad: 4500, producto: { unidad: 'MIL' } });
  });
});
