import { EnvironmentInjector, createEnvironmentInjector } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ErrorApi, pedirApi, registrarManejadorNoAutorizado, respuestaApi } from './api';
import { CapturaState } from './captura-state';
import { DialogoLoginService } from './dialogo-login/dialogo-login.service';
import { proveerClienteApi } from './proveedor-api';
import { SesionState } from './sesion-state';

function respuesta(status: number, cuerpo?: unknown): Response {
  return new Response(cuerpo === undefined ? null : JSON.stringify(cuerpo), { status });
}

describe('cliente de la API', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    registrarManejadorNoAutorizado(null);
    vi.unstubAllGlobals();
  });

  it('agrega X-Requested-With y X-Correlation-ID y manda la cookie', async () => {
    fetchMock.mockResolvedValueOnce(respuesta(200, []));

    await pedirApi('/api/v1/ventas/pedidos');

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Record<string, string>;
    expect(headers['X-Requested-With']).toBe('PolyConecta');
    expect(headers['X-Correlation-ID']).toBeTruthy();
    expect(init.credentials).toBe('same-origin');
  });

  it('respeta el X-Correlation-ID que trae la petición', async () => {
    fetchMock.mockResolvedValueOnce(respuesta(200, []));

    await pedirApi('/api/v1/ventas/pedidos', { headers: { 'X-Correlation-ID': 'custom-id-999' } });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect((init.headers as Record<string, string>)['X-Correlation-ID']).toBe('custom-id-999');
  });

  it('traduce el ProblemDetails a ErrorApi con code y errores por campo', async () => {
    fetchMock.mockResolvedValueOnce(
      respuesta(400, { code: 'VALIDACION', detail: 'Faltan datos', errores: [{ campo: 'clienteId', mensaje: 'Obligatorio' }] })
    );

    const error = (await pedirApi('/api/v1/ventas/pedidos', { method: 'POST' }).catch(e => e)) as ErrorApi;

    expect(error).toBeInstanceOf(ErrorApi);
    expect(error).toMatchObject({ status: 400, code: 'VALIDACION', message: 'Faltan datos' });
    expect(error.errores).toEqual([{ campo: 'clienteId', mensaje: 'Obligatorio' }]);
  });

  it('repite la petición con el mismo cuerpo si el manejador del 401 lo pide', async () => {
    fetchMock.mockResolvedValueOnce(respuesta(401)).mockResolvedValueOnce(respuesta(200, { id: 1 }));
    const manejador = vi.fn().mockResolvedValue(true);
    registrarManejadorNoAutorizado(manejador);

    const datos = await pedirApi('/api/v1/ventas/pedidos', { method: 'POST', body: '{"cliente":"ACME"}' });

    expect(manejador).toHaveBeenCalledOnce();
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect((fetchMock.mock.calls[1] as [string, RequestInit])[1].body).toBe('{"cliente":"ACME"}');
    expect(datos).toEqual({ id: 1 });
  });

  it('no atiende el 401 de /plataforma/sesion, para no abrir el diálogo en bucle', async () => {
    fetchMock.mockResolvedValueOnce(respuesta(401));
    const manejador = vi.fn().mockResolvedValue(true);
    registrarManejadorNoAutorizado(manejador);

    const res = await respuestaApi('/api/v1/plataforma/sesion');

    expect(res.status).toBe(401);
    expect(manejador).not.toHaveBeenCalled();
  });
});

describe('proveerClienteApi', () => {
  let fetchMock: ReturnType<typeof vi.fn>;
  let router: { url: string; navigate: ReturnType<typeof vi.fn> };
  let sesion: { establecerSesion: ReturnType<typeof vi.fn> };
  let captura: CapturaState;
  let dialogo: { abrir: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
    router = { url: '/ventas/pedidos/123', navigate: vi.fn().mockResolvedValue(true) };
    sesion = { establecerSesion: vi.fn() };
    captura = new CapturaState();
    dialogo = { abrir: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: router },
        { provide: SesionState, useValue: sesion },
        { provide: CapturaState, useValue: captura },
        { provide: DialogoLoginService, useValue: dialogo },
      ],
    });
    createEnvironmentInjector([proveerClienteApi()], TestBed.inject(EnvironmentInjector));
  });

  afterEach(() => {
    registrarManejadorNoAutorizado(null);
    vi.unstubAllGlobals();
  });

  it('sin cambios sin guardar, limpia la sesión y lleva a /login?volver=', async () => {
    fetchMock.mockResolvedValueOnce(respuesta(401));

    const error = (await pedirApi('/api/v1/ventas/pedidos').catch(e => e)) as ErrorApi;

    expect(error).toMatchObject({ status: 401 });
    expect(dialogo.abrir).not.toHaveBeenCalled();
    expect(sesion.establecerSesion).toHaveBeenCalledWith(null);
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { volver: '/ventas/pedidos/123' } });
  });

  it('con captura pendiente, abre el diálogo y repite la petición al entrar', async () => {
    captura.marcarCambiosSinGuardar(true);
    dialogo.abrir.mockResolvedValue(true);
    fetchMock.mockResolvedValueOnce(respuesta(401)).mockResolvedValueOnce(respuesta(200, { folio: 'PV-2026-0001' }));

    const datos = await pedirApi('/api/v1/ventas/pedidos', { method: 'POST', body: '{}' });

    expect(dialogo.abrir).toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
    expect(datos).toEqual({ folio: 'PV-2026-0001' });
  });

  it('con captura pendiente, si se cancela el diálogo, limpia la sesión y va a /login', async () => {
    captura.marcarCambiosSinGuardar(true);
    dialogo.abrir.mockResolvedValue(false);
    fetchMock.mockResolvedValueOnce(respuesta(401));

    const error = (await pedirApi('/api/v1/ventas/pedidos').catch(e => e)) as ErrorApi;

    expect(error).toMatchObject({ status: 401 });
    expect(sesion.establecerSesion).toHaveBeenCalledWith(null);
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { volver: '/ventas/pedidos/123' } });
  });
});
