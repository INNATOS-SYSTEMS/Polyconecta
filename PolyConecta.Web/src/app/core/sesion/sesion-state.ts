import { Injectable, computed, signal } from '@angular/core';
import { Sesion, UsuarioSesion } from './sesion.types';

export type { UsuarioSesion, AsignacionSesion, Sesion, EntrarRequest } from './sesion.types';

/**
 * Estado y operaciones de la sesión del usuario contra la API (contracts/api-f1.md, Sesión).
 * Sustituye al usuario fijo de la réplica inicial. La barra superior lo usa, así que va en la carga
 * inicial: cargar, entrar y salir viven en `SesionAcciones`, que solo cargan las pantallas que los usan.
 */
@Injectable({ providedIn: 'root' })
export class SesionState {
  readonly sesion = signal<Sesion | null>(null);

  /** Datos del usuario autenticado; sin sesión, vacío: la barra no muestra a nadie mientras llega. */
  readonly usuario = computed<UsuarioSesion>(() => this.sesion()?.usuario ?? { id: 0, usuario: '', nombre: '' });

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

}
