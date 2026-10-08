import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { SesionState } from './sesion-state';

/**
 * Guardia de rutas para proteger vistas que requieren sesión activa (L2-T010).
 * Si no hay sesión en el estado local, intenta recuperarla desde la cookie de la API.
 * Si no hay sesión válida, redirige a /login?volver=<url>.
 */
export const sesionGuard: CanActivateFn = (_route, state) => {
  const sesion = inject(SesionState);
  const router = inject(Router);

  if (sesion.conSesion()) {
    return true;
  }

  return sesion.cargarSesion().pipe(
    map(s => {
      if (s) {
        return true;
      }
      return router.createUrlTree(['/login'], {
        queryParams: { volver: state.url },
      });
    }),
    catchError(() =>
      of(
        router.createUrlTree(['/login'], {
          queryParams: { volver: state.url },
        })
      )
    )
  );
};
