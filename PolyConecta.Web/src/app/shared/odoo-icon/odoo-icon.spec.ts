import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { DE_BOOTSTRAP, ICONOS, OdooIcon, resolverIcono } from './odoo-icon';

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

  it('acepta el nombre de Bootstrap para migrar por partes', () => {
    expect(resolverIcono('bi bi-truck')).toBe('entrega');
    expect(resolverIcono('bi-x-octagon me-1')).toBe('hard-stop');
    expect(resolverIcono('no-existe')).toBeUndefined();
  });

  it('cubre los 45 íconos de Bootstrap que usaba la aplicación', () => {
    expect(Object.keys(DE_BOOTSTRAP)).toHaveLength(45);
    for (const n of Object.values(DE_BOOTSTRAP)) expect(ICONOS[n]).toBeDefined();
  });

  it('no pinta nada con un nombre desconocido', () => {
    const f = TestBed.createComponent(OdooIcon);
    f.componentRef.setInput('nombre', 'no-existe');
    f.detectChanges();
    expect(f.nativeElement.querySelector('svg')).toBeNull();
  });
});
