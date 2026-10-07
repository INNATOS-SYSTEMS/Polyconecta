import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { OperationalFlowState } from '../operational-flow-state';
import { PedidoLibre } from './pedido-libre';

describe('pedido libre (D-53, D-74)', () => {
  let flow: OperationalFlowState;
  let libre: PedidoLibre;
  beforeEach(() => {
    flow = TestBed.inject(OperationalFlowState);
    libre = TestBed.inject(PedidoLibre);
  });

  it('nace en Borrador, sin origen ni Contpaq ID, y la semilla no cambia', () => {
    const { pedido } = libre.crear('CLIENTE NUEVO', 'OC-1');
    expect(pedido).toMatchObject({ folio: 'PV-2026-0001', cliente: 'CLIENTE NUEVO', stage: 'Borrador', contpaqId: '', libre: true });
    expect(flow.pedidos.map(p => p.folio)).toEqual(['IV310-26', 'PV-2026-0001']);
    expect(flow.pedido().folio).toBe('IV310-26');
  });

  it('exige cliente', () => {
    expect(libre.crear('  ').error).toBe('Capture el cliente.');
  });

  it('cada línea lleva cantidad, unidad del producto, precio unitario y moneda', () => {
    const folio = libre.crear('C').pedido!.folio;
    expect(libre.agregarLinea(folio, 'PT1113 C567', 0, 5, 'MXN')).toBe('La cantidad debe ser mayor que cero.');
    expect(libre.agregarLinea(folio, 'PT1113 C567', 10, 0, 'MXN')).toBe('El precio unitario debe ser mayor que cero.');
    expect(libre.agregarLinea(folio, 'PT1113 C567', 10, 5, 'EUR')).toBe('Moneda no válida: EUR.');
    expect(libre.agregarLinea(folio, 'PT1113 C567', 10, 5.5, 'USD')).toBeUndefined();
    expect(flow.pedido(folio).lineas).toEqual([{ clave: 'PT1113 C567', producto: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]', cantidad: 10, unidad: 'MIL', precioUnitario: 5.5, moneda: 'USD' }]);
  });

  it('al confirmar recibe un Contpaq ID simulado y sigue el flujo de dos firmas', () => {
    const folio = libre.crear('C').pedido!.folio;
    expect(libre.confirmar(folio)).toBe('Agregue al menos una línea antes de confirmar.');
    libre.agregarLinea(folio, 'PT1113 C567', 10, 5, 'MXN');
    expect(libre.confirmar(folio)).toBeUndefined();
    const pedido = flow.pedido(folio);
    expect(pedido.stage).toBe('Confirmado');
    expect(pedido.contpaqId).toBe('26201');
    flow.autorizar(folio);
    expect(pedido.stage).toBe('Confirmado');
    flow.autorizar(folio);
    expect(pedido.stage).toBe('Autorizado');
    expect([...pedido.firmas.keys()]).toEqual(['Comercial', 'Cobranza']);
    expect(flow.pedido().stage).toBe('Borrador');
  });

  it('"Nuevo" guarda maestro y líneas juntos (D-136)', () => {
    const { pedido } = libre.crearConLineas('CLIENTE LIBRE SA', 'OC-7', [{ clave: 'PT1113 C567', cantidad: 100, precioUnitario: 7.5, moneda: 'USD' }]);
    expect(pedido?.lineas).toEqual([expect.objectContaining({ clave: 'PT1113 C567', cantidad: 100, unidad: 'MIL', precioUnitario: 7.5, moneda: 'USD' })]);
  });

  it('si una línea no es válida, no crea el pedido', () => {
    const antes = flow.pedidos.length;
    expect(libre.crearConLineas('CLIENTE', '', [{ clave: 'PT1113 C567', cantidad: 5, precioUnitario: 0, moneda: 'MXN' }]).error).toBe('El precio unitario debe ser mayor que cero.');
    expect(libre.crearConLineas('', '', []).error).toBe('Capture el cliente.');
    expect(flow.pedidos.length).toBe(antes);
  });
});
