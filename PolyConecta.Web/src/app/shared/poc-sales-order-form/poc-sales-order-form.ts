import { Component, computed, inject, input, signal } from '@angular/core';
import { etiquetaProducto } from '../../core/format/producto-etiqueta';
import { n1, n2 } from '../../core/format/numero';
import { SalesOrderLine, subtotal } from '../../core/models/ventas';
import { PEDIDO_FOLIO } from '../../core/seed/flujo';
import { InventoryState } from '../../core/state/inventory-state';
import { PedidoLibre } from '../../core/state/libre/pedido-libre';
import { OperationalFlowState } from '../../core/state/operational-flow-state';
import { OdooIcon } from '../odoo-icon/odoo-icon';
import { LineDraft, OdooLineCapture, emptyDraft } from '../odoo-line-capture/odoo-line-capture';
import { OdooTabs, PcPestana } from '../odoo-tabs/odoo-tabs';

/**
 * Réplica de Components/Poc/PocSalesOrderForm.razor: cabecera, líneas y procesos del pedido. Se
 * construye en la fase 3A porque inyecta el estado (research R-04).
 */
@Component({
  selector: 'pc-poc-sales-order-form',
  imports: [OdooLineCapture, OdooTabs, PcPestana, OdooIcon],
  templateUrl: './poc-sales-order-form.html',
  styles: ':host { display: contents; }',
})
export class PocSalesOrderForm {
  protected readonly producto = (claveONombre?: string | null, nombre?: string) => etiquetaProducto(this.inv.catalogo, claveONombre, nombre);
  protected readonly pestanas = [{ id: 'detalle', titulo: 'Detalle' }];
  protected readonly flow = inject(OperationalFlowState);
  protected readonly inv = inject(InventoryState);
  private readonly pedidoLibre = inject(PedidoLibre);
  /** Motivo por el que no se guardó la última línea de un pedido libre. */
  protected readonly errorLinea = signal<string | undefined>(undefined);
  readonly folio = input(PEDIDO_FOLIO);

  protected readonly n1 = n1;
  protected readonly n2 = n2;
  protected readonly subtotalDe = subtotal;

  protected readonly showFichaTecnica = signal(false);
  protected readonly draftLinea = signal<LineDraft>(emptyDraft());

  protected readonly materialType = signal('Polietileno Baja Densidad (LDPE Termoencogible)');
  protected readonly rollTypeDescriptor = signal('Tubular 20.5 x 370');
  protected readonly gaugeMicron = signal('60.0');
  protected readonly kgPerRoll = signal('100.0');
  protected readonly treatment = signal('Sin tratamiento corona');
  protected readonly pigment = signal('Natural (sin pigmentar)');
  protected readonly additive = signal('Antibloqueo');
  protected readonly perforacion = signal('No');
  protected readonly impresion = signal('Sí');
  protected readonly targetProductionKg = signal('500.0');
  protected readonly targetProductionKgDesde = signal('490.0');

  /** Devuelve el mismo objeto mutado: sin equal:false no avisaría a sus dependientes. */
  protected readonly pedido = computed(() => {
    this.flow.cambios();
    return this.flow.pedido(this.folio());
  }, { equal: () => false });

  /** Autorizado en adelante el pedido ya generó documentos: sus líneas quedan cerradas. */
  protected readonly pedidoAbierto = computed(() => this.pedido().stage === 'Borrador' || this.pedido().stage === 'Confirmado');

  protected readonly lineas = computed(() => {
    this.flow.cambios();
    return [...this.pedido().lineas];
  });

  protected readonly subtotal = computed(() => this.lineas().reduce((t, l) => t + subtotal(l), 0));
  protected readonly iva = computed(() => this.subtotal() * 0.16);
  protected readonly total = computed(() => this.subtotal() + this.iva());

  protected existencia(clave: string): number {
    this.flow.cambios();
    this.inv.cambios();
    return this.inv.disponible(clave);
  }

  protected editarLinea(linea: SalesOrderLine): void {
    this.draftLinea.set({ clave: linea.clave, producto: linea.producto, cantidad: linea.cantidad, unidad: linea.unidad, precioUnitario: linea.precioUnitario, moneda: linea.moneda });
    this.flow.quitarLineaPedido(linea, this.folio());
  }

  protected agregarLinea(d: LineDraft): void {
    if (this.pedido().libre) {
      const error = this.pedidoLibre.agregarLinea(this.folio(), d.clave, d.cantidad, d.precioUnitario ?? 0, d.moneda ?? 'MXN');
      this.errorLinea.set(error);
      if (!error) this.draftLinea.set(emptyDraft());
      return;
    }
    this.flow.agregarLineaPedido(d.clave, d.producto, d.cantidad, d.unidad, this.folio());
    this.draftLinea.set(emptyDraft());
  }

  protected procesoLabel(proceso: string): string {
    return proceso === 'Extrusion' ? 'Extrusión' : proceso === 'Impresion' ? 'Impresión' : proceso;
  }

  protected valor(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }
}
