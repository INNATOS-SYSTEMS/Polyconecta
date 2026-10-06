import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { OdooPager } from './odoo-pager';

describe('OdooPager', () => {
  it('muestra 0-0 sin registros y 1-N con registros', () => {
    const fixture = TestBed.createComponent(OdooPager);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('0-0 / 0');

    fixture.componentRef.setInput('count', 7);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('1-7 / 7');
  });
});
