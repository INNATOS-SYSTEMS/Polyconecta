import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { aplicar } from '../../../core/search/search-view';
import { OPERACIONES } from '../../../core/search/views';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { StockOperationState } from '../../../core/state/stock-operation-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooPager } from '../../../shared/odoo-pager/odoo-pager';
import { OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';

/** Réplica de Pages/RecoleccionesList.razor. */
@Component({
  selector: 'pc-recolecciones-list',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSearchPanel, OdooPager],
  templateUrl: './recolecciones-list.html',
  styles: ':host { display: contents; }',
})
export class RecoleccionesList {
  private readonly ops = inject(StockOperationState);
  protected readonly router = inject(Router);
  protected readonly vista = OPERACIONES;

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly seleccionadas = signal(new Set<string>());

  constructor() {
    // Las recolecciones nacen con las Órdenes de Fabricación: inyectar el flujo garantiza que existan.
    inject(OperationalFlowState);
  }

  protected readonly filtradas = computed(() => {
    this.ops.cambios();
    return aplicar(this.vista, this.ops.operaciones, this.searchText(), this.filtros());
  });

  protected toggle(folio: string): void {
    this.seleccionadas.update(s => {
      const n = new Set(s);
      if (!n.delete(folio)) n.add(folio);
      return n;
    });
  }

  protected toggleAll(event: Event): void {
    const check = (event.target as HTMLInputElement).checked;
    this.seleccionadas.set(new Set(check ? this.filtradas().map(o => o.folio) : []));
  }
}
