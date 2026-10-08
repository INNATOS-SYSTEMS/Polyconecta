import { Component, inject, Injectable } from '@angular/core';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { n1 } from '../../../core/format/numero';
import { TransicionKanban } from '../../../core/kanban/kanban';
import { OrigenEnMemoria } from '../../../core/lista/origen-en-memoria';
import { DocumentoLogistica } from '../../../core/models/logistica';
import { ProductionLot } from '../../../core/models/produccion';
import { LogisticsRow } from '../../../core/search/views';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { OdooDialog } from '../../../shared/odoo-dialog/odoo-dialog';
import { CONFIG, TipoLogistica } from './tipos';

/** Fila de la lista de traslados, recepciones o entregas: la de la vista de búsqueda más el documento. */
export interface FilaLogistica extends LogisticsRow {
  id: string;
  doc: DocumentoLogistica;
}

/** Lote que sale o entra al validar, con su cantidad (la del lote en el conjunto que permite el documento). */
export interface LoteAValidar {
  clave: string;
  lote: string;
  cantidad: number;
  unidad: string;
}

/**
 * Acciones de los documentos de logística (spec 011, P5 a P7): el origen de la lista y del kanban, y
 * validar, que usan el botón del formulario y el arrastre. "Validar" del formulario avanza una etapa
 * por clic, como el prototipo; en el kanban solo se arrastra Listo → Hecho, el cierre que mueve
 * inventario.
 * - Recepción (P6): al cerrar, un diálogo muestra los lotes que entran y la regla de D-56.
 * - Entregas (P7): hard-stop de Calidad; un lote elegido que no está liberado bloquea el cierre.
 */
@Injectable({ providedIn: 'root' })
export class LogisticaAcciones {
  private readonly flow = inject(OperationalFlowState);

  filas(tipo: TipoLogistica): FilaLogistica[] {
    return CONFIG[tipo].documentos(this.flow).map(doc => ({
      id: doc.folio, folio: doc.folio, operacion: doc.operacion, origen: doc.origen, destino: doc.destino, estado: doc.state, doc,
    }));
  }

  origen(tipo: TipoLogistica): OrigenEnMemoria<FilaLogistica> {
    return new OrigenEnMemoria<FilaLogistica>({ datos: () => this.filas(tipo), id: f => f.id, vista: CONFIG[tipo].vista });
  }

  /** Lotes que permite el documento: los de su regla si es libre (FR-012), los del prototipo si es semilla. */
  permitidos(tipo: TipoLogistica, doc: DocumentoLogistica): ProductionLot[] {
    return doc.libre ? this.flow.lotesDeDocumentoLibre(doc) : CONFIG[tipo].lotes(this.flow);
  }

  /** Lotes elegidos en el documento, con su cantidad si están entre los permitidos. */
  lotesAValidar(tipo: TipoLogistica, doc: DocumentoLogistica): LoteAValidar[] {
    const pool = this.permitidos(tipo, doc);
    return doc.lineas.flatMap(l => l.lotesSeleccionados.map(lote => ({ clave: l.clave, lote, cantidad: pool.find(p => p.lote === lote)?.real ?? 0, unidad: l.unidad })));
  }

  /** Entregas: lotes elegidos que ya no están liberados por Calidad (por ejemplo, rechazados después de elegirlos). */
  noLiberados(tipo: TipoLogistica, doc: DocumentoLogistica): string[] {
    if (tipo !== 'entrega') return [];
    const pool = this.permitidos(tipo, doc);
    return doc.lineas.flatMap(l => l.lotesSeleccionados).filter(lote => !pool.some(p => p.lote === lote));
  }

  motivoHardStop(lotes: string[]): string {
    return `Hard-stop de Calidad: ${lotes.length === 1 ? 'el lote' : 'los lotes'} ${lotes.join(', ')} no ${lotes.length === 1 ? 'está liberado' : 'están liberados'}. `
      + 'Quítelos de la entrega o espere a que Calidad los apruebe.';
  }

  /** Validar: avanza una etapa; en Listo cierra la salida de inventario. Devuelve el motivo si no procede. */
  validar(tipo: TipoLogistica, folio: string): string | undefined {
    const doc = CONFIG[tipo].documento(this.flow, folio);
    const bloqueados = doc.state === 'Listo' ? this.noLiberados(tipo, doc) : [];
    if (bloqueados.length > 0) {
      doc.error = this.motivoHardStop(bloqueados);
      this.flow.cambios.update(n => n + 1);
      return doc.error;
    }
    CONFIG[tipo].validar(this.flow, folio);
    return CONFIG[tipo].documento(this.flow, folio).error;
  }

  /** La recepción pide confirmar lo que entra cuando hay lotes elegidos (sin lotes, el cierre ya se rechaza con su motivo). */
  pideConfirmarRecepcion(doc: DocumentoLogistica): boolean {
    return doc.state === 'Listo' && doc.lineas.some(l => l.lotesSeleccionados.length > 0);
  }

  transiciones(tipo: TipoLogistica): TransicionKanban<FilaLogistica>[] {
    const validar: TransicionKanban<FilaLogistica> = { desde: 'Listo', hacia: 'Hecho', nombre: 'Validar', ejecutar: f => this.validar(tipo, f.folio) };
    if (tipo === 'recepcion') return [{ ...validar, dialogo: ValidarRecepcion, pideDialogo: f => this.pideConfirmarRecepcion(f.doc) }];
    return [validar];
  }
}

/**
 * Diálogo de validar la recepción (P6): los lotes que entran al almacén destino y la regla de D-56,
 * solo lotes en tránsito. Lo abren el botón "Validar" en Listo y el arrastre a Hecho.
 */
@Component({
  selector: 'pc-validar-recepcion',
  imports: [OdooDialog],
  template: `
    <pc-odoo-dialog titulo="Validar recepción" textoPrimario="Recibir" (confirmar)="ref.close(true)" (cancelar)="ref.close(false)">
      <p class="mb-2" data-regla-recepcion>Solo se reciben lotes <strong>en tránsito</strong> (<code>TRANS/*</code>): material que salió de otra planta con su traslado (D-56). Entran a <strong>{{ datos.fila.doc.destino }}</strong>.</p>
      <table class="table table-sm align-middle mb-0">
        <thead><tr><th>Clave</th><th>Lote</th><th class="text-end">Cantidad</th><th>Unidad</th></tr></thead>
        <tbody>
          @for (l of lotes; track l.lote) {
            <tr><td>{{ l.clave }}</td><td><code>{{ l.lote }}</code></td><td class="text-end">{{ n1(l.cantidad) }}</td><td>{{ l.unidad }}</td></tr>
          }
        </tbody>
      </table>
    </pc-odoo-dialog>
  `,
})
export class ValidarRecepcion {
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  protected readonly datos = inject<{ fila: { doc: DocumentoLogistica } }>(DIALOG_DATA);
  protected readonly n1 = n1;
  protected readonly lotes = inject(LogisticaAcciones).lotesAValidar('recepcion', this.datos.fila.doc);
}
