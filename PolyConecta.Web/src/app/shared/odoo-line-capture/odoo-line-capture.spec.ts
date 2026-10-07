import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { OdooLineCapture } from './odoo-line-capture';

describe('OdooLineCapture', () => {
  it('resuelve la clave contra el catálogo y precarga la unidad de CONTPAQi', () => {
    const fixture = TestBed.createComponent(OdooLineCapture);
    fixture.componentRef.setInput('catalogo', [
      { clave: 'PT1113 C567', nombre: 'BOLSA MEDIANA', clasificacion: 'Bolsa', unidad: 'PZA' },
    ]);
    fixture.detectChanges();
    const clave = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    clave.value = 'pt1113 c567';
    clave.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.componentInstance.draft()).toMatchObject({ clave: 'PT1113 C567', producto: 'BOLSA MEDIANA', unidad: 'PZA' });
    expect(clave.value).toBe('PT1113 C567 — BOLSA MEDIANA');
  });

  it('no deja agregar sin cantidad', () => {
    const fixture = TestBed.createComponent(OdooLineCapture);
    fixture.componentInstance.draft.set({ clave: 'X', producto: '', cantidad: 0, unidad: '' });
    fixture.detectChanges();
    expect((fixture.nativeElement.querySelector('button.btn-primary') as HTMLButtonElement).disabled).toBe(true);
  });
});
