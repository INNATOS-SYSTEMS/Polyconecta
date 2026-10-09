import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { SesionState } from './sesion-state';
import { Sesion } from './sesion.types';

describe('SesionState', () => {
  let sesion: SesionState;
  let mockRouter: { navigate: ReturnType<typeof vi.fn> };
  let originalFetch: typeof globalThis.fetch;

  const sesionEjemplo: Sesion = {
    usuario: { id: 1, usuario: 'ac1', nombre: 'Alejandro Carrillo' },
    asignaciones: [{ grupo: 'AC', nombreGrupo: 'Atención a Clientes', planta: 'Planta 1', suplente: false }],
    permisos: ['ventas.pedidos.crear', 'ventas.pedidos.editar'],
  };

  beforeEach(() => {
    originalFetch = globalThis.fetch;
    mockRouter = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        SesionState,
        { provide: Router, useValue: mockRouter },
      ],
    });

    sesion = TestBed.inject(SesionState);
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    vi.restoreAllMocks();
  });

  it('inicia sin sesión activa pero con usuario por omisión para compatibilidad', () => {
    expect(sesion.conSesion()).toBe(false);
    expect(sesion.usuario().nombre).toBe('Alejandro Porras');
    expect(sesion.inicial()).toBe('A');
    expect(sesion.tienePermiso('ventas.pedidos.crear')).toBe(false);
  });

  it('establecerSesion actualiza estado, usuario, inicial y permisos', () => {
    sesion.establecerSesion(sesionEjemplo);

    expect(sesion.conSesion()).toBe(true);
    expect(sesion.usuario().usuario).toBe('ac1');
    expect(sesion.usuario().nombre).toBe('Alejandro Carrillo');
    expect(sesion.inicial()).toBe('A');
    expect(sesion.tienePermiso('ventas.pedidos.crear')).toBe(true);
    expect(sesion.tienePermiso('ventas.pedidos.eliminar')).toBe(false);
  });

  it('cargarSesion recupera sesión con cookie activa vía GET /api/v1/plataforma/sesion', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => sesionEjemplo,
    });

    return new Promise<void>(resolve => {
      sesion.cargarSesion().subscribe(resultado => {
        expect(resultado).toEqual(sesionEjemplo);
        expect(sesion.conSesion()).toBe(true);
        expect(sesion.usuario().usuario).toBe('ac1');
        expect(globalThis.fetch).toHaveBeenCalledWith('/api/v1/plataforma/sesion', expect.objectContaining({
          headers: { 'X-Requested-With': 'PolyConecta' },
          credentials: 'same-origin',
        }));
        resolve();
      });
    });
  });

  it('cargarSesion devuelve null y limpia estado si la respuesta no es exitosa (401)', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
    });

    return new Promise<void>(resolve => {
      sesion.cargarSesion().subscribe(resultado => {
        expect(resultado).toBeNull();
        expect(sesion.conSesion()).toBe(false);
        resolve();
      });
    });
  });
});
