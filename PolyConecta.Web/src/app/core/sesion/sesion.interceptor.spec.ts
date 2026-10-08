import { HttpClient, HttpErrorResponse, HttpHandlerFn, HttpRequest, HttpResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CapturaState } from './captura-state';
import { DialogoLoginService } from './dialogo-login/dialogo-login.service';
import { SesionState } from './sesion-state';
import { sesionInterceptor } from './sesion.interceptor';

describe('sesionInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let mockRouter: { url: string; navigate: ReturnType<typeof vi.fn> };
  let mockSesion: { establecerSesion: ReturnType<typeof vi.fn> };
  let mockCaptura: CapturaState;
  let mockDialogo: { abrir: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    mockRouter = {
      url: '/ventas/pedidos/123',
      navigate: vi.fn(),
    };
    mockSesion = {
      establecerSesion: vi.fn(),
    };
    mockCaptura = new CapturaState();
    mockDialogo = {
      abrir: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([sesionInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: mockRouter },
        { provide: SesionState, useValue: mockSesion },
        { provide: CapturaState, useValue: mockCaptura },
        { provide: DialogoLoginService, useValue: mockDialogo },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('agrega encabezados X-Requested-With y X-Correlation-ID y activa withCredentials', () => {
    http.get('/api/v1/ventas/pedidos').subscribe();

    const req = httpMock.expectOne('/api/v1/ventas/pedidos');
    expect(req.request.headers.get('X-Requested-With')).toBe('PolyConecta');
    expect(req.request.headers.has('X-Correlation-ID')).toBe(true);
    expect(req.request.withCredentials).toBe(true);

    req.flush([]);
  });

  it('preserva el X-Correlation-ID existente si la petición ya lo incluía', () => {
    http.get('/api/v1/ventas/pedidos', {
      headers: { 'X-Correlation-ID': 'custom-id-999' },
    }).subscribe();

    const req = httpMock.expectOne('/api/v1/ventas/pedidos');
    expect(req.request.headers.get('X-Correlation-ID')).toBe('custom-id-999');

    req.flush([]);
  });

  it('no intercepta 401 en peticiones a /plataforma/sesion para no generar bucles', () => {
    mockCaptura.marcarCambiosSinGuardar(true);

    http.get('/api/v1/plataforma/sesion').subscribe({
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(401);
      },
    });

    const req = httpMock.expectOne('/api/v1/plataforma/sesion');
    req.flush('No autenticado', { status: 401, statusText: 'Unauthorized' });

    expect(mockDialogo.abrir).not.toHaveBeenCalled();
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });

  it('ante 401 sin cambios sin guardar, limpia sesión y redirige a /login?volver=', () => {
    mockCaptura.marcarCambiosSinGuardar(false);

    http.get('/api/v1/ventas/pedidos').subscribe({
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(401);
      },
    });

    const req = httpMock.expectOne('/api/v1/ventas/pedidos');
    req.flush('Sesión expirada', { status: 401, statusText: 'Unauthorized' });

    expect(mockDialogo.abrir).not.toHaveBeenCalled();
    expect(mockSesion.establecerSesion).toHaveBeenCalledWith(null);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: { volver: '/ventas/pedidos/123' },
    });
  });

  it('ante 401 con captura pendiente, abre diálogo y reintenta petición si reanuda con éxito', () => {
    mockCaptura.marcarCambiosSinGuardar(true);
    mockDialogo.abrir.mockReturnValue(of(true));

    let datosFinales: unknown = null;
    http.post('/api/v1/ventas/pedidos', { cliente: 'ACME' }).subscribe(res => {
      datosFinales = res;
    });

    // 1. Petición inicial falla con 401
    const req1 = httpMock.expectOne('/api/v1/ventas/pedidos');
    expect(req1.request.method).toBe('POST');
    req1.flush('Sesión expirada', { status: 401, statusText: 'Unauthorized' });

    // El diálogo se abrió
    expect(mockDialogo.abrir).toHaveBeenCalled();
    // No redirigió a /login porque se resolvió true
    expect(mockRouter.navigate).not.toHaveBeenCalled();

    // 2. Se reintenta la petición POST con los mismos datos
    const req2 = httpMock.expectOne('/api/v1/ventas/pedidos');
    expect(req2.request.method).toBe('POST');
    expect(req2.request.body).toEqual({ cliente: 'ACME' });
    req2.flush({ id: 1, folio: 'PV-2026-0001' });

    expect(datosFinales).toEqual({ id: 1, folio: 'PV-2026-0001' });
  });

  it('ante 401 con captura pendiente, si el usuario cancela el diálogo, limpia sesión y va a /login', () => {
    mockCaptura.marcarCambiosSinGuardar(true);
    mockDialogo.abrir.mockReturnValue(of(false));

    let errorCapturado: HttpErrorResponse | null = null;
    http.get('/api/v1/ventas/pedidos').subscribe({
      error: (err: HttpErrorResponse) => {
        errorCapturado = err;
      },
    });

    const req = httpMock.expectOne('/api/v1/ventas/pedidos');
    req.flush('Sesión expirada', { status: 401, statusText: 'Unauthorized' });

    expect(mockDialogo.abrir).toHaveBeenCalled();
    expect(mockSesion.establecerSesion).toHaveBeenCalledWith(null);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: { volver: '/ventas/pedidos/123' },
    });
    expect(errorCapturado).not.toBeNull();
  });
});
