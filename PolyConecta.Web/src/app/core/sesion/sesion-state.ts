import { Injectable, computed, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { Sesion, UsuarioSesion } from './sesion.types';

export type { UsuarioSesion, AsignacionSesion, Sesion, EntrarRequest } from './sesion.types';

/**
 * Estado y operaciones de la sesión del usuario contra la API (contracts/api-f1.md, Sesión).
 * Sustituye al usuario fijo de la réplica inicial. La barra superior lo usa, así que va en la carga
 * inicial: entrar y salir viven en `SesionAcciones`, que solo cargan las pantallas que los usan.
 */
@Injectable({ providedIn: 'root' })
export class SesionState {
  readonly sesion = signal<Sesion | null>(null);

  /** Datos del usuario autenticado; si no hay sesión, entrega usuario por omisión. */
  readonly usuario = computed<UsuarioSesion>(() =>
    this.sesion()?.usuario ?? { id: 0, usuario: 'porras', nombre: 'Alejandro Porras' }
  );

  /** True si hay una sesión activa en la API. */
  readonly conSesion = computed(() => this.sesion() !== null);

  /** Inicial del nombre para el avatar en la barra superior (D-143). */
  readonly inicial = computed(() => {
    const n = this.usuario().nombre.trim();
    return (n.charAt(0) || 'U').toUpperCase();
  });

  readonly asignaciones = computed(() => this.sesion()?.asignaciones ?? []);

  readonly permisos = computed(() => new Set(this.sesion()?.permisos ?? []));

  tienePermiso(permiso: string): boolean {
    return this.permisos().has(permiso);
  }

  establecerSesion(sesion: Sesion | null): void {
    this.sesion.set(sesion);
  }

  /**
   * Consulta la sesión actual con la cookie activa (GET /api/v1/plataforma/sesion).
   */
  cargarSesion(): Observable<Sesion | null> {
    return new Observable<Sesion | null>(subscriber => {
      fetch('/api/v1/plataforma/sesion', {
        headers: { 'X-Requested-With': 'PolyConecta' },
        credentials: 'same-origin',
      })
        .then(async res => {
          if (res.ok) {
            const data = (await res.json()) as Sesion;
            this.sesion.set(data);
            subscriber.next(data);
          } else {
            this.sesion.set(null);
            subscriber.next(null);
          }
          subscriber.complete();
        })
        .catch(() => {
          this.sesion.set(null);
          subscriber.next(null);
          subscriber.complete();
        });
    });
  }
}
