import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { Component, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { PEDIDO_FOLIO } from '../../../core/seed/flujo';
import { PedidoLibre } from '../../../core/state/libre/pedido-libre';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { ChatterEntry, OdooChatterDrawer } from '../../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { OdooSmartButtons, SmartButtonModel } from '../../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { OdooStatusPipeline } from '../../../shared/odoo-status-pipeline/odoo-status-pipeline';
import { PocSalesOrderForm } from '../../../shared/poc-sales-order-form/poc-sales-order-form';

/** Réplica de Pages/PedidoFormView.razor. */
@Component({
  selector: 'pc-pedido-form',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSmartButtons, OdooStatusPipeline, PocSalesOrderForm, OdooChatterDrawer],
  templateUrl: './pedido-form.html',
  styles: ':host { display: contents; }',
})
export class PedidoForm {
  protected readonly flow = inject(OperationalFlowState);
  protected readonly router = inject(Router);
  private readonly pedidoLibre = inject(PedidoLibre);
  protected readonly error = signal<string | undefined>(undefined);

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
    if (this.pedido().libre) {
      // FR-014: un pedido libre no tiene entrega ni OF hasta que se generen; nunca un origen falso.
      const ofs = this.flow.ordenesDePedido(this.pedido().folio).length;
      return [
        { label: 'Entrega', countBadge: 0, iconClass: 'bi bi-truck', targetRoute: '', deshabilitado: true },
        { label: 'Fabricación', countBadge: ofs, iconClass: 'bi bi-diagram-3', targetRoute: `/fabricacion?pedido=${this.pedido().folio}`, deshabilitado: ofs === 0 },
      ];
    }
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
    const entradas: ChatterEntry[] = this.pedido().libre
      ? [{ author: 'Sistema', timestamp: 'hoy', text: 'Pedido creado con Nuevo, sin sincronización de origen.' }]
      : [{ author: 'Sistema', timestamp: '17 Sep 26', text: 'Creación del pedido por Alejandro' }];
    for (const [rol, quien] of this.pedido().firmas) entradas.push({ author: 'Sistema', timestamp: 'hoy', text: `Autorización de ${rol} firmada por ${quien}` });
    return entradas;
  });

  protected confirmar(): void {
    // El pedido libre recibe su Contpaq ID simulado al confirmar (D-53).
    if (this.pedido().libre) this.error.set(this.pedidoLibre.confirmar(this.pedido().folio));
    else this.flow.setOrderStage('Confirmado', this.pedido().folio);
  }

  protected autorizar(): void {
    this.flow.autorizar(this.pedido().folio);
  }

  protected navegar(ruta: string): void {
    void this.router.navigateByUrl(ruta);
  }
}
