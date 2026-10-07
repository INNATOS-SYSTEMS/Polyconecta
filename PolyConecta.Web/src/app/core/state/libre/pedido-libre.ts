import { Injectable, inject } from '@angular/core';
import { SalesOrder } from '../../models/ventas';
import { InventoryState } from '../inventory-state';
import { OperationalFlowState } from '../operational-flow-state';
import { crearLineaLibre, siguienteFolio } from './linea-libre';

export const MONEDAS = ['MXN', 'USD'] as const;

/**
 * Pedido de venta libre (D-53, D-74): nace en PolyConecta, sin sincronización, con cliente y líneas
 * con precio unitario y moneda. Al confirmarlo recibe un Contpaq ID simulado, como si el bridge lo
 * hubiera dado de alta, y sigue el mismo flujo de dos firmas que el pedido semilla.
 */
@Injectable({ providedIn: 'root' })
export class PedidoLibre {
  private readonly flow = inject(OperationalFlowState);
  private readonly inv = inject(InventoryState);

  crear(cliente: string, ordenCompra = ''): { pedido?: SalesOrder; error?: string } {
    if (cliente.trim() === '') return { error: 'Capture el cliente.' };
    const pedido = new SalesOrder({
      folio: siguienteFolio('PV', this.flow.pedidos.map(p => p.folio)),
      cliente: cliente.trim(),
      ordenCompraCliente: ordenCompra.trim(),
      agente: 'Administrator',
      libre: true,
    });
    this.flow.pedidos.push(pedido);
    this.flow.notificar();
    return { pedido };
  }

  agregarLinea(folio: string, clave: string, cantidad: number, precioUnitario: number, moneda: string): string | undefined {
    const pedido = this.flow.pedido(folio);
    if (!pedido.libre || pedido.folio !== folio) return 'Solo un pedido libre captura precio.';
    const { linea, error } = crearLineaLibre(this.inv.getProducto(clave), cantidad);
    if (error) return error;
    if (!Number.isFinite(precioUnitario) || precioUnitario <= 0) return 'El precio unitario debe ser mayor que cero.';
    if (!(MONEDAS as readonly string[]).includes(moneda)) return `Moneda no válida: ${moneda}.`;
    pedido.lineas.push({ ...linea!, precioUnitario, moneda });
    this.flow.notificar();
    return undefined;
  }

  /** Confirma el pedido libre y le asigna el Contpaq ID simulado (D-53). */
  confirmar(folio: string): string | undefined {
    const pedido = this.flow.pedido(folio);
    if (pedido.folio !== folio || !pedido.libre) return 'No es un pedido libre.';
    if (pedido.stage !== 'Borrador') return 'El pedido ya fue confirmado.';
    if (pedido.lineas.length === 0) return 'Agregue al menos una línea antes de confirmar.';
    pedido.contpaqId = this.siguienteContpaqId();
    this.flow.setOrderStage('Confirmado', folio);
    return undefined;
  }

  /** El simulador numera después del mayor ID que ya existe, como haría CONTPAQi. */
  private siguienteContpaqId(): string {
    const max = this.flow.pedidos.reduce((m, p) => Math.max(m, Number.parseInt(p.contpaqId, 10) || 0), 0);
    return String(max + 1);
  }
}
