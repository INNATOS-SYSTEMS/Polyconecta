import { Component, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { aplicar } from '../../../core/search/search-view';
import { LogisticsRow } from '../../../core/search/views';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooPager } from '../../../shared/odoo-pager/odoo-pager';
import { OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';
import { CONFIG, TipoLogistica } from './tipos';

/** Réplica de Pages/TrasladosList, RecepcionList y EntregasList (.razor), sobre sus colecciones (R-02). */
@Component({
  selector: 'pc-logistica-list',
  imports: [OdooBreadcrumb, OdooSearchPanel, OdooPager],
  templateUrl: './logistica-list.html',
  styles: ':host { display: contents; }',
})
export class LogisticaList {
  private readonly flow = inject(OperationalFlowState);
  protected readonly router = inject(Router);

  /** Llega del data de la ruta. */
  readonly tipo = input.required<TipoLogistica>();
  protected readonly cfg = computed(() => CONFIG[this.tipo()]);

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly seleccionadas = signal(new Set<string>());

  protected readonly filas = computed<LogisticsRow[]>(() => {
    this.flow.cambios();
    const filas = this.cfg()
      .documentos(this.flow)
      .map(d => ({ folio: d.folio, operacion: d.operacion, origen: d.origen, destino: d.destino, estado: d.state }));
    return aplicar(this.cfg().vista, filas, this.searchText(), this.filtros());
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
    this.seleccionadas.set(new Set(check ? this.filas().map(f => f.folio) : []));
  }
}
