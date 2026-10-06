import { Injectable, inject } from '@angular/core';
import { ManufacturingOrder, ProductionLot } from '../../models/produccion';
import { EstadoBase } from '../estado-base';
import { OperationalFlowState } from '../operational-flow-state';
import { siguienteFolio } from './linea-libre';

export interface LoteControlado {
  ofFolio: string;
  /** El mismo objeto del lote en su OF: aprobar o rechazar lo cambia ahí. */
  lote: ProductionLot;
}

export interface ControlLibre {
  folio: string;
  lotes: LoteControlado[];
}

/**
 * Control de calidad libre (FR-012): se crea sobre lotes que ya existen, sin pasar por la ficha de
 * la OF. Aprobar o rechazar usa las mismas operaciones que el control ligado, así que el efecto es
 * idéntico (estado del lote, `.S` al rechazar, hard-stop de la OF).
 */
@Injectable({ providedIn: 'root' })
export class CalidadLibre extends EstadoBase {
  private readonly flow = inject(OperationalFlowState);
  readonly controles: ControlLibre[] = [];

  /** Lotes en revisión de cualquier OF: los únicos que tiene sentido controlar. */
  elegibles(): LoteControlado[] {
    return this.flow.manufacturingOrders.flatMap((of: ManufacturingOrder) =>
      of.produccion.filter(l => l.estado === 'En revisión').map(lote => ({ ofFolio: of.folio, lote })),
    );
  }

  /** Cada lote se identifica por su OF y su nombre: la semilla repite nombres entre órdenes (R004-IV310-26). */
  crear(lotes: { ofFolio: string; lote: string }[]): { control?: ControlLibre; error?: string } {
    if (lotes.length === 0) return { error: 'Elija al menos un lote existente.' };
    const elegibles = this.elegibles();
    const elegidos: LoteControlado[] = [];
    for (const ref of lotes) {
      const l = elegibles.find(e => e.ofFolio === ref.ofFolio && e.lote.lote === ref.lote);
      if (!l) return { error: `El lote ${ref.lote} de ${ref.ofFolio} no existe o no está en revisión.` };
      elegidos.push(l);
    }
    const control: ControlLibre = { folio: siguienteFolio('QC', this.controles.map(c => c.folio)), lotes: elegidos };
    this.controles.push(control);
    this.notify();
    return { control };
  }

  control(folio: string): ControlLibre | undefined {
    return this.controles.find(c => c.folio === folio);
  }

  aprobar(l: LoteControlado): void {
    this.flow.aprobarLote(l.lote);
    this.notify();
  }

  rechazar(l: LoteControlado): void {
    this.flow.rechazarLote(l.lote);
    this.notify();
  }

  estado(control: ControlLibre): string {
    if (control.lotes.some(l => l.lote.estado === 'En revisión')) return 'Parcial';
    return control.lotes.some(l => l.lote.estado === 'Rechazado') ? 'Parcial' : 'Aprobado';
  }
}
