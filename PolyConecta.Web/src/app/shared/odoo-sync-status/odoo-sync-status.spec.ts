import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';
import { AvisosService } from '../odoo-dialog/avisos';
import { OdooSyncStatus } from './odoo-sync-status';

describe('OdooSyncStatus (CT-15)', () => {
  it('muestra el folio al confirmar y el reintento solo en error y solo si se permite', () => {
    const f = TestBed.createComponent(OdooSyncStatus);
    f.componentRef.setInput('estado', 'Confirmado');
    f.componentRef.setInput('folio', 'F-1');
    f.detectChanges();
    expect(f.nativeElement.textContent).toContain('En CONTPAQi · F-1');
    expect(f.nativeElement.querySelector('[data-sync-reintentar]')).toBeNull();

    f.componentRef.setInput('estado', 'Error');
    f.componentRef.setInput('error', { codigo: 'SDK_ERROR', mensaje: 'Falló.' });
    f.detectChanges();
    expect(f.nativeElement.querySelector('[data-sync-error]').textContent).toContain('SDK_ERROR');
    expect(f.nativeElement.querySelector('[data-sync-reintentar]')).toBeNull();

    f.componentRef.setInput('puedeReintentar', true);
    f.detectChanges();
    expect(f.nativeElement.querySelector('[data-sync-reintentar]')).not.toBeNull();
  });
});

describe('AvisosService', () => {
  it('cierra solos los de éxito y aviso; el de error se queda', () => {
    vi.useFakeTimers();
    const s = TestBed.inject(AvisosService);
    s.exito('ok');
    s.error('mal');
    expect(s.avisos().map(a => a.tipo)).toEqual(['exito', 'error']);
    vi.advanceTimersByTime(AvisosService.DURACION_MS);
    expect(s.avisos().map(a => a.tipo)).toEqual(['error']);
    s.cerrar(s.avisos()[0].id);
    expect(s.avisos()).toEqual([]);
    vi.useRealTimers();
  });
});
