import { Component, computed, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { PEDIDO_FOLIO } from '../../../core/seed/flujo';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { ChatterEntry, OdooChatterDrawer } from '../../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { OdooSmartButtons, SmartButtonModel } from '../../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { OdooStatusPipeline } from '../../../shared/odoo-status-pipeline/odoo-status-pipeline';
import { PocSalesOrderForm } from '../../../shared/poc-sales-order-form/poc-sales-order-form';

/** Réplica de Pages/PedidoFormView.razor. */
@Component({
  selector: 'pc-pedido-form',
  imports: [OdooBreadcrumb, OdooSmartButtons, OdooStatusPipeline, PocSalesOrderForm, OdooChatterDrawer],
  templateUrl: './pedido-form.html',
  styles: ':host { display: contents; }',
})
export class PedidoForm {
  protected readonly flow = inject(OperationalFlowState);
  protected readonly router = inject(Router);

  /** Llega de la ruta /pedidos/:folio. */
  readonly folio = input(PEDIDO_FOLIO);

  protected readonly stages = ['Borrador', 'Confirmado', 'Autorizado', 'En progreso', 'Hecho'];

  /** Devuelve el mismo objeto mutado: sin equal:false no avisaría a sus dependientes. */
  protected readonly pedido = computed(() => {
    this.flow.cambios();
    return this.flow.pedido(this.folio());
  }, { equal: () => false });

  protected readonly firmas = computed(() => {
    this.flow.cambios();
    return this.flow.firmasRecogidas(this.pedido().folio);
  });

  protected readonly smartButtons = computed<SmartButtonModel[]>(() => {
    this.flow.cambios();
    return [
      { label: 'Entrega', countBadge: 1, iconClass: 'bi bi-truck', targetRoute: `/entregas/${this.flow.entrega().folio}` },
      {
        label: 'Fabricación',
        countBadge: this.flow.ordenesDePedido(this.folio()).length,
        iconClass: 'bi bi-diagram-3',
        targetRoute: `/fabricacion?pedido=${this.folio()}`,
      },
    ];
  });

  protected readonly chatterEntries = computed<ChatterEntry[]>(() => {
    this.flow.cambios();
    const entradas: ChatterEntry[] = [{ author: 'Sistema', timestamp: '17 Sep 26', text: 'Creación del pedido por Alejandro' }];
    for (const [rol, quien] of this.pedido().firmas) entradas.push({ author: 'Sistema', timestamp: 'hoy', text: `Autorización de ${rol} firmada por ${quien}` });
    return entradas;
  });

  protected confirmar(): void {
    this.flow.setOrderStage('Confirmado', this.pedido().folio);
  }

  protected autorizar(): void {
    this.flow.autorizar(this.pedido().folio);
  }

  protected navegar(ruta: string): void {
    void this.router.navigateByUrl(ruta);
  }
}
