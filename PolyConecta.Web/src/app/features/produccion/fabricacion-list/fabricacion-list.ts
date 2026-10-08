import { Component, computed, inject, input, signal, TemplateRef, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { fechaCampo, n1 } from '../../../core/format/numero';
import { FiltroLista } from '../../../core/lista/origen';
import { FABRICACION } from '../../../core/search/views';
import { UiViewState } from '../../../core/state/ui-view-state';
import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { Crumb, OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { OdooKanban } from '../../../shared/odoo-kanban/odoo-kanban';
import { ColumnaLista } from '../../../shared/odoo-list/columnas';
import { OdooList } from '../../../shared/odoo-list/odoo-list';
import { OdooPager } from '../../../shared/odoo-pager/odoo-pager';
import { Facet, OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';
import { OdooViewSwitcher } from '../../../shared/odoo-view-switcher/odoo-view-switcher';
import { ETAPAS_OF, FabricacionAcciones, FilaOf } from '../fabricacion-acciones';

/**
 * Réplica de Pages/FabricacionList.razor sobre los componentes de la spec 011 (P2): la lista conserva la
 * cadena de OF (cada hija debajo de su origen) y el kanban confirma y cierra al arrastrar.
 */
@Component({
  selector: 'pc-fabricacion-list',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSearchPanel, OdooViewSwitcher, OdooPager, OdooList, OdooKanban, OdooIcon],
  templateUrl: './fabricacion-list.html',
  styles: ':host { display: contents; }',
})
export class FabricacionList {
  private readonly router = inject(Router);
  private readonly acciones = inject(FabricacionAcciones);
  protected readonly viewState = inject(UiViewState);
  protected readonly vista = FABRICACION;
  protected readonly n1 = n1;
  protected readonly lista = viewChild(OdooList<FilaOf>);
  private readonly celdaFolio = viewChild.required<TemplateRef<{ $implicit: FilaOf }>>('celdaFolio');

  /** Filtro por pedido: el smart button del Pedido enlaza aquí con su folio (?pedido=). */
  readonly pedido = input<string | undefined>(undefined);

  protected readonly origen = this.acciones.origen(() => this.pedido());
  protected readonly transiciones = this.acciones.transiciones();
  protected readonly fechaCampo = fechaCampo;
  protected readonly etapas = ETAPAS_OF.map(e => ({ valor: e, titulo: e }));
  protected readonly idOf = (f: FilaOf) => f.id;
  protected readonly etapaOf = (f: FilaOf) => f.estado;

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly agrupaciones = signal<string[]>([]);
  /** El pedido del contexto llega como filtro de la consulta, para que la lista y el kanban se recarguen. */
  protected readonly filtroPedido = computed<FiltroLista[]>(() => (this.pedido() ? [{ campo: 'pedido', operador: 'igual', valor: this.pedido() }] : []));

  protected readonly columnas = computed<ColumnaLista<FilaOf>[]>(() => [
    { campo: 'folio', titulo: 'Folio', clase: 'fw-semibold text-primary', celda: this.celdaFolio() },
    { campo: 'producto', titulo: 'Producto' },
    { campo: 'proceso', titulo: 'Proceso' },
    { campo: 'cantidad', titulo: 'Cantidad', texto: f => `${n1(f.cantidad)} ${f.unidad}`, valor: f => f.cantidad, clase: 'text-end' },
    { campo: 'estado', titulo: 'Estado', tipo: 'estado' },
  ]);

  protected readonly breadcrumb = computed<Crumb[]>(() => {
    const p = this.pedido();
    return !p
      ? [{ label: 'Órdenes de Fabricación' }]
      : [{ label: 'Pedidos', url: '/pedidos' }, { label: p, url: `/pedidos/${p}` }, { label: 'Órdenes de Fabricación' }];
  });

  /** El contexto de navegación se expresa como faceta, igual que cualquier filtro. */
  protected readonly facetas = computed<Facet[]>(() => {
    const p = this.pedido();
    return !p ? [] : [{ campo: 'Pedido', valor: p, onRemove: () => void this.router.navigateByUrl('/fabricacion') }];
  });

  /** Abre la OF arrastrando el contexto: al volver, el breadcrumb regresa a esta lista filtrada. */
  protected abrir(f: FilaOf): void {
    const p = this.pedido();
    void this.router.navigateByUrl(!p ? `/fabricacion/${f.folio}` : `/fabricacion/${f.folio}?pedido=${p}`);
  }
}
