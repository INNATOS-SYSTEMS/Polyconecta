import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { PedidoLibre } from '../../../core/state/libre/pedido-libre';
import { HojaNueva } from '../../../shared/hoja-nueva/hoja-nueva';

/** "Nuevo" pedido de venta (FR-012, D-53): cliente y orden de compra; las líneas se capturan ya creado. */
@Component({
  selector: 'pc-pedido-nuevo',
  imports: [HojaNueva],
  template: `
    <pc-hoja-nueva lista="Pedidos" ruta="/pedidos" titulo="Pedido" [stages]="stages" [error]="error()" (guardar)="guardar()" (descartar)="router.navigateByUrl('/pedidos')">
      <div class="row g-4 mb-2">
        <div class="col-md-6">
          <div class="o_form_label_row"><span class="o_form_label">Cliente</span><input class="form-control form-control-sm o_inline_input" name="cliente" [value]="cliente()" (input)="cliente.set($any($event.target).value)" /></div>
        </div>
        <div class="col-md-6">
          <div class="o_form_label_row"><span class="o_form_label">Orden de Compra</span><input class="form-control form-control-sm o_inline_input" name="ordenCompra" [value]="ordenCompra()" (input)="ordenCompra.set($any($event.target).value)" /></div>
          <div class="o_form_label_row"><span class="o_form_label">Contpaq ID</span><span class="o_form_value text-muted">— (se asigna al confirmar)</span></div>
        </div>
      </div>
      <p class="small text-muted mb-0">Las líneas, con precio unitario y moneda, se capturan después de guardar.</p>
    </pc-hoja-nueva>
  `,
  styles: ':host { display: contents; }',
})
export class PedidoNuevo {
  private readonly libre = inject(PedidoLibre);
  protected readonly router = inject(Router);
  protected readonly stages = ['Borrador', 'Confirmado', 'Autorizado', 'En progreso', 'Hecho'];
  protected readonly cliente = signal('');
  protected readonly ordenCompra = signal('');
  protected readonly error = signal<string | undefined>(undefined);

  protected guardar(): void {
    const { pedido, error } = this.libre.crear(this.cliente(), this.ordenCompra());
    this.error.set(error);
    if (pedido) void this.router.navigateByUrl(`/pedidos/${pedido.folio}`);
  }
}
