import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { OdooSmartButtons } from './odoo-smart-buttons';

describe('OdooSmartButtons', () => {
  it('emite la ruta del botón pulsado', () => {
    const fixture = TestBed.createComponent(OdooSmartButtons);
    fixture.componentRef.setInput('buttons', [
      { label: 'Fabricación', countBadge: 3, iconClass: 'bi bi-gear', targetRoute: '/fabricacion' },
    ]);
    let ruta = '';
    fixture.componentInstance.smartNavigate.subscribe(r => (ruta = r));
    fixture.detectChanges();

    const boton = fixture.nativeElement.querySelector('.o_smart_button') as HTMLElement;
    expect(boton.textContent).toContain('Fabricación');
    expect(boton.textContent).toContain('3');
    boton.click();
    expect(ruta).toBe('/fabricacion');
  });
});
