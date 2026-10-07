import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { OperationalFlowState } from '../operational-flow-state';
import { StockOperationState } from '../stock-operation-state';
import { OfLibre, nombreLoteLibre } from './of-libre';

describe('OF libre (FR-012, D-54)', () => {
  let flow: OperationalFlowState;
  let ofs: OfLibre;
  let ops: StockOperationState;
  beforeEach(() => {
    flow = TestBed.inject(OperationalFlowState);
    ofs = TestBed.inject(OfLibre);
    ops = TestBed.inject(StockOperationState);
  });

  it('nace sin pedido, con el folio siguiente de su proceso y la unidad del producto', () => {
    const { of } = ofs.crear('Bolseo', 'PT1113 C567', 1000);
    expect(of).toMatchObject({ folio: 'BOL-2026-0002', pedidoFolio: '', libre: true, state: 'Borrador', unidad: 'MIL', processLabel: 'Bolseo' });
    expect(ofs.crear('Bolseo', 'NO-EXISTE', 1).error).toBe('Elija un producto del catálogo de CONTPAQi.');
    expect(ofs.crear('Bolseo', 'PT1113 C567', 0).error).toBe('La cantidad debe ser mayor que cero.');
  });

  it('sus lotes usan el folio de la OF raíz con / cambiado por -', () => {
    const { of } = ofs.crear('Bolseo', 'PT1113 C567', 1000);
    expect(ofs.siguienteLote(of!.folio)).toBe('R001-BOL-2026-0002');
    flow.agregarLoteProduccion(of!.folio, ofs.siguienteLote(of!.folio), 10, '');
    expect(ofs.siguienteLote(of!.folio)).toBe('R002-BOL-2026-0002');
    const hija = ofs.crear('Extrusion', 'PT3413 C455', 300).of!;
    hija.originFolio = of!.folio;
    expect(nombreLoteLibre(hija, flow.manufacturingOrders, 7)).toBe('R007-BOL-2026-0002');
  });

  it('al confirmarla genera su recolección y su control igual que una ligada', () => {
    const of = ofs.crear('Extrusion', 'PT3413 C455', 300).of!;
    flow.agregarComponente(of.folio, 'PACT', 'PAC TPTE', 120, 'KGS');
    const recoleccion = ops.deOf(of.folio)[0];
    expect(recoleccion.state).toBe('Borrador');
    expect(recoleccion.origen).toBe('PIM/Stock/MP');
    flow.planear(of.folio);
    expect(of.state).toBe('Planeado');
    expect(recoleccion.state).toBe('En espera');
    flow.agregarLoteProduccion(of.folio, ofs.siguienteLote(of.folio), 100, '');
    expect(of.folio).toBe('EXT-2026-0012');
    expect(of.produccion[0]).toMatchObject({ lote: 'R001-EXT-2026-0012', estado: 'En revisión', unidad: 'KGS' });
  });

  it('"Nuevo" guarda el maestro con sus componentes y subproductos (D-136)', () => {
    const { of } = ofs.crearConLineas('Bolseo', 'PT1113 C567', 1000, [{ clave: 'PT3413 C4235', cantidad: 50 }], [{ clave: 'PT3413 C4235', cantidad: 5 }]);
    expect(of?.componentes).toEqual([expect.objectContaining({ clave: 'PT3413 C4235', cantidad: 50 })]);
    expect(of?.subproductos).toEqual([expect.objectContaining({ clave: 'PT3413 C4235', cantidad: 5, producido: false })]);
  });

  it('si una línea no es válida, no crea la OF', () => {
    const antes = flow.manufacturingOrders.length;
    expect(ofs.crearConLineas('Bolseo', 'PT1113 C567', 1000, [{ clave: 'NO-EXISTE', cantidad: 5 }], []).error).toBeDefined();
    expect(flow.manufacturingOrders.length).toBe(antes);
  });
});
