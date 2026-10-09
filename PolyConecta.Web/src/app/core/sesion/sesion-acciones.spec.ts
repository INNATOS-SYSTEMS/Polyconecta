import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { SesionAcciones } from './sesion-acciones';
import { SesionState } from './sesion-state';
import { Sesion } from './sesion.types';

describe('SesionAcciones', () => {
  let sesion: SesionState;
  let acciones: SesionAcciones;
  let mockRouter: { navigate: ReturnType<typeof vi.fn> };

  const sesionEjemplo: Sesion = {
    usuario: { id: 1, usuario: 'ac1', nombre: 'Alejandro Carrillo' },
    asignaciones: [{ grupo: 'AC', nombreGrupo: 'Atención a Clientes', planta: 'Planta 1', suplente: false }],
    permisos: ['ventas.pedidos.crear', 'ventas.pedidos.editar'],
  };

  beforeEach(() => {
    mockRouter = { navigate: vi.fn().mockResolvedValue(true) };
    TestBed.configureTestingModule({ providers: [{ provide: Router, useValue: mockRouter }] });
    sesion = TestBed.inject(SesionState);
    acciones = TestBed.inject(SesionAcciones);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('cargarSesion recupera la sesión de la cookie con GET /api/v1/plataforma/sesion', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(sesionEjemplo), { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);

    const resultado = await firstValueFrom(acciones.cargarSesion());

    expect(resultado).toEqual(sesionEjemplo);
    expect(sesion.usuario().usuario).toBe('ac1');
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/plataforma/sesion', expect.objectContaining({ credentials: 'same-origin' }));
  });

  it('cargarSesion devuelve null y deja sin sesión ante un 401 o sin API', async () => {
    sesion.establecerSesion(sesionEjemplo);
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })));
    expect(await firstValueFrom(acciones.cargarSesion())).toBeNull();
    expect(sesion.conSesion()).toBe(false);

    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('sin red')));
    expect(await firstValueFrom(acciones.cargarSesion())).toBeNull();
  });

  it('iniciarSesion realiza POST /api/v1/plataforma/sesion y establece la sesión', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(sesionEjemplo), { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);

    const resultado = await firstValueFrom(acciones.iniciarSesion('ac1', 'clave123'));

    expect(resultado).toEqual(sesionEjemplo);
    expect(sesion.conSesion()).toBe(true);
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/plataforma/sesion', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ usuario: 'ac1', contrasena: 'clave123' }),
    }));
  });

  it('iniciarSesion falla y no deja sesión con credenciales malas', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })));

    await expect(firstValueFrom(acciones.iniciarSesion('ac1', 'mala'))).rejects.toMatchObject({ status: 401 });
    expect(sesion.conSesion()).toBe(false);
  });

  it('cerrarSesion realiza DELETE, limpia la sesión y navega a /login', async () => {
    sesion.establecerSesion(sesionEjemplo);
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);

    await firstValueFrom(acciones.cerrarSesion(), { defaultValue: undefined });

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/plataforma/sesion', expect.objectContaining({ method: 'DELETE' }));
    expect(sesion.conSesion()).toBe(false);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
  });
});
