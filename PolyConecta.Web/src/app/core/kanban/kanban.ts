import { Type } from '@angular/core';

/** Columna de un kanban (spec 011, data-model §3). */
export interface EtapaKanban {
  /** Estado del documento, o valor del campo de agrupación. */
  valor: string;
  titulo: string;
  /** Las etapas terminales (Hecho, Cancelado) empiezan plegadas. */
  plegada?: boolean;
}

/**
 * Movimiento permitido entre dos etapas. `ejecutar` llama al MISMO método del servicio de estado que el
 * botón del formulario, así que la regla vive en un solo lugar; devuelve el motivo si no procede.
 */
export interface TransicionKanban<T> {
  desde: string;
  hacia: string;
  nombre: string;
  /** Diálogo que captura los datos que pide la transición; la tarjeta se mueve solo si se confirma. */
  dialogo?: Type<unknown> | null;
  ejecutar(fila: T, datos?: unknown): string | undefined;
}

/** Motivo cuando no hay transición declarada entre dos etapas. */
export function motivoSinTransicion(desde: string, hacia: string): string {
  return `No se puede pasar de ${desde} a ${hacia}`;
}
