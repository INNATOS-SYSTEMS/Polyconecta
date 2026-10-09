import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { OdooDualList } from './odoo-dual-list';
import { ModuloPermisoItem } from './odoo-dual-list.types';

const arbolPrueba: ModuloPermisoItem[] = [
  {
    modulo: 'ventas',
    etiqueta: 'Ventas',
    objetos: [
      {
        objeto: 'pedido',
        etiqueta: 'Pedido',
        tipo: 'Documento',
        acciones: [
          { clave: 'ventas.pedido.leer', accion: 'leer', etiqueta: 'Leer' },
          { clave: 'ventas.pedido.crear', accion: 'crear', etiqueta: 'Crear' },
          { clave: 'ventas.pedido.editar', accion: 'editar', etiqueta: 'Editar' },
        ],
      },
      {
        objeto: 'cliente',
        etiqueta: 'Cliente',
        tipo: 'Funcionalidad',
        acciones: [
          { clave: 'ventas.cliente.leer', accion: 'leer', etiqueta: 'Leer' },
        ],
      },
    ],
  },
  {
    modulo: 'inventario',
    etiqueta: 'Inventario',
    objetos: [
      {
        objeto: 'producto',
        etiqueta: 'Producto',
        tipo: 'Funcionalidad',
        acciones: [
          { clave: 'inventario.producto.leer', accion: 'leer', etiqueta: 'Leer' },
          { clave: 'inventario.producto.clasificar', accion: 'clasificar', etiqueta: 'Clasificar' },
        ],
      },
    ],
  },
];

@Component({
  standalone: true,
  imports: [OdooDualList],
  template: `
    <pc-odoo-dual-list
      [arbol]="arbol"
      [(seleccionados)]="seleccionados" />
  `,
})
class HostComponent {
  readonly arbol = arbolPrueba;
  readonly seleccionados = signal<string[]>(['ventas.pedido.leer']);
}

describe('OdooDualList', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let component: OdooDualList;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    component = fixture.debugElement.children[0].componentInstance as OdooDualList;
  });

  it('inicia separando correctamente disponibles y asignados según seleccionados()', () => {
    // Total 6 permisos: 1 asignado ('ventas.pedido.leer'), 5 disponibles
    expect(component.totalAsignados()).toBe(1);
    expect(component.totalDisponibles()).toBe(5);

    const el = fixture.nativeElement as HTMLElement;
    const badgeDisp = el.querySelector('[data-conteo-disponibles]')?.textContent?.trim();
    const badgeAsig = el.querySelector('[data-conteo-asignados]')?.textContent?.trim();

    expect(badgeDisp).toBe('5');
    expect(badgeAsig).toBe('1');
  });

  it('filtra los permisos disponibles por texto de búsqueda', () => {
    const el = fixture.nativeElement as HTMLElement;
    const inputBuscar = el.querySelector('[data-buscar-disponibles]') as HTMLInputElement;

    inputBuscar.value = 'clasificar';
    inputBuscar.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(component.totalDisponibles()).toBe(1);
    expect(component.arbolDisponibles()[0].modulo).toBe('inventario');
  });

  it('asigna permisos marcados al presionar el botón >', () => {
    const el = fixture.nativeElement as HTMLElement;
    // Marcar ventas.pedido.crear en el panel izquierdo
    const checkCrear = el.querySelector('[data-check-permiso="ventas.pedido.crear"]') as HTMLInputElement;
    checkCrear.click();
    fixture.detectChanges();

    expect(component.marcadosIzq().has('ventas.pedido.crear')).toBe(true);

    // Clic en asignar >
    const btnAsignar = el.querySelector('[data-btn-asignar]') as HTMLButtonElement;
    btnAsignar.click();
    fixture.detectChanges();

    expect(host.seleccionados()).toContain('ventas.pedido.crear');
    expect(component.totalAsignados()).toBe(2);
    expect(component.totalDisponibles()).toBe(4);
  });

  it('desasigna permisos marcados al presionar el botón <', () => {
    const el = fixture.nativeElement as HTMLElement;
    // Marcar ventas.pedido.leer en el panel derecho
    const checkLeerAsig = el.querySelector('[data-check-permiso-asig="ventas.pedido.leer"]') as HTMLInputElement;
    checkLeerAsig.click();
    fixture.detectChanges();

    expect(component.marcadosDer().has('ventas.pedido.leer')).toBe(true);

    // Clic en quitar <
    const btnQuitar = el.querySelector('[data-btn-quitar]') as HTMLButtonElement;
    btnQuitar.click();
    fixture.detectChanges();

    expect(host.seleccionados()).not.toContain('ventas.pedido.leer');
    expect(component.totalAsignados()).toBe(0);
    expect(component.totalDisponibles()).toBe(6);
  });

  it('asigna todos los permisos con el botón »', () => {
    const el = fixture.nativeElement as HTMLElement;
    const btnAsignarTodo = el.querySelector('[data-btn-asignar-todo]') as HTMLButtonElement;
    btnAsignarTodo.click();
    fixture.detectChanges();

    expect(component.totalAsignados()).toBe(6);
    expect(component.totalDisponibles()).toBe(0);
  });

  it('desasigna todos los permisos con el botón «', () => {
    const el = fixture.nativeElement as HTMLElement;
    const btnQuitarTodo = el.querySelector('[data-btn-quitar-todo]') as HTMLButtonElement;
    btnQuitarTodo.click();
    fixture.detectChanges();

    expect(component.totalAsignados()).toBe(0);
    expect(component.totalDisponibles()).toBe(6);
  });

  it('moverAccion con doble clic transfiere la acción directamente', () => {
    component.moverAccion('inventario.producto.clasificar', true);
    fixture.detectChanges();

    expect(host.seleccionados()).toContain('inventario.producto.clasificar');
    expect(component.totalAsignados()).toBe(2);

    // De vuelta
    component.moverAccion('inventario.producto.clasificar', false);
    fixture.detectChanges();

    expect(host.seleccionados()).not.toContain('inventario.producto.clasificar');
    expect(component.totalAsignados()).toBe(1);
  });

  it('moverObjeto transfiere todas las acciones de un objeto completo', () => {
    const modVentas = component.arbolDisponibles().find(m => m.modulo === 'ventas')!;
    const objPedido = modVentas.objetos.find(o => o.objeto === 'pedido')!;

    component.moverObjeto(objPedido, true);
    fixture.detectChanges();

    expect(host.seleccionados()).toContain('ventas.pedido.crear');
    expect(host.seleccionados()).toContain('ventas.pedido.editar');
  });
});
