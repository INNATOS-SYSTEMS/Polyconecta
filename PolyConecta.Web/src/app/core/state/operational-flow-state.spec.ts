import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { InventoryState } from './inventory-state';
import { OperationalFlowState } from './operational-flow-state';

describe('OperationalFlowState', () => {
  let flow: OperationalFlowState;
  beforeEach(() => (flow = TestBed.inject(OperationalFlowState)));

  it('la semilla trae el pedido, 13 OF y los tres documentos de logística', () => {
    expect(flow.pedido().folio).toBe('IV310-26');
    expect(flow.pedido().contpaqId).toBe('26200');
    expect(flow.manufacturingOrders.map(o => o.folio).slice(0, 4)).toEqual(['BOL-2026-0001', 'IMP-2026-0001', 'EXT-2026-0001', 'EXT-2026-0002']);
    expect(flow.manufacturingOrders).toHaveLength(13);
    expect(flow.getOrder('EXT-2026-0006')!.componentes.map(c => c.cantidad)).toEqual([490, 216, 14]);
    expect([flow.traslado().state, flow.recepcion().state, flow.entrega().state]).toEqual(['Borrador', 'Borrador', 'Borrador']);
  });

  it('autorizar solo en Confirmado y con dos firmas pasa a Autorizado', () => {
    flow.autorizar();
    expect(flow.firmasRecogidas()).toBe(0);

    flow.setOrderStage('Confirmado');
    flow.autorizar();
    expect(flow.currentOrderStage).toBe('Confirmado');
    expect(flow.pedido().firmas.get('Comercial')).toBe('Celia Villarreal');
    expect(flow.firmaPendiente()).toBe('Cobranza');

    flow.autorizar();
    expect(flow.currentOrderStage).toBe('Autorizado');
    expect(flow.pedido().firmas.get('Cobranza')).toBe('Crédito y Cobranza');
    expect(flow.puedeFirmar()).toBe(false);
  });

  it('revocar borra las firmas, libera las reservas y regresa a Confirmado', () => {
    const inv = TestBed.inject(InventoryState);
    flow.setOrderStage('Confirmado');
    flow.autorizar();
    flow.autorizar();
    inv.reservar('IV310-26-C01', 'IV310-26');

    flow.revocarFirmas();

    expect(flow.currentOrderStage).toBe('Confirmado');
    expect(flow.firmasRecogidas()).toBe(0);
    expect(inv.lotes.find(l => l.lote === 'IV310-26-C01')!.estado).toBe('Libre');
  });

  it('rechazar marca Rechazado, agrega .S una sola vez y no cuenta en lo producido', () => {
    const of = flow.getOrder('IMP-2026-0001')!;
    expect(of.producidoTotal).toBe(500);
    const lote = of.produccion[3];
    flow.rechazarLote(lote);
    flow.rechazarLote(lote);
    expect(lote.lote).toBe('R004-IV310-26.S');
    expect(lote.estado).toBe('Rechazado');
    expect(of.producidoTotal).toBe(400);
  });

  it('el cierre se bloquea con lotes en revisión (hard-stop) y el pedido pasa a Hecho al cerrar todas', () => {
    flow.cerrarProduccion('IMP-2026-0001');
    expect(flow.getOrder('IMP-2026-0001')!.state).toBe('Borrador');

    for (const l of flow.getOrder('IMP-2026-0001')!.produccion) flow.aprobarLote(l);
    flow.cerrarProduccion('IMP-2026-0001');
    expect(flow.getOrder('IMP-2026-0001')!.state).toBe('Hecho');
    expect(flow.currentOrderStage).toBe('Borrador');

    for (const of of flow.manufacturingOrders) {
      of.produccion.forEach(l => flow.aprobarLote(l));
      flow.cerrarProduccion(of.folio);
    }
    expect(flow.currentOrderStage).toBe('Hecho');
  });

  it('planear exige componentes y libera la recolección', () => {
    flow.planear('EXT-2026-0001');
    expect(flow.getOrder('EXT-2026-0001')!.state).toBe('Planeado');
    const of = flow.getOrder('BOL-2026-0001')!;
    of.componentes.length = 0;
    flow.planear('BOL-2026-0001');
    expect(of.state).toBe('Borrador');
  });

  it('agregar planeación pone en progreso la OF planeada y el pedido autorizado', () => {
    flow.planear('EXT-2026-0001');
    flow.setOrderStage('Autorizado');
    flow.agregarPlaneacion('EXT-2026-0001', 'COEXT-003', 'X', 10, 'KGS', 2, new Date(), new Date(), 'Op');
    expect(flow.getOrder('EXT-2026-0001')!.state).toBe('En progreso');
    expect(flow.currentOrderStage).toBe('En progreso');
  });

  it('el pesaje de rollo crea el siguiente lote en revisión con peso neto', () => {
    flow.registrarPesajeRollo('EXT-2026-0001', 105.4, 5.4);
    expect(flow.getOrder('EXT-2026-0001')!.produccion.at(-1)).toEqual({ lote: 'R006-IV310-26', real: 100, unidad: 'KGS', estado: 'En revisión' });
  });

  it('la línea del pedido precarga el precio de la lista', () => {
    flow.agregarLineaPedido('pt3413 c460', '', 10, 'KGS');
    expect(flow.pedidoLineas.at(-1)).toMatchObject({ producto: 'pt3413 c460', precioUnitario: 49.5 });
    flow.agregarLineaPedido('SIN-PRECIO', 'X', 1, 'PZA');
    expect(flow.pedidoLineas.at(-1)!.precioUnitario).toBe(0);
  });

  it('la logística no cierra sin lotes; con parcialidad avisa y registra lo entregado', () => {
    const t = flow.traslado();
    for (let i = 0; i < 3; i++) flow.validarTraslado();
    expect(t.state).toBe('Listo');
    flow.validarTraslado();
    expect(t.error).toBe('No se puede validar: no hay lotes capturados. Selecciona al menos un lote antes de cerrar el traslado.');
    expect(t.state).toBe('Listo');

    t.lineas[0].lotesSeleccionados.push('R001-IV310-26', 'R002-IV310-26');
    flow.validarTraslado();
    expect(t.state).toBe('Hecho');
    expect(t.lineas[0].entregado).toBe(195);
    expect(t.warning).toBe('Traslado parcial: 195.0 de 500.0 KGS.');
  });

  it('la entrega toma los lotes aprobados de bolseo', () => {
    const e = flow.entrega();
    flow.validarEntrega();
    flow.validarEntrega();
    e.lineas[0].lotesSeleccionados.push('R001-IV310-26');
    flow.validarEntrega();
    expect(e.state).toBe('Hecho');
    expect(e.warning).toBeUndefined();
    expect(e.lineas[0].entregado).toBe(10000);
  });
});
