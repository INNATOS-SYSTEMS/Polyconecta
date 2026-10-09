import { computed, signal } from '@angular/core';
import {
  AgenteVentaDto,
  ClienteBusquedaDto,
  DatosPedidoInputDto,
  DomicilioEnvioDto,
  PedidoDetalleDto,
} from '../pedidos.service';

export interface LineaBorrador {
  id?: number | null;
  productoId: number;
  clave: string;
  producto: string;
  unidad: string;
  cantidad: number;
  precioUnitario: number | null;
}

/** Monedas que el bridge sabe traducir (spec 003, "Moneda por pedido"). */
export const MONEDAS_PEDIDO = ['MXN', 'USD'];

const iso = (d: Date | null): string | null =>
  d ? `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}` : null;

const fecha = (s: string | null | undefined): Date | null => {
  if (!s) return null;
  const [a, m, d] = s.substring(0, 10).split('-').map(Number);
  return new Date(a, m - 1, d);
};

/**
 * Lo capturable de un pedido, en edición en su lugar (D-164): el maestro y las líneas se editan mientras
 * el estado lo permite y "Guardar" aparece solo con cambios. El domicilio de entrega no se elige: es el
 * primer domicilio de envío del cliente (D-160). Sin tipo de cambio (D-161).
 */
export class BorradorPedido {
  readonly cliente = signal<ClienteBusquedaDto | null>(null);
  readonly ordenCompra = signal('');
  readonly agente = signal<AgenteVentaDto | null>(null);
  readonly fechaPedido = signal<Date | null>(new Date());
  readonly fechaPromesa = signal<Date | null>(null);
  readonly moneda = signal('MXN');
  readonly lineas = signal<LineaBorrador[]>([]);
  readonly sucio = signal(false);

  /** Domicilio guardado del pedido, mientras no cambie el cliente. */
  private readonly domicilioGuardado = signal<DomicilioEnvioDto | null>(null);
  private clienteOriginalId: number | null = null;

  readonly domicilio = computed<DomicilioEnvioDto | null>(() => {
    const c = this.cliente();
    if (c && c.id === this.clienteOriginalId && this.domicilioGuardado()) return this.domicilioGuardado();
    return c?.domiciliosEnvio?.[0] ?? null;
  });

  readonly subtotal = computed(() => this.lineas().reduce((t, l) => t + l.cantidad * (l.precioUnitario ?? 0), 0));

  /** Carga el pedido guardado; los catálogos completan cliente y agente cuando llegan. */
  cargar(p: PedidoDetalleDto, clientes: readonly ClienteBusquedaDto[], agentes: readonly AgenteVentaDto[]): void {
    this.clienteOriginalId = p.cliente.id;
    this.domicilioGuardado.set(p.domicilioEntrega ?? null);
    this.cliente.set(
      clientes.find(c => c.id === p.cliente.id) ??
        { id: p.cliente.id, clave: p.cliente.clave, nombre: p.cliente.nombre, etiqueta: p.cliente.nombre, domiciliosEnvio: [] },
    );
    this.ordenCompra.set(p.ordenCompraCliente ?? '');
    this.agente.set(
      p.agente
        ? agentes.find(a => a.id === p.agente!.id) ??
            { id: p.agente.id, clave: p.agente.clave, nombre: p.agente.nombre, etiqueta: p.agente.nombre, tipo: '' }
        : null,
    );
    this.fechaPedido.set(fecha(p.fechaPedido));
    this.fechaPromesa.set(fecha(p.fechaPromesa));
    this.moneda.set(p.moneda);
    this.lineas.set(
      p.lineas.map(l => ({
        id: l.id, productoId: l.productoId, clave: l.clave, producto: l.producto, unidad: l.unidad,
        cantidad: l.cantidad, precioUnitario: l.precioUnitario,
      })),
    );
    this.sucio.set(false);
  }

  /** Al elegir cliente se proponen su moneda y su primer domicilio de envío (D-146, D-160). */
  elegirCliente(c: ClienteBusquedaDto | null): void {
    this.cliente.set(c);
    if (c?.moneda) this.moneda.set(c.moneda);
    this.marcar();
  }

  marcar(): void {
    this.sucio.set(true);
  }

  payload(): DatosPedidoInputDto {
    return {
      clienteId: this.cliente()?.id ?? 0,
      ordenCompraCliente: this.ordenCompra().trim() || null,
      agenteId: this.agente()?.id ?? null,
      fechaPedido: iso(this.fechaPedido()),
      fechaPromesa: iso(this.fechaPromesa()),
      domicilioEntregaId: this.domicilio()?.id ?? null,
      moneda: this.moneda(),
      lineas: this.lineas().map(l => ({
        id: l.id ?? null,
        productoId: l.productoId,
        cantidad: l.cantidad,
        precioUnitario: l.precioUnitario,
        metaProduccionKg: null,
        toleranciaPorcentaje: null,
      })),
    };
  }
}
