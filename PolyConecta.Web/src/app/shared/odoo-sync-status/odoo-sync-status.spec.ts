import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';
import { AvisosService } from '../odoo-dialog/avisos';
import { OdooSyncStatus } from './odoo-sync-status';

describe('OdooSyncStatus (CT-15)', () => {
  it('es solo un ícono con el estado como nombre accesible', () => {
    const f = TestBed.createComponent(OdooSyncStatus);
    f.componentRef.setInput('estado', 'Error');
    f.detectChanges();
    const boton: HTMLButtonElement = f.nativeElement.querySelector('[data-sync]');
    expect(boton.textContent?.trim()).toBe('');
    expect(boton.getAttribute('aria-label')).toBe('Sincronización con CONTPAQi: Error al enviar a CONTPAQi');
    expect(boton.classList).toContain('o_sync_Error');
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
