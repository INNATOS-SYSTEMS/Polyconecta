import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { aplicar } from '../../../core/search/search-view';
import { CALIDAD } from '../../../core/search/views';
import { CalidadLibre } from '../../../core/state/libre/calidad-libre';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooPager } from '../../../shared/odoo-pager/odoo-pager';
import { OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';
import { estadoQc, qcFolio } from '../calidad-estado';

/** Réplica de Pages/CalidadList.razor: un control por cada OF que requiere calidad. */
@Component({
  selector: 'pc-calidad-list',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSearchPanel, OdooPager],
  templateUrl: './calidad-list.html',
  styles: ':host { display: contents; }',
})
export class CalidadList {
  private readonly flow = inject(OperationalFlowState);
  protected readonly router = inject(Router);
  protected readonly vista = CALIDAD;
  protected readonly qcFolio = qcFolio;
  protected readonly estadoQc = estadoQc;

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly selected = signal(new Set<string>());

  protected readonly filteredOrders = computed(() => {
    this.flow.cambios();
    return aplicar(this.vista, this.flow.manufacturingOrders.filter(o => o.calidadRequerida), this.searchText(), this.filtros());
  });

  protected readonly qc = inject(CalidadLibre);

  /**
   * Controles creados con "Nuevo" (FR-012). No tienen OF, así que los filtros de la vista (proceso,
   * resultado) no les aplican: se muestran sin filtros y se buscan por folio o lote.
   */
  protected readonly controlesLibres = computed(() => {
    this.qc.cambios();
    this.flow.cambios();
    if (this.filtros().length > 0) return [];
    const texto = this.searchText().trim().toLowerCase();
    return this.qc.controles.filter(c => texto === '' || c.folio.toLowerCase().includes(texto) || c.lotes.some(l => l.lote.lote.toLowerCase().includes(texto)));
  });

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
