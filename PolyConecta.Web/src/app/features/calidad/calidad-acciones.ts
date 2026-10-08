import { inject, Injectable } from '@angular/core';
import { Dialog } from '@angular/cdk/dialog';
import { firstValueFrom } from 'rxjs';
import { OrigenEnMemoria } from '../../core/lista/origen-en-memoria';
import { ManufacturingOrder, ProductionLot } from '../../core/models/produccion';
import { SearchView } from '../../core/search/search-view';
import { CALIDAD } from '../../core/search/views';
import { CalidadLibre, ControlLibre } from '../../core/state/libre/calidad-libre';
import { OperationalFlowState } from '../../core/state/operational-flow-state';
import { abrirDialogo, OdooConfirmacion } from '../../shared/odoo-dialog/odoo-dialog';
import { estadoQc, qcFolio } from './calidad-estado';

/** Etapas del control (aclaración P4): salen de sus lotes, así que el kanban no se arrastra. */
export const ETAPAS_CALIDAD = ['Planeado', 'Parcial', 'Aprobado'];

/** Fila de la lista de Calidad: el control de una OF o un control libre (FR-012). */
export interface FilaControl {
  id: string;
  folio: string;
  producto: string;
  orden: string;
  proceso: string;
  lotes: number;
  enRevision: number;
  estado: string;
  /** OF del control ligado; vacío en un control libre. */
  of?: ManufacturingOrder;
  libre?: ControlLibre;
}

/**
 * Vista CALIDAD sobre las filas: los controles libres no tienen OF, así que los filtros de la vista
 * (proceso, resultado) no les aplican y se buscan por folio o lote, como en el prototipo.
 */
const VISTA: SearchView<FilaControl> = {
  campos: CALIDAD.campos.map(c => ({
    etiqueta: c.etiqueta,
    valor: (f: FilaControl) => (f.of ? c.valor(f.of) : c.etiqueta === 'Orden' ? [f.folio, ...(f.libre?.lotes.map(l => l.lote.lote) ?? [])].join(' ') : undefined),
  })),
  filtros: CALIDAD.filtros.map(x => ({ nombre: x.nombre, campo: x.campo, condicion: (f: FilaControl) => !!f.of && x.condicion(f.of) })),
  agrupaciones: CALIDAD.agrupaciones.map(g => ({ etiqueta: g.etiqueta, clave: (f: FilaControl) => (f.of ? g.clave(f.of) : 'Libre') })),
  referencia: f => f.folio,
};

/**
 * Acciones de Calidad (spec 011, P4): el origen de la lista y del kanban, y aprobar o fallar un lote.
 * Fallar pide confirmación (aclaración P4) en el control ligado y en el libre; aprobar es inmediato.
 */
@Injectable({ providedIn: 'root' })
export class CalidadAcciones {
  private readonly flow = inject(OperationalFlowState);
  private readonly qc = inject(CalidadLibre);
  private readonly dialog = inject(Dialog);

  readonly vista = VISTA;

  filas(): FilaControl[] {
    const ligados = this.flow.manufacturingOrders.filter(o => o.calidadRequerida).map<FilaControl>(of => ({
      id: of.folio, folio: qcFolio(of.folio), producto: of.producto, orden: of.folio, proceso: of.processLabel,
      lotes: of.produccion.length, enRevision: of.produccion.filter(l => l.estado === 'En revisión').length, estado: estadoQc(of), of,
    }));
    const libres = this.qc.controles.map<FilaControl>(c => ({
      id: c.folio, folio: c.folio, producto: '—', orden: '— (libre)', proceso: '—',
      lotes: c.lotes.length, enRevision: c.lotes.filter(l => l.lote.estado === 'En revisión').length, estado: this.qc.estado(c), libre: c,
    }));
    return [...ligados, ...libres];
  }

  origen(): OrigenEnMemoria<FilaControl> {
    return new OrigenEnMemoria<FilaControl>({ datos: () => this.filas(), id: f => f.id, vista: VISTA });
  }

  /** Confirma el rechazo de un lote; devuelve `true` si Calidad confirma. */
  async confirmarFalla(lote: ProductionLot): Promise<boolean> {
    const ref = abrirDialogo<boolean>(this.dialog, OdooConfirmacion, {
      titulo: 'Fallar lote',
      mensaje: `El lote ${lote.lote} queda rechazado y se renombra ${lote.lote.endsWith('.S') ? lote.lote : lote.lote + '.S'}. `
        + 'Calidad no lo libera: no se puede mover ni entregar (hard-stop). El rechazo no se puede deshacer.',
      confirmar: 'Fallar lote',
    });
    return (await firstValueFrom(ref.closed)) === true;
  }

  /** Falla el lote de un control ligado, previa confirmación. */
  async fallar(lote: ProductionLot): Promise<void> {
    if (await this.confirmarFalla(lote)) this.flow.rechazarLote(lote);
  }
}
