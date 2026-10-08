import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { formatearNumero, interpretarNumero, OdooNumber } from './odoo-number';

describe('OdooNumber', () => {
  it('formatea en es-MX con decimales fijos y porcentaje', () => {
    expect(formatearNumero(5500, 'cantidad', 1)).toBe('5,500.0');
    expect(formatearNumero(5.7, 'moneda', 2)).toBe('5.70');
    expect(formatearNumero(12.5, 'porcentaje', 1)).toBe('12.5 %');
    expect(formatearNumero(null, 'cantidad', 2)).toBe('');
  });

  it('interpreta lo escrito con separadores y signos; vacío es null', () => {
    expect(interpretarNumero('1,234.5')).toBe(1234.5);
    expect(interpretarNumero('$ 12')).toBe(12);
    expect(interpretarNumero('')).toBeNull();
    expect(interpretarNumero('abc')).toBeNaN();
  });

  it('edita sin formato, confirma al salir y avisa el mínimo', () => {
    const f = TestBed.createComponent(OdooNumber);
    f.componentRef.setInput('min', 0);
    f.componentRef.setInput('unidad', 'KG');
    const cambios: (number | null)[] = [];
    f.componentInstance.registerOnChange(v => cambios.push(v));
    f.componentInstance.writeValue(1500);
    f.detectChanges();
    const input: HTMLInputElement = f.nativeElement.querySelector('input');
    expect(input.value).toBe('1,500.00');
    expect(f.nativeElement.textContent).toContain('KG');
    input.dispatchEvent(new Event('focus'));
    f.detectChanges();
    expect(input.value).toBe('1500');
    input.value = '-2';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    f.detectChanges();
    expect(f.nativeElement.querySelector('[role="alert"]').textContent).toContain('El valor mínimo es 0.00.');
    expect(cambios).toEqual([]);
    input.dispatchEvent(new Event('focus'));
    input.value = '20';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    f.detectChanges();
    expect(cambios).toEqual([20]);
  });
});
