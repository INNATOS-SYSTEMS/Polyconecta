import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { CapturaState, tieneCambiosPendientes } from './captura-state';
import { DialogoLoginService } from './dialogo-login/dialogo-login.service';
import { SesionState } from './sesion-state';

function generarCorrelationId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return 'corr-' + Math.random().toString(36).substring(2, 15);
}

/**
 * Interceptor HTTP de PolyConecta (L2-T010):
 * 1. Agrega encabezados X-Requested-With y X-Correlation-ID en cada petición.
 * 2. Si recibe 401:
 *    - Si hay cambios pendientes de guardar, abre el diálogo de login sin salir de la página
 *      y al autenticarse reintenta la petición original.
 *    - Si no hay cambios pendientes o el usuario cancela el diálogo, limpia la sesión y redirige a /login?volver=.
 */
export const sesionInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router, { optional: true });
  const sesion = inject(SesionState);
  const captura = inject(CapturaState, { optional: true });
  const dialogo = inject(DialogoLoginService, { optional: true });

  const headers: Record<string, string> = {
    'X-Requested-With': 'PolyConecta',
  };

  if (!req.headers.has('X-Correlation-ID')) {
    headers['X-Correlation-ID'] = generarCorrelationId();
  }

  const peticionClonada = req.clone({
    withCredentials: true,
    setHeaders: headers,
  });

  return next(peticionClonada).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      // No interceptar peticiones del propio endpoint de sesión para evitar bucles
      if (req.url.includes('/plataforma/sesion')) {
        return throwError(() => error);
      }

      const hayCambios = tieneCambiosPendientes(captura);

      if (hayCambios && dialogo) {
        return dialogo.abrir().pipe(
          switchMap(reanudado => {
            if (reanudado) {
              // Reintentar la petición original con los mismos datos
              return next(peticionClonada);
            }
            sesion.establecerSesion(null);
            const volver = router?.url || '/';
            router?.navigate(['/login'], { queryParams: { volver } });
            return throwError(() => error);
          })
        );
      }

      sesion.establecerSesion(null);
      const volver = router?.url || '/';
      router?.navigate(['/login'], { queryParams: { volver } });
      return throwError(() => error);
    })
  );
};
