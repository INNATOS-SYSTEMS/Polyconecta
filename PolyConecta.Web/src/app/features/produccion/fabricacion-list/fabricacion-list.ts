import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { Component, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { n1, ordenCultural } from '../../../core/format/numero';
import { ManufacturingOrder } from '../../../core/models/produccion';
import { aplicar } from '../../../core/search/search-view';
import { FABRICACION } from '../../../core/search/views';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { UiViewState } from '../../../core/state/ui-view-state';
import { Crumb, OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooPager } from '../../../shared/odoo-pager/odoo-pager';
import { Facet, OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';
import { OdooViewSwitcher } from '../../../shared/odoo-view-switcher/odoo-view-switcher';

interface Nodo {
  of: ManufacturingOrder;
  nivel: number;
  hijos: Nodo[];
}

/** Réplica de Pages/FabricacionList.razor: la lista anidada por jerarquía de OF. */
@Component({
  selector: 'pc-fabricacion-list',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSearchPanel, OdooViewSwitcher, OdooPager],
  templateUrl: './fabricacion-list.html',
  styles: ':host { display: contents; }',
})
export class FabricacionList {
  private readonly flow = inject(OperationalFlowState);
  private readonly router = inject(Router);
  protected readonly viewState = inject(UiViewState);
  protected readonly vista = FABRICACION;
  protected readonly stages = ['Borrador', 'Planeado', 'En progreso', 'Hecho'];
  protected readonly n1 = n1;

  /** Filtro por pedido: el smart button del Pedido enlaza aquí con su folio (?pedido=). */
  readonly pedido = input<string | undefined>(undefined);

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly selected = signal(new Set<string>());

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

  protected readonly filteredOrders = computed(() => {
    this.flow.cambios();
    const p = this.pedido();
    return aplicar(this.vista, this.flow.manufacturingOrders.filter(o => !p || o.pedidoFolio === p), this.searchText(), this.filtros());
  });

  /** Recorrido en profundidad del árbol por originFolio: cada fila conoce su nivel. */
  protected readonly arbolPlano = computed<Nodo[]>(() => {
    const visibles = this.filteredOrders();
    const folios = new Set(visibles.map(o => o.folio));
    const porFolio = (a: ManufacturingOrder, b: ManufacturingOrder) => ordenCultural(a.folio, b.folio);
    const hijos = (folio: string, nivel: number): Nodo[] =>
      visibles.filter(o => o.originFolio === folio).sort(porFolio).map(o => ({ of: o, nivel, hijos: hijos(o.folio, nivel + 1) }));
    // Raíz: sin origen, o cuyo origen quedó fuera del filtro actual.
    const raices = visibles
      .filter(o => o.originFolio === undefined || !folios.has(o.originFolio))
      .sort(porFolio)
      .map(o => ({ of: o, nivel: 0, hijos: hijos(o.folio, 1) }));
    const plano: Nodo[] = [];
    const recorrer = (nodos: Nodo[]) => nodos.forEach(n => (plano.push(n), recorrer(n.hijos)));
    recorrer(raices);
    return plano;
  });

  protected enColumna(stage: string): ManufacturingOrder[] {
    return this.filteredOrders().filter(o => o.state === stage);
  }

  /** Abre la OF arrastrando el contexto: al volver, el breadcrumb regresa a esta lista filtrada. */
  protected abrir(folio: string): void {
    const p = this.pedido();
    void this.router.navigateByUrl(!p ? `/fabricacion/${folio}` : `/fabricacion/${folio}?pedido=${p}`);
  }

  protected toggleOne(folio: string): void {
    this.selected.update(s => {
      const n = new Set(s);
      if (!n.delete(folio)) n.add(folio);
      return n;
    });
  }

  protected toggleAll(event: Event): void {
    const check = (event.target as HTMLInputElement).checked;
    this.selected.set(new Set(check ? this.filteredOrders().map(o => o.folio) : []));
  }
}
