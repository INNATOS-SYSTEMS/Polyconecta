/** Línea del pedido de venta. Misma forma que las demás líneas capturables. */
export interface SalesOrderLine {
  clave: string;
  producto: string;
  cantidad: number;
  unidad: string;
  precioUnitario: number;
  /** Solo en pedidos libres (D-74). */
  moneda?: string;
}

export const subtotal = (l: SalesOrderLine): number => l.cantidad * l.precioUnitario;

export interface ProcessCheck {
  proceso: string;
  activo: boolean;
  origen: string;
  producto: string;
}

/**
 * Pedido de venta. En el prototipo es un solo documento dentro de OperationalFlowState
 * (CurrentOrderStage, PedidoLineas, Procesos, Firmas…); en la réplica es una colección cuyo primer
 * elemento es la semilla IV310-26 (research R-02 de la spec 001).
 */
export class SalesOrder {
  folio = '';
  cliente = '';
  stage = 'Borrador';
  ordenCompraCliente = '';
  agente = '';
  contpaqId = '';
  cantidadPedido = 0;
  lineas: SalesOrderLine[] = [];
  procesos: ProcessCheck[] = [];
  /** Firmas recogidas: rol → quién firmó. */
  firmas = new Map<string, string>();
  /** Creado con "Nuevo", sin origen (Principio X). */
  libre = false;

  constructor(init: Partial<SalesOrder> = {}) {
    Object.assign(this, init);
  }
}
