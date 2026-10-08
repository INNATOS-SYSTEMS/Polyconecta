import { Injectable, computed, signal } from '@angular/core';

export interface UsuarioSesion {
  nombre: string;
}

/**
 * Usuario de la sesión, para la barra superior. Hasta que exista el inicio de sesión (F1) es un usuario
 * fijo; F1 lo llena desde la API y conecta "Preferencias" y "Cerrar sesión".
 */
@Injectable({ providedIn: 'root' })
export class SesionState {
  readonly usuario = signal<UsuarioSesion>({ nombre: 'Alejandro Porras' });
  /** Primera letra del nombre, para el avatar. */
  readonly inicial = computed(() => this.usuario().nombre.trim().charAt(0).toUpperCase());
  /** Mientras no hay inicio de sesión, las opciones del usuario se ven pero no hacen nada. */
  readonly conSesion = signal(false);
}
