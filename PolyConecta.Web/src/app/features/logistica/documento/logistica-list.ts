import { Component, computed, inject, input, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { fechaCampo } from '../../../core/format/numero';
import { UiViewState } from '../../../core/state/ui-view-state';
import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { OdooKanban } from '../../../shared/odoo-kanban/odoo-kanban';
import { ColumnaLista } from '../../../shared/odoo-list/columnas';
import { OdooList } from '../../../shared/odoo-list/odoo-list';
import { OdooSeleccion } from '../../../shared/odoo-list/odoo-seleccion';
import { OdooPager } from '../../../shared/odoo-pager/odoo-pager';
import { OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';
import { OdooViewSwitcher } from '../../../shared/odoo-view-switcher/odoo-view-switcher';
import { FilaLogistica, LogisticaAcciones } from './logistica-acciones';
import { CONFIG, TipoLogistica } from './tipos';

/**
 * Réplica de Pages/TrasladosList, RecepcionList y EntregasList (.razor) sobre los componentes de la
 * spec 011 (P5): lista y kanban desde un origen de datos, con las etapas de cada documento.
 */
@Component({
  selector: 'pc-logistica-list',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSearchPanel, OdooViewSwitcher, OdooList, OdooSeleccion, OdooKanban, OdooIcon, OdooPager],
  templateUrl: './logistica-list.html',
  styles: ':host { display: contents; }',
})
export class LogisticaList {
  /** La tabla vive dentro del `@if` de la vista: su selección va en el panel de control (D-167). */
  protected readonly tabla = viewChild(OdooList);
  private readonly router = inject(Router);
  private readonly acciones = inject(LogisticaAcciones);
  protected readonly viewState = inject(UiViewState);
  protected readonly lista = viewChild(OdooList<FilaLogistica>);

  /** Llega del data de la ruta. */
  readonly tipo = input.required<TipoLogistica>();
  protected readonly cfg = computed(() => CONFIG[this.tipo()]);
  protected readonly origen = computed(() => this.acciones.origen(this.tipo()));
  protected readonly transiciones = computed(() => this.acciones.transiciones(this.tipo()));
  protected readonly etapas = computed(() => this.cfg().stages.map(e => ({ valor: e, titulo: e })));
  protected readonly fechaCampo = fechaCampo;
  protected readonly idDoc = (f: FilaLogistica) => f.id;
  protected readonly etapaDoc = (f: FilaLogistica) => f.estado;
  protected readonly columnas = computed<ColumnaLista<FilaLogistica>[]>(() => [
    { campo: 'folio', titulo: 'Folio', clase: 'fw-semibold text-primary' },
    { campo: 'operacion', titulo: 'Operación' },
    ...(this.cfg().mostrarOrigenEnLista ? [{ campo: 'origen', titulo: 'Origen' }] : []),
    { campo: 'destino', titulo: 'Destino' },
    { campo: 'estado', titulo: 'Estado', tipo: 'estado' as const },
  ]);

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly agrupaciones = signal<string[]>([]);

  protected abrir(f: FilaLogistica): void {
    void this.router.navigateByUrl(`${this.cfg().ruta}/${f.folio}`);
  }
}
