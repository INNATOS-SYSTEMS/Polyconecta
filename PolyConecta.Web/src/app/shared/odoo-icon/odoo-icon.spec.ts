import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { OdooIcon, resolverIcono } from './odoo-icon';

describe('OdooIcon', () => {
  it('pinta el ícono Lucide con el tamaño del contexto', () => {
    const f = TestBed.createComponent(OdooIcon);
    f.componentRef.setInput('nombre', 'confirmar');
    f.componentRef.setInput('contexto', 'icono');
    f.detectChanges();
    const svg: SVGElement = f.nativeElement.querySelector('svg');
    expect(svg.getAttribute('width')).toBe('18');
    expect(svg.getAttribute('stroke-width')).toBe('2');
    expect(svg.getAttribute('aria-hidden')).toBe('true');
    expect(svg.children.length).toBeGreaterThan(0);
  });

  it('solo resuelve nombres del catálogo', () => {
    expect(resolverIcono('entrega')).toBe('entrega');
    expect(resolverIcono('no-existe')).toBeUndefined();
  });

  it('no pinta nada con un nombre desconocido', () => {
    const f = TestBed.createComponent(OdooIcon);
    f.componentRef.setInput('nombre', 'no-existe');
    f.detectChanges();
    expect(f.nativeElement.querySelector('svg')).toBeNull();
  });
});
