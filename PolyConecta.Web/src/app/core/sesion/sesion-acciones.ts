import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, from, map } from 'rxjs';
import { pedirApi, respuestaApi } from './api';
import { EntrarRequest, Sesion } from './sesion.types';
import { SesionState } from './sesion-state';

/** Entrar y salir de la sesión (contracts/api-f1.md, Sesión). Fuera de `SesionState` para no subir la carga inicial. */
@Injectable({ providedIn: 'root' })
export class SesionAcciones {
  private readonly sesion = inject(SesionState);
  private readonly router = inject(Router, { optional: true });

  /** Inicia sesión con usuario y contraseña (POST /api/v1/plataforma/sesion). */
  iniciarSesion(usuario: string, contrasena: string): Observable<Sesion> {
    const req: EntrarRequest = { usuario, contrasena };
    return from(pedirApi<Sesion>('/api/v1/plataforma/sesion', { method: 'POST', body: JSON.stringify(req) })).pipe(
      map(sesion => {
        this.sesion.establecerSesion(sesion);
        return sesion;
      })
    );
  }

  /** Cierra la sesión activa (DELETE /api/v1/plataforma/sesion) y navega a /login. */
  cerrarSesion(): Observable<void> {
    return from(
      respuestaApi('/api/v1/plataforma/sesion', { method: 'DELETE' })
        .catch(() => undefined)
        .then(() => {
          this.sesion.establecerSesion(null);
          void this.router?.navigate(['/login']);
        })
    );
  }
}
