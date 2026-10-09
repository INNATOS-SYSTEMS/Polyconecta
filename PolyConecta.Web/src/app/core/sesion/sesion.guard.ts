import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { SesionAcciones } from './sesion-acciones';
import { SesionState } from './sesion-state';

/**
 * Guardia de rutas para proteger vistas que requieren sesión activa (L2-T010).
 * Si no hay sesión en el estado local, intenta recuperarla desde la cookie de la API.
 * Si no hay sesión válida, redirige a /login?volver=<url>.
 */
export const sesionGuard: CanActivateFn = (_route, state) => {
  const sesion = inject(SesionState);
  const acciones = inject(SesionAcciones);
  const router = inject(Router);

  if (sesion.conSesion()) {
    return true;
  }

  return acciones.cargarSesion().pipe(
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
