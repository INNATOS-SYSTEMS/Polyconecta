import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { Component, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { n1 } from '../../../core/format/numero';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { ChatterEntry, OdooChatterDrawer } from '../../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { OdooSmartButtons, SmartButtonModel } from '../../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { OdooTabs } from '../../../shared/odoo-tabs/odoo-tabs';
import { CalidadAcciones } from '../calidad-acciones';
import { estadoQc, qcFolio } from '../calidad-estado';

/**
 * Réplica de Pages/CalidadFormView.razor sobre los componentes de la spec 011 (P4). Fallar un lote pide
 * confirmación (aclaración P4); aprobar es inmediato.
 */
@Component({
  selector: 'pc-calidad-form',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSmartButtons, OdooChatterDrawer, OdooTabs, OdooIcon, RouterLink],
  templateUrl: './calidad-form.html',
  styles: ':host { display: contents; }',
})
export class CalidadForm {
  protected readonly flow = inject(OperationalFlowState);
  private readonly router = inject(Router);
  protected readonly acciones = inject(CalidadAcciones);

  readonly folioOf = input('');

  protected readonly n1 = n1;
  protected readonly tab = signal<string>('controles');
  protected readonly pestanas = [{ id: 'controles', titulo: 'Controles' }, { id: 'notas', titulo: 'Notas' }];
  protected readonly chatterEntries: ChatterEntry[] = [{ author: 'Sistema', timestamp: 'hoy', text: 'Ficha de Control de Calidad generada.' }];

  /** Devuelve el mismo objeto mutado: sin equal:false no avisaría a sus dependientes. */
  protected readonly of = computed(() => {
    this.flow.cambios();
    return this.flow.getOrder(this.folioOf());
  }, { equal: () => false });

  protected readonly qcFolio = computed(() => qcFolio(this.folioOf()));
  protected readonly estado = computed(() => estadoQc(this.of()));
  protected readonly pendientes = computed(() => this.of()?.produccion.filter(l => l.estado === 'En revisión').length ?? 0);
  protected readonly cantidadPlaneadaPorLote = computed(() => {
    const of = this.of();
    return !of || of.numeroRollos === 0 ? 0 : of.cantidad / of.numeroRollos;
  });

  protected readonly smartButtons = computed<SmartButtonModel[]>(() => [
    { label: 'Orden de Fabricación', countBadge: 1, iconClass: 'fabricacion', targetRoute: `/fabricacion/${this.folioOf()}` },
  ]);

  protected navegar(ruta: string): void {
    void this.router.navigateByUrl(ruta);
  }

  protected aprobarTodo(): void {
    const lote = this.of()?.produccion.find(l => l.estado === 'En revisión');
    if (lote) this.flow.aprobarLote(lote);
  }

  protected fallarUltimo(): void {
    const lote = this.of()?.produccion.find(l => l.estado === 'En revisión');
    if (lote) void this.acciones.fallar(lote);
  }
}
