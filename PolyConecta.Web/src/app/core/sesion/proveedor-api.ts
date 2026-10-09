import { EnvironmentProviders, inject, makeEnvironmentProviders, provideEnvironmentInitializer } from '@angular/core';
import { Router } from '@angular/router';
import { registrarManejadorNoAutorizado } from './api';
import { CapturaState, tieneCambiosPendientes } from './captura-state';
import { DialogoLoginService } from './dialogo-login/dialogo-login.service';
import { SesionState } from './sesion-state';

/**
 * Registra el manejo del `401` del cliente de la API (L2-T010). Se provee en las rutas perezosas,
 * así el diálogo de inicio de sesión no entra a la carga inicial (D-143):
 * - con cambios sin guardar, abre el diálogo sobre la página y, al entrar, repite la petición;
 * - sin cambios, o si se cancela el diálogo, limpia la sesión y lleva a `/login?volver=`.
 */
export function proveerClienteApi(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideEnvironmentInitializer(() => {
      const router = inject(Router);
      const sesion = inject(SesionState);
      const captura = inject(CapturaState);
      const dialogo = inject(DialogoLoginService);

      registrarManejadorNoAutorizado(async () => {
        if (tieneCambiosPendientes(captura) && (await dialogo.abrir())) return true;
        sesion.establecerSesion(null);
        void router.navigate(['/login'], { queryParams: { volver: router.url || '/' } });
        return false;
      });
    }),
  ]);
}
