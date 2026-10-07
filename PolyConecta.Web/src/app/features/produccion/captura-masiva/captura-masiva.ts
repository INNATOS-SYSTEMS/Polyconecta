import { Component, computed, inject, signal } from '@angular/core';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { Router } from '@angular/router';
import { n0 } from '../../../core/format/numero';
import { aplicar } from '../../../core/search/search-view';
import { FABRICACION } from '../../../core/search/views';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooPager } from '../../../shared/odoo-pager/odoo-pager';
import { OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';
import { OdooViewSwitcher } from '../../../shared/odoo-view-switcher/odoo-view-switcher';

/** Réplica de Pages/CapturaMasivaPage.razor: kg por rollo de las OF de Extrusión. */
@Component({
  selector: 'pc-captura-masiva',
  imports: [OdooBreadcrumb, OdooSearchPanel, OdooViewSwitcher, OdooPager, OdooIcon],
  templateUrl: './captura-masiva.html',
  styles: ':host { display: contents; }',
})
export class CapturaMasiva {
  private readonly flow = inject(OperationalFlowState);
  protected readonly router = inject(Router);
  protected readonly vista = FABRICACION;
  protected readonly n0 = n0;

  /** Columnas de captura de kg por rollo (R1..R15), fijas. */
  protected readonly rollos = Array.from({ length: 15 }, (_, i) => i);

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly selected = signal(new Set<string>());

  /** La captura masiva es exclusiva de la producción de Extrusión. */
  protected readonly filteredOrders = computed(() => {
    this.flow.cambios();
    return aplicar(this.vista, this.flow.manufacturingOrders.filter(o => o.processType === 'Extrusion'), this.searchText(), this.filtros());
  });

  protected stateBadgeClass(state: string): string {
    return state === 'En progreso' ? 'o_badge_progreso' : state === 'Planeado' ? 'o_badge_planeado' : state === 'Hecho' ? 'o_badge_hecho' : 'badge-brand';
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
