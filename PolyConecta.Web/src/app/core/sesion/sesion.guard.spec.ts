import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable, isObservable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { SesionAcciones } from './sesion-acciones';
import { SesionState } from './sesion-state';
import { sesionGuard } from './sesion.guard';
import { Sesion } from './sesion.types';

describe('sesionGuard', () => {
  let mockSesion: {
    conSesion: ReturnType<typeof vi.fn>;
    cargarSesion: ReturnType<typeof vi.fn>;
  };
  let mockRouter: {
    createUrlTree: ReturnType<typeof vi.fn>;
  };

  const dummyRoute = {} as ActivatedRouteSnapshot;
  const dummyState = { url: '/ventas/pedidos' } as RouterStateSnapshot;

  beforeEach(() => {
    mockSesion = {
      conSesion: vi.fn(),
      cargarSesion: vi.fn(),
    };
    mockRouter = {
      createUrlTree: vi.fn((commands, extras) => ({ commands, extras } as unknown as UrlTree)),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: SesionState, useValue: mockSesion },
        { provide: SesionAcciones, useValue: mockSesion },
        { provide: Router, useValue: mockRouter },
      ],
    });
  });

  function ejecutarGuard(): Observable<boolean | UrlTree> | boolean | UrlTree | any {
    return TestBed.runInInjectionContext(() => sesionGuard(dummyRoute, dummyState));
  }

  it('permite acceso inmediatamente si conSesion() es true', () => {
    mockSesion.conSesion.mockReturnValue(true);

    const resultado = ejecutarGuard();
    expect(resultado).toBe(true);
    expect(mockSesion.cargarSesion).not.toHaveBeenCalled();
  });

  it('permite acceso si conSesion() es false pero cargarSesion() recupera la sesión', async () => {
    mockSesion.conSesion.mockReturnValue(false);
    mockSesion.cargarSesion.mockReturnValue(
      of({
        usuario: { id: 1, usuario: 'ac1', nombre: 'Alejandro Carrillo' },
        asignaciones: [],
        permisos: [],
      } as Sesion)
    );

    const resultado = ejecutarGuard();
    expect(isObservable(resultado)).toBe(true);

    return new Promise<void>(resolve => {
      (resultado as Observable<boolean | UrlTree>).subscribe(val => {
        expect(val).toBe(true);
        expect(mockSesion.cargarSesion).toHaveBeenCalled();
        resolve();
      });
    });
  });

  it('redirige a /login?volver=<url> si cargarSesion() devuelve null', async () => {
    mockSesion.conSesion.mockReturnValue(false);
    mockSesion.cargarSesion.mockReturnValue(of(null));

    const resultado = ejecutarGuard();
    expect(isObservable(resultado)).toBe(true);

    return new Promise<void>(resolve => {
      (resultado as Observable<boolean | UrlTree>).subscribe(val => {
        expect(mockRouter.createUrlTree).toHaveBeenCalledWith(['/login'], {
          queryParams: { volver: '/ventas/pedidos' },
        });
        expect(val).toEqual(
          expect.objectContaining({
            commands: ['/login'],
            extras: { queryParams: { volver: '/ventas/pedidos' } },
          })
        );
        resolve();
      });
    });
  });

  it('redirige a /login?volver=<url> si cargarSesion() produce un error', async () => {
    mockSesion.conSesion.mockReturnValue(false);
    mockSesion.cargarSesion.mockReturnValue(throwError(() => new Error('Error de red')));

    const resultado = ejecutarGuard();
    expect(isObservable(resultado)).toBe(true);

    return new Promise<void>(resolve => {
      (resultado as Observable<boolean | UrlTree>).subscribe(val => {
        expect(mockRouter.createUrlTree).toHaveBeenCalledWith(['/login'], {
          queryParams: { volver: '/ventas/pedidos' },
        });
        resolve();
      });
    });
  });
});
