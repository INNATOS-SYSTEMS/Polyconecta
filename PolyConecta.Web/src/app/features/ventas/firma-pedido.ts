import { Component, OnInit, inject, signal } from '@angular/core';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { FormsModule } from '@angular/forms';
import { OdooDialog } from '../../shared/odoo-dialog/odoo-dialog';
import { PedidosService } from './pedidos.service';

/**
 * Diálogo de la transición Confirmado → Autorizado en el kanban:
 * muestra las firmas que faltan y pide el rol solo si el usuario puede firmar por los dos (D-33, L2-T024).
 */
@Component({
  selector: 'pc-firma-pedido',
  imports: [FormsModule, OdooDialog],
  template: `
    <pc-odoo-dialog
      titulo="Autorizar pedido"
      [textoPrimario]="cargando() ? 'Cargando...' : 'Firmar'"
      [primarioDeshabilitado]="cargando() || (roles().length === 0 && !puedeFirmar())"
      (confirmar)="confirmar()"
      (cancelar)="ref.close(false)"
    >
      @if (cargando()) {
        <div class="text-center py-3">
          <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
        </div>
      } @else if (error()) {
        <div class="alert alert-danger py-2 mb-0">{{ error() }}</div>
      } @else {
        <p class="mb-2">
          Pedido <strong>{{ folio() }}</strong>: {{ firmasCount() }} de 2 firmas.
        </p>
        <p class="mb-3">
          Firma pendiente: <strong data-firma-pendiente>{{ pendiente() }}</strong>.
        </p>

        @if (roles().length > 1) {
          <div class="mb-3">
            <label class="form-label small fw-semibold text-muted">Firmar en calidad de:</label>
            <select class="form-select form-select-sm" [(ngModel)]="rolSeleccionado" id="selector-rol-firma">
              @for (r of roles(); track r) {
                <option [value]="r">{{ r }}</option>
              }
            </select>
          </div>
        }
      }
    </pc-odoo-dialog>
  `,
})
export class FirmaPedido implements OnInit {
  protected readonly ref = inject<DialogRef<string | boolean>>(DialogRef);
  protected readonly datos = inject<{ fila: { id: string | number; folio: string } }>(DIALOG_DATA);
  private readonly pedidosService = inject(PedidosService);

  protected readonly cargando = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly folio = signal(this.datos.fila.folio);
  protected readonly firmasCount = signal(0);
  protected readonly pendiente = signal('—');
  protected readonly roles = signal<string[]>([]);
  protected readonly puedeFirmar = signal(true);
  protected rolSeleccionado = '';

  async ngOnInit(): Promise<void> {
    try {
      const p = await this.pedidosService.obtener(this.datos.fila.id);
      this.folio.set(p.folio);
      this.firmasCount.set(p.firmas.length);
      this.pendiente.set(p.firmasPendientes.join(', ') || '—');
      this.roles.set(p.rolesPorFirmar);
      if (p.rolesPorFirmar.length > 0) {
        this.rolSeleccionado = p.rolesPorFirmar[0];
      }
      const accionAut = p.acciones.find(a => a.accion === 'autorizar');
      if (accionAut && !accionAut.disponible) {
        this.puedeFirmar.set(false);
        this.error.set(accionAut.razon || 'No tiene permisos o condiciones para autorizar este pedido.');
      }
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al obtener datos del pedido.');
    } finally {
      this.cargando.set(false);
    }
  }

  protected confirmar(): void {
    if (this.roles().length > 1) {
      this.ref.close(this.rolSeleccionado);
    } else {
      this.ref.close(true);
    }
  }
}
