import { inject, Injectable } from '@angular/core';
import { TransicionKanban } from '../../../core/kanban/kanban';
import { OrigenEnMemoria } from '../../../core/lista/origen-en-memoria';
import { DocumentoLogistica } from '../../../core/models/logistica';
import { LogisticsRow } from '../../../core/search/views';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { CONFIG, TipoLogistica } from './tipos';

/** Fila de la lista de traslados, recepciones o entregas: la de la vista de búsqueda más el documento. */
export interface FilaLogistica extends LogisticsRow {
  id: string;
  doc: DocumentoLogistica;
}

/**
 * Acciones de los documentos de logística (spec 011, P5 a P7): el origen de la lista y del kanban, y
 * validar, que usan el botón del formulario y el arrastre. "Validar" del formulario avanza una etapa
 * por clic, como el prototipo; en el kanban solo se arrastra Listo → Hecho, el cierre que mueve
 * inventario. Recepción y Entregas declaran su transición en P6 y P7.
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

  /** Validar: avanza una etapa; en Listo cierra la salida de inventario. Devuelve el motivo si no procede. */
  validar(tipo: TipoLogistica, folio: string): string | undefined {
    CONFIG[tipo].validar(this.flow, folio);
    return CONFIG[tipo].documento(this.flow, folio).error;
  }

  transiciones(tipo: TipoLogistica): TransicionKanban<FilaLogistica>[] {
    if (tipo !== 'traslado') return [];
    return [{ desde: 'Listo', hacia: 'Hecho', nombre: 'Validar', ejecutar: f => this.validar(tipo, f.folio) }];
  }
}
