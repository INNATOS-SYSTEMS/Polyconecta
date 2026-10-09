import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, defer, from, map } from 'rxjs';
import { pedirApi, respuestaApi } from './api';
import { EntrarRequest, Sesion } from './sesion.types';
import { SesionState } from './sesion-state';

/** Cargar, entrar y salir de la sesión (contracts/api-f1.md, Sesión). Fuera de `SesionState` para no subir la carga inicial. */
@Injectable({ providedIn: 'root' })
export class SesionAcciones {
  private readonly sesion = inject(SesionState);
  private readonly router = inject(Router, { optional: true });

  /**
   * Recupera la sesión de la cookie (GET /api/v1/plataforma/sesion); null si no hay. La llaman la guardia de
   * las rutas con sesión y el menú del usuario, que carga diferido: así no entra a la carga inicial (D-143).
   */
  cargarSesion(): Observable<Sesion | null> {
    return defer(async () => {
      let sesion: Sesion | null = null;
      try {
        const res = await respuestaApi('/api/v1/plataforma/sesion');
        if (res.ok) sesion = (await res.json()) as Sesion;
      } catch {
        // Sin API: sin sesión.
      }
      this.sesion.establecerSesion(sesion);
      return sesion;
    });
  }

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
