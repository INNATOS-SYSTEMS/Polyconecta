import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { fechaGuion } from '../../../core/format/numero';
import { OrigenEnMemoria } from '../../../core/lista/origen-en-memoria';
import { Incidencia } from '../../../core/models/produccion';
import { adaptarVista } from '../../../core/search/search-view';
import { INCIDENCIAS } from '../../../core/search/views';
import { UiViewState } from '../../../core/state/ui-view-state';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { OdooKanban } from '../../../shared/odoo-kanban/odoo-kanban';
import { ColumnaLista } from '../../../shared/odoo-list/columnas';
import { OdooList } from '../../../shared/odoo-list/odoo-list';
import { OdooSeleccion } from '../../../shared/odoo-list/odoo-seleccion';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';
import { OdooViewSwitcher } from '../../../shared/odoo-view-switcher/odoo-view-switcher';

/** Fila de la lista: la incidencia con un id (no lo tiene: es un renglón de captura, P-27). */
interface FilaIncidencia extends Incidencia {
  id: string;
}

/**
 * Réplica de Pages/IncidenciasPage.razor. Ya es libre en el prototipo (FR-012). Spec 011 (P2): la lista
 * sale de un origen de datos y el kanban agrupa por centro de trabajo, sin arrastre (no tiene estados, P-27).
 */
@Component({
  selector: 'pc-incidencias',
  imports: [OdooBreadcrumb, OdooSearchPanel, OdooViewSwitcher, OdooList, OdooSeleccion, OdooKanban, OdooIcon],
  templateUrl: './incidencias.html',
  styles: ':host { display: contents; }',
})
export class Incidencias {
  /** La tabla vive dentro del `@if` de la vista: su selección va en el panel de control (D-167). */
  protected readonly tabla = viewChild(OdooList);
  private readonly flow = inject(OperationalFlowState);
  protected readonly vista = INCIDENCIAS;
  protected readonly fechaGuion = fechaGuion;

  protected readonly centro = signal('');
  protected readonly tipo = signal('');
  protected readonly comentarios = signal('');
  protected readonly horaInicio = signal('');
  protected readonly horaFin = signal('');
  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);

  protected readonly agrupaciones = signal<string[]>([]);
  protected readonly viewState = inject(UiViewState);
  protected readonly lista = viewChild(OdooList<FilaIncidencia>);

  protected readonly origen = new OrigenEnMemoria<FilaIncidencia>({
    datos: () => this.flow.incidencias.map((inc, i) => ({ ...inc, id: String(i) })),
    id: f => f.id,
    vista: adaptarVista(INCIDENCIAS, (f: FilaIncidencia) => f),
  });
  protected readonly idIncidencia = (f: FilaIncidencia) => f.id;
  protected readonly centroDe = (f: FilaIncidencia) => f.centroTrabajo;
  protected readonly columnas: ColumnaLista<FilaIncidencia>[] = [
    { campo: 'fecha', titulo: 'Fecha', texto: f => fechaGuion(f.fecha), clase: 'small' },
    { campo: 'centroTrabajo', titulo: 'Centro de trabajo', clase: 'small' },
    { campo: 'tipo', titulo: 'Tipo de incidencia', clase: 'small' },
    { campo: 'comentarios', titulo: 'Comentarios', clase: 'small' },
    { campo: 'horaInicio', titulo: 'Hora Inicio', clase: 'small' },
    { campo: 'horaFin', titulo: 'Hora Fin', clase: 'small' },
  ];
  /** Columnas del kanban: un centro de trabajo por columna, en orden alfabético. */
  protected readonly centros = computed(() => {
    this.flow.cambios();
    return [...new Set(this.flow.incidencias.map(i => i.centroTrabajo))].sort((a, b) => a.localeCompare(b, 'es-MX')).map(c => ({ valor: c, titulo: c }));
  });

  protected valor(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected agregar(): void {
    if (this.centro().trim() === '' || this.tipo().trim() === '') return;
    const hoy = new Date();
    this.flow.incidencias.push({
      fecha: new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate()),
      centroTrabajo: this.centro(),
      tipo: this.tipo(),
      comentarios: this.comentarios(),
      horaInicio: this.horaInicio(),
      horaFin: this.horaFin(),
    });
    for (const s of [this.centro, this.tipo, this.comentarios, this.horaInicio, this.horaFin]) s.set('');
    this.flow.cambios.update(n => n + 1);
    this.lista()?.recargar();
  }
}
