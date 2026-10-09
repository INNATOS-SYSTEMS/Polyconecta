import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { OdooBreadcrumb } from './odoo-breadcrumb';

describe('OdooBreadcrumb', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideRouter([])] }));

  it('colapsa los niveles anteriores en "…" y los despliega al pulsarlo', () => {
    const fixture = TestBed.createComponent(OdooBreadcrumb);
    fixture.componentRef.setInput('items', [
      { label: 'Pedidos', url: '/ventas/pedidos' },
      { label: 'IV310-26', url: '/ventas/pedidos/IV310-26' },
      { label: 'BOL-2026-0001' },
    ]);
    fixture.detectChanges();
    const nav = fixture.nativeElement as HTMLElement;
    expect(nav.querySelector('.o_breadcrumb_more')).not.toBeNull();
    expect(nav.textContent).not.toContain('Pedidos');
    expect(nav.querySelector('.current-item')?.textContent).toBe('BOL-2026-0001');

    (nav.querySelector('.o_breadcrumb_more') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(nav.textContent).toContain('Pedidos');
  });
});
