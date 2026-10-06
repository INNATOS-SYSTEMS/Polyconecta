import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { LotPickerModal } from './lot-picker-modal';

describe('LotPickerModal', () => {
  it('agrega un lote disponible y rechaza uno desconocido o repetido', () => {
    const fixture = TestBed.createComponent(LotPickerModal);
    fixture.componentRef.setInput('show', true);
    fixture.componentRef.setInput('availableLots', [{ lote: 'R001-IV310-26', real: 100, unidad: 'KGS', estado: 'Aprobado' }]);
    fixture.componentRef.setInput('requiredQty', 100);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const input = el.querySelector('input') as HTMLInputElement;
    const capturar = (valor: string) => {
      input.value = valor;
      input.dispatchEvent(new Event('input'));
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
      fixture.detectChanges();
    };

    capturar('R001-IV310-26');
    expect(fixture.componentInstance.selectedFolios()).toEqual(['R001-IV310-26']);
    expect(el.textContent).toContain('Completo');

    capturar('R001-IV310-26');
    expect(el.textContent).toContain('El lote R001-IV310-26 ya fue capturado.');

    capturar('NO-EXISTE');
    expect(el.textContent).toContain('Lote NO-EXISTE no encontrado o no aprobado.');
  });
});
