import { signal } from '@angular/core';

/**
 * Base de los servicios de estado portados del prototipo: los objetos se mutan igual que en C# y
 * `cambios` sube en cada notificación, como el evento OnChange de Blazor. Las pantallas leen
 * `cambios()` dentro de sus computed para volver a pintarse.
 */
export abstract class EstadoBase {
  readonly cambios = signal(0);

  protected notify(): void {
    this.cambios.update(n => n + 1);
  }

  /** Para los servicios del modo libre, que agregan documentos a las colecciones de este estado. */
  notificar(): void {
    this.notify();
  }
}

/** Comparación de cadenas sin distinguir mayúsculas, como StringComparison.OrdinalIgnoreCase. */
export const igualSinMayusculas = (a: string | null | undefined, b: string | null | undefined): boolean =>
  (a ?? '').trim().toLowerCase() === (b ?? '').trim().toLowerCase();

export const contieneSinMayusculas = (texto: string, parte: string): boolean => texto.toLowerCase().includes(parte.toLowerCase());

export const suma = <T>(items: readonly T[], valor: (item: T) => number): number => items.reduce((t, i) => t + valor(i), 0);
