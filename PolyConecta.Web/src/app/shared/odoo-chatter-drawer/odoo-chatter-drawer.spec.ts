import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { OdooChatterDrawer, horaCorta } from './odoo-chatter-drawer';

describe('OdooChatterDrawer', () => {
  it('agrega la nota en local como Administrator', () => {
    const fixture = TestBed.createComponent(OdooChatterDrawer);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = 'Revisar calibre';
    input.dispatchEvent(new Event('change'));
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.componentInstance.messages()).toEqual([
      expect.objectContaining({ author: 'Administrator', text: 'Revisar calibre' }),
    ]);
    expect(fixture.nativeElement.textContent).toContain('Revisar calibre');
  });

  it('formatea la hora como h:mm tt de .NET', () => {
    expect(horaCorta(new Date(2026, 9, 5, 9, 5))).toBe('9:05 AM');
    expect(horaCorta(new Date(2026, 9, 5, 0, 30))).toBe('12:30 AM');
    expect(horaCorta(new Date(2026, 9, 5, 15, 0))).toBe('3:00 PM');
  });
});
