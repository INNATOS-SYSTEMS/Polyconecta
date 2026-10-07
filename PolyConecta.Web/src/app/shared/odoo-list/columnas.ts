import { TemplateRef } from '@angular/core';
import { n1 } from '../../core/format/numero';

export type TipoColumna = 'texto' | 'numero' | 'moneda' | 'fecha' | 'estado';

/** Columna de `pc-odoo-list` (spec 011, contracts/componentes.md). */
export interface ColumnaLista<T> {
  campo: string;
  titulo: string;
  tipo?: TipoColumna;
  /** Valor crudo: el que se ordena, se suma y se exporta. Por omisión, la propiedad `campo`. */
  valor?: (fila: T) => unknown;
  /** Texto a mostrar. Por omisión, el valor con el formato de su tipo. */
  texto?: (fila: T) => string;
  /** Plantilla de la celda, para contenido con marcado (insignias, íconos). */
  celda?: TemplateRef<{ $implicit: T }>;
  /** Clases de la celda, como en la tabla de hoy (`fw-semibold text-primary`). */
  clase?: string;
  visible?: boolean;
  ordenable?: boolean;
  sumable?: boolean;
  ancho?: string;
}

/** Acción sobre las filas seleccionadas. Exportar a Excel siempre está. */
export interface AccionMasiva {
  nombre: string;
  icono?: string;
  ejecutar(ids: string[]): void;
}

const MONEDA = new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' });
const FECHA = new Intl.DateTimeFormat('es-MX', { day: '2-digit', month: '2-digit', year: 'numeric' });

export function leerColumna<T>(c: ColumnaLista<T>, fila: T): unknown {
  return c.valor ? c.valor(fila) : (fila as Record<string, unknown>)[c.campo];
}

export function textoColumna<T>(c: ColumnaLista<T>, fila: T): string {
  if (c.texto) return c.texto(fila);
  return formatear(c.tipo, leerColumna(c, fila));
}

export function formatear(tipo: TipoColumna | undefined, v: unknown): string {
  if (v == null || v === '') return '';
  switch (tipo) {
    case 'numero': return n1(Number(v));
    case 'moneda': return MONEDA.format(Number(v));
    case 'fecha': return v instanceof Date ? FECHA.format(v) : String(v);
    default: return String(v);
  }
}

export const esNumerica = (tipo?: TipoColumna): boolean => tipo === 'numero' || tipo === 'moneda';
