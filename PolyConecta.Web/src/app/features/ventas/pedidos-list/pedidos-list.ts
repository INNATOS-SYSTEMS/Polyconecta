import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { n1 } from '../../../core/format/numero';
import { SalesOrder } from '../../../core/models/ventas';
import { aplicar } from '../../../core/search/search-view';
import { PEDIDOS, SalesOrderRow } from '../../../core/search/views';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { UiViewState } from '../../../core/state/ui-view-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooPager } from '../../../shared/odoo-pager/odoo-pager';
import { OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';
import { OdooViewSwitcher } from '../../../shared/odoo-view-switcher/odoo-view-switcher';

interface Fila {
  pedido: SalesOrder;
  row: SalesOrderRow;
  cantidad: string;
}

/** Réplica de Pages/PedidosList.razor, sobre la colección de pedidos (R-02). */
@Component({
  selector: 'pc-pedidos-list',
  imports: [OdooBreadcrumb, OdooSearchPanel, OdooViewSwitcher, OdooPager],
  templateUrl: './pedidos-list.html',
  styles: ':host { display: contents; }',
})
export class PedidosList {
  private readonly flow = inject(OperationalFlowState);
  private readonly router = inject(Router);
  protected readonly viewState = inject(UiViewState);
  protected readonly vista = PEDIDOS;
  protected readonly stages = ['Borrador', 'Confirmado', 'Autorizado', 'En progreso', 'Hecho'];

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly seleccionados = signal(new Set<string>());

  protected readonly filas = computed<Fila[]>(() => {
    this.flow.cambios();
    const todas = this.flow.pedidos.map(p => {
      const linea = p.lineas[0];
      return {
        pedido: p,
        row: { folio: p.folio, cliente: p.cliente, producto: linea?.clave ?? '', estado: p.stage },
        cantidad: linea ? `${n1(linea.cantidad)} ${linea.unidad}` : '',
      };
    });
    const visibles = new Set(aplicar(this.vista, todas.map(f => f.row), this.searchText(), this.filtros()));
    return todas.filter(f => visibles.has(f.row));
  });

  protected enColumna(stage: string): Fila[] {
    return this.filas().filter(f => f.pedido.stage === stage);
  }

  protected toggleAll(event: Event): void {
    const marcado = (event.target as HTMLInputElement).checked;
    this.seleccionados.set(new Set(marcado ? this.filas().map(f => f.pedido.folio) : []));
  }

  protected toggle(folio: string): void {
    this.seleccionados.update(s => {
      const n = new Set(s);
      if (!n.delete(folio)) n.add(folio);
      return n;
    });
  }

  protected abrir(folio: string): void {
    void this.router.navigateByUrl(`/pedidos/${folio}`);
  }
}
