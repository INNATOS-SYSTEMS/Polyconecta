import { Injectable, signal } from '@angular/core';

/**
 * Estado que indica si la pantalla activa tiene captura o cambios sin guardar.
 * Permite al interceptor HTTP abrir el diálogo de reanudación ante un 401 para no perder los datos (caso límite spec 003).
 */
@Injectable({ providedIn: 'root' })
export class CapturaState {
  private readonly _tieneCambiosSinGuardar = signal(false);
  readonly tieneCambiosSinGuardar = this._tieneCambiosSinGuardar.asReadonly();

  marcarCambiosSinGuardar(hayCambios: boolean): void {
    this._tieneCambiosSinGuardar.set(hayCambios);
  }
}

/**
 * Determina si hay cambios sin guardar, revisando el estado del servicio y formularios con clase dirty en el DOM.
 */
export function tieneCambiosPendientes(captura?: CapturaState | null): boolean {
  if (captura?.tieneCambiosSinGuardar()) return true;
  if (typeof document !== 'undefined') {
    const elementosSucios = document.querySelectorAll('form.ng-dirty, input.ng-dirty, textarea.ng-dirty, [data-captura-pendiente="true"]');
    return elementosSucios.length > 0;
  }
  return false;
}
