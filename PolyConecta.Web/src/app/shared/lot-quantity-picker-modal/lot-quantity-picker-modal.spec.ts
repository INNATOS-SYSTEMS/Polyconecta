import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { LotQuantityPickerModal } from './lot-quantity-picker-modal';

describe('LotQuantityPickerModal', () => {
  it('propone lo que falta al tomar un lote y no deja sacar más de lo que tiene', () => {
    const fixture = TestBed.createComponent(LotQuantityPickerModal);
    fixture.componentRef.setInput('show', true);
    fixture.componentRef.setInput('origen', 'PIM/Stock/MP');
    fixture.componentRef.setInput('line', {
      clave: 'MP-001', producto: 'Resina', unidad: 'KGS', solicitado: 500, asignaciones: [], entregado: 0,
    });
    fixture.componentRef.setInput('availableLots', [
      { lote: 'L-1', clave: 'MP-001', ubicacion: 'PIM/Stock/MP', cantidad: 300, estado: 'Libre' },
    ]);
    const agregados: { lote: string; cantidad: number }[] = [];
    fixture.componentInstance.added.subscribe(a => agregados.push(a));
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;

    (el.querySelector('button.btn-link.p-0.text-decoration-none') as HTMLButtonElement).click();
    fixture.detectChanges();
    (el.querySelector('button.btn-outline-primary') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(agregados).toEqual([{ lote: 'L-1', cantidad: 300 }]);

    const [folio, cantidad] = [...el.querySelectorAll('input')] as HTMLInputElement[];
    folio.value = 'L-1';
    folio.dispatchEvent(new Event('input'));
    cantidad.value = '400';
    cantidad.dispatchEvent(new Event('change'));
    (el.querySelector('button.btn-outline-primary') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(el.textContent).toContain('El lote L-1 solo tiene 300.0.');
  });
});
