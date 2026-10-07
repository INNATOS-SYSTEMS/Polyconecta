import { Component, computed, inject, signal } from '@angular/core';
import { fechaGuion } from '../../../core/format/numero';
import { aplicar } from '../../../core/search/search-view';
import { INCIDENCIAS } from '../../../core/search/views';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';
import { OdooViewSwitcher } from '../../../shared/odoo-view-switcher/odoo-view-switcher';

/** Réplica de Pages/IncidenciasPage.razor. Ya es libre en el prototipo (FR-012). */
@Component({
  selector: 'pc-incidencias',
  imports: [OdooBreadcrumb, OdooSearchPanel, OdooViewSwitcher],
  templateUrl: './incidencias.html',
  styles: ':host { display: contents; }',
})
export class Incidencias {
  private readonly flow = inject(OperationalFlowState);
  protected readonly vista = INCIDENCIAS;
  protected readonly fechaGuion = fechaGuion;

  protected readonly centro = signal('');
  protected readonly tipo = signal('');
  protected readonly comentarios = signal('');
  protected readonly horaInicio = signal('');
  protected readonly horaFin = signal('');
  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);

  protected readonly filteredIncidencias = computed(() => {
    this.flow.cambios();
    return aplicar(this.vista, this.flow.incidencias, this.searchText(), this.filtros());
  });

  protected valor(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected agregar(): void {
    if (this.centro().trim() === '' || this.tipo().trim() === '') return;
    const hoy = new Date();
    this.flow.incidencias.push({
      fecha: new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate()),
      centroTrabajo: this.centro(),
      tipo: this.tipo(),
      comentarios: this.comentarios(),
      horaInicio: this.horaInicio(),
      horaFin: this.horaFin(),
    });
    for (const s of [this.centro, this.tipo, this.comentarios, this.horaInicio, this.horaFin]) s.set('');
    this.flow.cambios.update(n => n + 1);
  }
}
