import { Component, inject, signal, viewChild } from '@angular/core';
import { InventoryState } from '../../../core/state/inventory-state';
import { etiquetaProducto } from '../../../core/format/producto-etiqueta';
import { Router } from '@angular/router';
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
import { CalidadAcciones, ETAPAS_CALIDAD, FilaControl } from '../calidad-acciones';

/**
 * Réplica de Pages/CalidadList.razor sobre los componentes de la spec 011 (P4): un control por cada OF
 * que requiere calidad, más los controles libres. El kanban agrupa por estado y no se arrastra: el
 * estado sale de los lotes (aclaración P4).
 */
@Component({
  selector: 'pc-calidad-list',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSearchPanel, OdooViewSwitcher, OdooList, OdooSeleccion, OdooKanban, OdooIcon, OdooPager],
  templateUrl: './calidad-list.html',
  styles: ':host { display: contents; }',
})
export class CalidadList {
  /** La tabla vive dentro del `@if` de la vista: su selección va en el panel de control (D-167). */
  protected readonly tabla = viewChild(OdooList);
  protected readonly producto = (claveONombre?: string | null, nombre?: string) => etiquetaProducto(this.inv.catalogo, claveONombre, nombre);
  private readonly inv = inject(InventoryState);
  private readonly router = inject(Router);
  private readonly acciones = inject(CalidadAcciones);
  protected readonly viewState = inject(UiViewState);
  protected readonly vista = this.acciones.vista;
  protected readonly lista = viewChild(OdooList<FilaControl>);

  protected readonly origen = this.acciones.origen();
  protected readonly etapas = ETAPAS_CALIDAD.map(e => ({ valor: e, titulo: e }));
  protected readonly idControl = (f: FilaControl) => f.id;
  protected readonly etapaControl = (f: FilaControl) => f.estado;
  protected readonly columnas: ColumnaLista<FilaControl>[] = [
    { campo: 'folio', titulo: 'Folio', clase: 'fw-semibold text-primary' },
    { campo: 'producto', titulo: 'Producto', texto: f => (f.libre ? f.producto : etiquetaProducto(this.inv.catalogo, f.producto)) },
    { campo: 'orden', titulo: 'Orden de Fabricación' },
    { campo: 'proceso', titulo: 'Proceso' },
    { campo: 'lotes', titulo: 'Lotes', texto: f => String(f.lotes), clase: 'text-end' },
    { campo: 'estado', titulo: 'Estado', tipo: 'estado' },
  ];

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly agrupaciones = signal<string[]>([]);

  protected abrir(f: FilaControl): void {
    void this.router.navigateByUrl(`/calidad/${f.libre ? f.folio : f.orden}`);
  }
}
