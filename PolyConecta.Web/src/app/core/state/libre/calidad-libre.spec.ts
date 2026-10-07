import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { OperationalFlowState } from '../operational-flow-state';
import { CalidadLibre } from './calidad-libre';

describe('control de calidad libre', () => {
  let flow: OperationalFlowState;
  let qc: CalidadLibre;
  beforeEach(() => {
    flow = TestBed.inject(OperationalFlowState);
    qc = TestBed.inject(CalidadLibre);
    flow.agregarLoteProduccion('BOL-2026-0001', 'R001-BOL', 100, '');
    flow.agregarLoteProduccion('BOL-2026-0001', 'R002-BOL', 100, '');
  });

  it('distingue lotes con el mismo nombre en OF distintas', () => {
    const repetidos = qc.elegibles().filter(e => e.lote.lote === 'R004-IV310-26').map(e => e.ofFolio);
    expect(repetidos.length).toBeGreaterThan(1);
    const control = qc.crear([{ ofFolio: repetidos[1], lote: 'R004-IV310-26' }]).control!;
    expect(control.lotes[0].ofFolio).toBe(repetidos[1]);
  });

  it('solo se crea sobre lotes existentes en revisión', () => {
    expect(qc.crear([]).error).toBe('Elija al menos un lote existente.');
    expect(qc.crear([{ ofFolio: 'BOL-2026-0001', lote: 'NO-EXISTE' }]).error).toBe('El lote NO-EXISTE de BOL-2026-0001 no existe o no está en revisión.');
    expect(qc.crear([{ ofFolio: 'BOL-2026-0001', lote: 'R001-BOL' }]).control!.folio).toBe('QC-2026-0001');
  });

  it('aprobar y rechazar tienen los mismos efectos que en el control ligado', () => {
    const control = qc.crear([{ ofFolio: 'BOL-2026-0001', lote: 'R001-BOL' }, { ofFolio: 'BOL-2026-0001', lote: 'R002-BOL' }]).control!;
    qc.aprobar(control.lotes[0]);
    qc.rechazar(control.lotes[1]);
    const of = flow.getOrder('BOL-2026-0001')!;
    expect(of.produccion.filter(l => l.lote.includes('-BOL')).map(l => [l.lote, l.estado])).toEqual([['R001-BOL', 'Aprobado'], ['R002-BOL.S', 'Rechazado']]);
    expect(control.lotes[1].lote.lote).toBe('R002-BOL.S');
    expect(qc.estado(control)).toBe('Parcial');
  });

  it('con el lote aprobado en el control libre, la OF deja de estar bloqueada por calidad', () => {
    const control = qc.crear([{ ofFolio: 'BOL-2026-0001', lote: 'R001-BOL' }, { ofFolio: 'BOL-2026-0001', lote: 'R002-BOL' }]).control!;
    control.lotes.forEach(l => qc.aprobar(l));
    expect(qc.estado(control)).toBe('Aprobado');
    expect(control.lotes.every(l => l.lote.estado === 'Aprobado')).toBe(true);
  });
});
