import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { routes } from '../../app.routes';
import { OdooTopbar } from './odoo-topbar';
import { moduleFor } from './modulos';

describe('OdooTopbar', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideRouter(routes)] }));

  it('resuelve el módulo por prefijo, como el prototipo', () => {
    expect(moduleFor('/ventas/pedidos/IV310-26').name).toBe('Ventas');
    expect(moduleFor('/ventas/inventario').name).toBe('Ventas');
    expect(moduleFor('/logistica/traslados/PIM/OUT/48213').name).toBe('Inventario');
    expect(moduleFor('/produccion/captura-masiva').name).toBe('Fabricación');
    expect(moduleFor('/').name).toBe('');
  });

  it('muestra el menú del módulo de la ruta actual', async () => {
    await TestBed.inject(Router).navigateByUrl('/produccion/fabricacion');
    const fixture = TestBed.createComponent(OdooTopbar);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.o_module_name')?.textContent).toBe('Fabricación');
    expect([...el.querySelectorAll('.o_header_menu a')].map(a => a.textContent)).toEqual(['Fabricación', 'Producción', 'Incidencias']);
    expect(el.querySelector('.o_header_menu a.active')?.textContent).toBe('Fabricación');
  });
});
