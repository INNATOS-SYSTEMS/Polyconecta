import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { Component, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { PEDIDO_FOLIO } from '../../../core/seed/flujo';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { PedidosAcciones } from '../pedidos-acciones';
import { ChatterEntry, OdooChatterDrawer } from '../../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { OdooSmartButtons, SmartButtonModel } from '../../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { OdooStatusPipeline } from '../../../shared/odoo-status-pipeline/odoo-status-pipeline';
import { PocSalesOrderForm } from '../../../shared/poc-sales-order-form/poc-sales-order-form';

/** Réplica de Pages/PedidoFormView.razor. */
@Component({
  selector: 'pc-pedido-form',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSmartButtons, OdooStatusPipeline, PocSalesOrderForm, OdooChatterDrawer, OdooIcon],
  templateUrl: './pedido-form.html',
  styles: ':host { display: contents; }',
})
export class PedidoForm {
  protected readonly flow = inject(OperationalFlowState);
  protected readonly router = inject(Router);
  private readonly acciones = inject(PedidosAcciones);
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
        { label: 'Entrega', countBadge: 0, iconClass: 'entrega', targetRoute: '', deshabilitado: true },
        { label: 'Fabricación', countBadge: ofs, iconClass: 'fabricacion', targetRoute: `/fabricacion?pedido=${this.pedido().folio}`, deshabilitado: ofs === 0 },
      ];
    }
    return [
      { label: 'Entrega', countBadge: 1, iconClass: 'entrega', targetRoute: `/entregas/${this.flow.entrega().folio}` },
      {
        label: 'Fabricación',
        countBadge: this.flow.ordenesDePedido(this.folio()).length,
        iconClass: 'fabricacion',
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

  /** Las mismas acciones que el kanban (PedidosAcciones): una sola regla por transición. */
  protected confirmar(): void {
    this.error.set(this.acciones.confirmar(this.pedido().folio));
  }

  protected autorizar(): void {
    // La firma pendiente no es un error en el formulario: el botón muestra "1/2".
    this.acciones.autorizar(this.pedido().folio);
  }

  protected navegar(ruta: string): void {
    void this.router.navigateByUrl(ruta);
  }
}
