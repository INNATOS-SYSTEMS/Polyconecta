import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { n1, n2 } from '../../../core/format/numero';
import { SalesOrderLine, subtotal } from '../../../core/models/ventas';
import { InventoryState } from '../../../core/state/inventory-state';
import { PedidoLibre } from '../../../core/state/libre/pedido-libre';
import { HojaNueva } from '../../../shared/hoja-nueva/hoja-nueva';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { emptyDraft, LineDraft, OdooLineCapture } from '../../../shared/odoo-line-capture/odoo-line-capture';
import { OdooTabs, PcPestana } from '../../../shared/odoo-tabs/odoo-tabs';
import { ETAPAS_PEDIDO } from '../pedidos-acciones';

/**
 * "Nuevo" pedido de venta (FR-012, D-53) con la estructura completa del documento ligado (D-136):
 * etapas, maestro, pestaña Detalle con la captura de líneas y chatter, que se activa al guardar.
 * Maestro y líneas se guardan juntos.
 */
@Component({
  selector: 'pc-pedido-nuevo',
  imports: [HojaNueva, OdooTabs, PcPestana, OdooLineCapture, OdooIcon],
  template: `
    <pc-hoja-nueva lista="Pedidos" ruta="/pedidos" titulo="Pedido" [stages]="stages" [error]="error()" [conChatter]="true"
                   (guardar)="guardar()" (descartar)="router.navigateByUrl('/pedidos')">
      <div class="row g-4 mb-2">
        <div class="col-md-6">
          <div class="o_form_label_row"><span class="o_form_label">Cliente</span><input class="form-control form-control-sm o_inline_input" name="cliente" [value]="cliente()" (input)="cliente.set($any($event.target).value)" /></div>
        </div>
        <div class="col-md-6">
          <div class="o_form_label_row"><span class="o_form_label">Orden de Compra</span><input class="form-control form-control-sm o_inline_input" name="ordenCompra" [value]="ordenCompra()" (input)="ordenCompra.set($any($event.target).value)" /></div>
          <div class="o_form_label_row"><span class="o_form_label">Contpaq ID</span><span class="o_form_value text-muted">— (se asigna al confirmar)</span></div>
        </div>
      </div>
      <pc-odoo-tabs [pestanas]="pestanas">
        <ng-template pcPestana="detalle">
          <pc-odoo-line-capture [(draft)]="borrador" [catalogo]="inv.catalogo" [unidadFija]="true" [conPrecio]="true" (submitted)="agregar($event)" />
          @if (errorLinea()) {
            <div class="alert alert-danger py-2 px-3 small mb-3"><pc-odoo-icon nombre="hard-stop" />{{ errorLinea() }}</div>
          }
          <table class="table align-middle mb-0" data-lineas-nuevas>
            <thead>
              <tr class="text-muted small">
                <th style="width:15%;">Clave</th><th>Producto</th><th class="text-center" style="width:110px;">Cantidad</th>
                <th class="text-center">Unidad</th><th class="text-end">Precio Unitario</th><th class="text-end">Subtotal</th><th></th>
              </tr>
            </thead>
            <tbody>
              @for (l of lineas(); track $index) {
                <tr>
                  <td class="fw-semibold text-primary small">{{ l.clave }}</td>
                  <td class="small">{{ l.producto }}</td>
                  <td class="text-center">{{ n1(l.cantidad) }}</td>
                  <td class="text-center small text-muted">{{ l.unidad }}</td>
                  <td class="text-end text-muted">\${{ n2(l.precioUnitario) }} {{ l.moneda }}</td>
                  <td class="text-end fw-semibold">\${{ n2(subtotal(l)) }}</td>
                  <td class="text-center"><button type="button" class="btn o_btn_icon" title="Eliminar" (click)="quitar($index)"><pc-odoo-icon nombre="quitar-linea" contexto="icono" /></button></td>
                </tr>
              } @empty {
                <tr><td colspan="7" class="small text-muted">Sin líneas. Se pueden agregar ahora o después de guardar.</td></tr>
              }
            </tbody>
          </table>
          <p class="small text-muted mt-2 mb-0">IVA, descuentos y totales los calcula CONTPAQi al dar de alta el pedido (D-74).</p>
        </ng-template>
      </pc-odoo-tabs>
    </pc-hoja-nueva>
  `,
  styles: ':host { display: contents; }',
})
export class PedidoNuevo {
  private readonly libre = inject(PedidoLibre);
  protected readonly inv = inject(InventoryState);
  protected readonly router = inject(Router);
  protected readonly stages = ETAPAS_PEDIDO;
  protected readonly pestanas = [{ id: 'detalle', titulo: 'Detalle' }];
  protected readonly cliente = signal('');
  protected readonly ordenCompra = signal('');
  protected readonly error = signal<string | undefined>(undefined);
  protected readonly errorLinea = signal<string | undefined>(undefined);
  protected readonly lineas = signal<SalesOrderLine[]>([]);
  protected borrador: LineDraft = emptyDraft();
  protected readonly n1 = n1;
  protected readonly n2 = n2;
  protected readonly subtotal = subtotal;

  /** Valida la línea con las mismas reglas del pedido libre y la guarda en el borrador (todavía no hay pedido). */
  protected agregar(d: LineDraft): void {
    const { linea, error } = this.libre.validarLinea(d.clave, d.cantidad, d.precioUnitario ?? 0, d.moneda ?? 'MXN');
    this.errorLinea.set(error);
    if (!linea) return;
    this.lineas.update(ls => [...ls, linea]);
    this.borrador = emptyDraft();
  }

  protected quitar(i: number): void {
    this.lineas.update(ls => ls.filter((_, j) => j !== i));
  }

  protected guardar(): void {
    const { pedido, error } = this.libre.crearConLineas(this.cliente(), this.ordenCompra(),
      this.lineas().map(l => ({ clave: l.clave, cantidad: l.cantidad, precioUnitario: l.precioUnitario, moneda: l.moneda ?? 'MXN' })));
    this.error.set(error);
    if (pedido) void this.router.navigateByUrl(`/pedidos/${pedido.folio}`);
  }
}
