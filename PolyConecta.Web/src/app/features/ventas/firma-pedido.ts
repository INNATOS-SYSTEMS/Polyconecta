import { Component, inject } from '@angular/core';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { OperationalFlowState } from '../../core/state/operational-flow-state';
import { OdooDialog } from '../../shared/odoo-dialog/odoo-dialog';

/**
 * Diálogo de la transición Confirmado → Autorizado en el kanban: muestra la firma que falta y la
 * registra al confirmar (D-33). Sin sesión todavía, la firma se atribuye al rol pendiente, como el botón.
 */
@Component({
  selector: 'pc-firma-pedido',
  imports: [OdooDialog],
  template: `
    <pc-odoo-dialog titulo="Autorizar pedido" textoPrimario="Firmar" (confirmar)="ref.close(true)" (cancelar)="ref.close(false)">
      <p class="mb-1">Pedido <strong>{{ datos.fila.folio }}</strong>: {{ firmas }} de 2 firmas.</p>
      <p class="mb-0">Firma pendiente: <strong data-firma-pendiente>{{ pendiente }}</strong>.</p>
    </pc-odoo-dialog>
  `,
})
export class FirmaPedido {
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  protected readonly datos = inject<{ fila: { folio: string } }>(DIALOG_DATA);
  private readonly flow = inject(OperationalFlowState);
  protected readonly firmas = this.flow.firmasRecogidas(this.datos.fila.folio);
  protected readonly pendiente = this.flow.firmaPendiente(this.datos.fila.folio) ?? '—';
}
