import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { EntrarRequest, Sesion, UsuarioSesion } from './sesion.types';

export type { UsuarioSesion, AsignacionSesion, Sesion, EntrarRequest } from './sesion.types';

/**
 * Estado y operaciones de la sesión del usuario contra la API (contracts/api-f1.md, Sesión).
 * Sustituye al usuario fijo de la réplica inicial.
 */
@Injectable({ providedIn: 'root' })
export class SesionState {
  private readonly router = inject(Router, { optional: true });

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

  /**
   * Inicia sesión con usuario y contraseña (POST /api/v1/plataforma/sesion).
   */
  iniciarSesion(usuario: string, contrasena: string): Observable<Sesion> {
    const req: EntrarRequest = { usuario, contrasena };
    return new Observable<Sesion>(subscriber => {
      fetch('/api/v1/plataforma/sesion', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Requested-With': 'PolyConecta',
        },
        body: JSON.stringify(req),
        credentials: 'same-origin',
      })
        .then(async res => {
          if (res.ok) {
            const data = (await res.json()) as Sesion;
            this.sesion.set(data);
            subscriber.next(data);
            subscriber.complete();
          } else {
            subscriber.error(new Error('Credenciales inválidas'));
          }
        })
        .catch(err => subscriber.error(err));
    });
  }

  /**
   * Cierra la sesión activa (DELETE /api/v1/plataforma/sesion) y navega a /login.
   */
  cerrarSesion(): Observable<void> {
    const terminar = () => {
      this.sesion.set(null);
      this.router?.navigate(['/login']);
    };

    return new Observable<void>(subscriber => {
      fetch('/api/v1/plataforma/sesion', {
        method: 'DELETE',
        headers: { 'X-Requested-With': 'PolyConecta' },
        credentials: 'same-origin',
      })
        .finally(() => {
          terminar();
          subscriber.next();
          subscriber.complete();
        });
    });
  }
}
