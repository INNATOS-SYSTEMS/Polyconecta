import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { OdooLoginForm } from './odoo-login-form';

describe('OdooLoginForm', () => {
  let fixture: ComponentFixture<OdooLoginForm>;
  let component: OdooLoginForm;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OdooLoginForm],
    }).compileComponents();

    fixture = TestBed.createComponent(OdooLoginForm);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('inicia con botón deshabilitado si faltan campos obligatorios', () => {
    const el = fixture.nativeElement as HTMLElement;
    const btnSubmit = el.querySelector('[data-login-submit]') as HTMLButtonElement;
    expect(btnSubmit.disabled).toBe(true);
  });

  it('muestra mensaje de error cuando se le pasa error input', () => {
    fixture.componentRef.setInput('error', 'Credenciales inválidas');
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const alertError = el.querySelector('[data-login-error]');
    expect(alertError).not.toBeNull();
    expect(alertError?.textContent).toContain('Credenciales inválidas');
  });

  it('emite evento enviar al completar los campos y enviar', () => {
    const spy = vi.fn();
    component.enviar.subscribe(spy);

    const el = fixture.nativeElement as HTMLElement;
    const inputUsuario = el.querySelector('[data-login-usuario]') as HTMLInputElement;
    const inputContrasena = el.querySelector('[data-login-contrasena]') as HTMLInputElement;
    const btnSubmit = el.querySelector('[data-login-submit]') as HTMLButtonElement;

    inputUsuario.value = 'ac1';
    inputUsuario.dispatchEvent(new Event('input'));
    inputContrasena.value = 'clave123';
    inputContrasena.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(btnSubmit.disabled).toBe(false);
    btnSubmit.click();
    fixture.detectChanges();

    expect(spy).toHaveBeenCalledWith({
      usuario: 'ac1',
      contrasena: 'clave123',
    });
  });

  it('muestra estado de carga y deshabilita entradas', () => {
    fixture.componentRef.setInput('cargando', true);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const inputUsuario = el.querySelector('[data-login-usuario]') as HTMLInputElement;
    const btnSubmit = el.querySelector('[data-login-submit]') as HTMLButtonElement;

    expect(inputUsuario.disabled).toBe(true);
    expect(btnSubmit.disabled).toBe(true);
    expect(btnSubmit.textContent).toContain('Iniciando sesión...');
  });
});
