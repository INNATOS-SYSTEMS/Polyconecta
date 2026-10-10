import { Component, computed, inject, signal, TemplateRef, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { fechaCampo } from '../../../core/format/numero';
import { OPERACIONES } from '../../../core/search/views';
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
import { ETAPAS_RECOLECCION, FilaRecoleccion, RecoleccionAcciones } from '../recoleccion-acciones';

/**
 * Réplica de Pages/RecoleccionesList.razor sobre los componentes de la spec 011 (P3): lista y kanban
 * desde un origen de datos; el kanban valida al arrastrar a Hecho.
 */
@Component({
  selector: 'pc-recolecciones-list',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSearchPanel, OdooViewSwitcher, OdooList, OdooSeleccion, OdooKanban, OdooIcon, OdooPager],
  templateUrl: './recolecciones-list.html',
  styles: ':host { display: contents; }',
})
export class RecoleccionesList {
  /** La tabla vive dentro del `@if` de la vista: su selección va en el panel de control (D-167). */
  protected readonly tabla = viewChild(OdooList);
  private readonly router = inject(Router);
  private readonly acciones = inject(RecoleccionAcciones);
  protected readonly viewState = inject(UiViewState);
  protected readonly vista = OPERACIONES;
  protected readonly lista = viewChild(OdooList<FilaRecoleccion>);
  private readonly celdaFolio = viewChild.required<TemplateRef<{ $implicit: FilaRecoleccion }>>('celdaFolio');

  protected readonly origen = this.acciones.origen();
  protected readonly transiciones = this.acciones.transiciones();
  protected readonly fechaCampo = fechaCampo;
  protected readonly etapas = ETAPAS_RECOLECCION.map(e => ({ valor: e, titulo: e }));
  protected readonly idRecoleccion = (f: FilaRecoleccion) => f.id;
  protected readonly etapaRecoleccion = (f: FilaRecoleccion) => f.estado;
  protected readonly columnas = computed<ColumnaLista<FilaRecoleccion>[]>(() => [
    { campo: 'folio', titulo: 'Folio', clase: 'fw-semibold text-primary', celda: this.celdaFolio() },
    { campo: 'operacion', titulo: 'Operación' },
    { campo: 'origen', titulo: 'Origen' },
    { campo: 'destino', titulo: 'Destino' },
    { campo: 'estado', titulo: 'Estado', tipo: 'estado' },
  ]);

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly agrupaciones = signal<string[]>([]);

  protected abrir(f: FilaRecoleccion): void {
    void this.router.navigateByUrl(`/logistica/recolecciones/${f.folio}`);
  }
}
