import { Injectable, inject } from '@angular/core';
import { TransicionKanban } from '../../core/kanban/kanban';
import { OrigenHttp } from '../../core/lista/origen-http';
import { PedidosService, PedidoDetalleDto } from './pedidos.service';
import { FirmaPedido } from './firma-pedido';

/** Fila de la lista de pedidos proyectada por la API (contracts/api-listas.md, VistasDeF1.Pedidos). */
export interface FilaPedido {
  id: number | string;
  folio: string;
  cliente: string;
  fechaPromesa?: string | null;
  entrega?: Date | null;
  estado: string;
  producto?: string;
  cantidad?: string;
  rowVersion?: string;
  _filtros?: string[];
  [key: string]: unknown;
}

export const ETAPAS_PEDIDO = ['Borrador', 'Confirmado', 'Autorizado', 'Cancelado'];


/**
 * Acciones del pedido sobre la API (spec 003, L2-T024):
 * usadas por el formulario y el kanban para mantener una sola regla por transición.
 */
@Injectable({ providedIn: 'root' })
export class PedidosAcciones {
  private readonly pedidosService = inject(PedidosService);

  /** Origen HTTP sobre /api/v1/ventas/pedidos (D-151, D-155). */
  origen(): OrigenHttp<FilaPedido> {
    return new OrigenHttp<FilaPedido>({
      modulo: 'ventas',
      lista: 'pedidos',
      id: f => String(f.id),
    });
  }

  async confirmar(id: number | string, rowVersion?: string): Promise<PedidoDetalleDto> {
    if (!rowVersion) {
      const p = await this.pedidosService.obtener(id);
      rowVersion = p.rowVersion;
    }
    return this.pedidosService.confirmar(id, rowVersion);
  }

  async autorizar(id: number | string, rowVersion?: string, rol?: string | null): Promise<PedidoDetalleDto> {
    if (!rowVersion) {
      const p = await this.pedidosService.obtener(id);
      rowVersion = p.rowVersion;
    }
    return this.pedidosService.autorizar(id, rowVersion, rol);
  }

  async revocar(id: number | string, rowVersion?: string, motivo?: string | null): Promise<PedidoDetalleDto> {
    if (!rowVersion) {
      const p = await this.pedidosService.obtener(id);
      rowVersion = p.rowVersion;
    }
    return this.pedidosService.revocar(id, rowVersion, motivo);
  }

  async cancelar(id: number | string, rowVersion?: string, motivo?: string | null): Promise<PedidoDetalleDto> {
    if (!rowVersion) {
      const p = await this.pedidosService.obtener(id);
      rowVersion = p.rowVersion;
    }
    return this.pedidosService.cancelar(id, rowVersion, motivo);
  }

  /** Transiciones arrastrables en el kanban (contratos visuales §4.3, D-138). */
  transiciones(): TransicionKanban<FilaPedido>[] {
    return [
      {
        desde: 'Borrador',
        hacia: 'Confirmado',
        nombre: 'Confirmar',
        ejecutar: async f => {
          try {
            await this.confirmar(f.id, f.rowVersion);
            return undefined;
          } catch (e: unknown) {
            return (e as Error).message || 'Error al confirmar el pedido';
          }
        },
      },
      {
        desde: 'Confirmado',
        hacia: 'Autorizado',
        nombre: 'Autorizar',
        dialogo: FirmaPedido,
        ejecutar: async (f, res) => {
          try {
            const rol = typeof res === 'string' ? res : undefined;
            await this.autorizar(f.id, f.rowVersion, rol);
            return undefined;
          } catch (e: unknown) {
            return (e as Error).message || 'Error al autorizar el pedido';
          }
        },
      },
    ];
  }
}
