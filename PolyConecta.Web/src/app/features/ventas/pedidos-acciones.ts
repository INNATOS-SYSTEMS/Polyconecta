import { Injectable, inject } from '@angular/core';
import { TransicionKanban } from '../../core/kanban/kanban';
import { OrigenEnMemoria } from '../../core/lista/origen-en-memoria';
import { n1 } from '../../core/format/numero';
import { SalesOrder } from '../../core/models/ventas';
import { PEDIDOS, SalesOrderRow } from '../../core/search/views';
import { PedidoLibre } from '../../core/state/libre/pedido-libre';
import { OperationalFlowState } from '../../core/state/operational-flow-state';
import { FirmaPedido } from './firma-pedido';

/** Fila de la lista de pedidos: la de la vista de búsqueda más lo que muestra la tabla. */
export interface FilaPedido extends SalesOrderRow {
  id: string;
  cantidad: string;
  pedido: SalesOrder;
}

export const ETAPAS_PEDIDO = ['Borrador', 'Confirmado', 'Autorizado', 'En progreso', 'Hecho'];

/**
 * Acciones del pedido (spec 011): las usan el formulario y el kanban, para que una transición tenga
 * una sola regla. También arma el origen de datos de la lista (en memoria hasta F1).
 */
@Injectable({ providedIn: 'root' })
export class PedidosAcciones {
  private readonly flow = inject(OperationalFlowState);
  private readonly libre = inject(PedidoLibre);

  /** Confirmar: el pedido libre recibe su Contpaq ID simulado (D-53). Devuelve el motivo si no procede. */
  confirmar(folio: string): string | undefined {
    const pedido = this.flow.pedido(folio);
    if (pedido.stage !== 'Borrador') return 'El pedido ya fue confirmado.';
    if (pedido.libre) return this.libre.confirmar(folio);
    this.flow.setOrderStage('Confirmado', folio);
    return undefined;
  }

  /** Autorizar: registra la firma pendiente; con las dos pasa a Autorizado (D-33). */
  autorizar(folio: string): string | undefined {
    if (!this.flow.puedeFirmar(folio)) return 'El pedido no se puede autorizar en su estado actual.';
    this.flow.autorizar(folio);
    const pendiente = this.flow.firmaPendiente(folio);
    return pendiente ? `Falta la firma de ${pendiente}.` : undefined;
  }

  firmaPendiente(folio: string): string | undefined {
    return this.flow.firmaPendiente(folio);
  }

  /** Filas de la lista, como las arma hoy la pantalla. */
  filas(): FilaPedido[] {
    return this.flow.pedidos.map(p => {
      const linea = p.lineas[0];
      return {
        id: p.folio,
        folio: p.folio,
        cliente: p.cliente,
        producto: linea?.clave ?? '',
        estado: p.stage,
        cantidad: linea ? `${n1(linea.cantidad)} ${linea.unidad}` : '',
        pedido: p,
      };
    });
  }

  /** Origen de la lista y del kanban: busca y filtra con la vista PEDIDOS, igual que la barra de búsqueda. */
  origen(): OrigenEnMemoria<FilaPedido> {
    return new OrigenEnMemoria<FilaPedido>({ datos: () => this.filas(), id: f => f.id, vista: PEDIDOS });
  }

  /** Transiciones que se pueden arrastrar en el kanban (data-model §3). */
  transiciones(): TransicionKanban<FilaPedido>[] {
    return [
      { desde: 'Borrador', hacia: 'Confirmado', nombre: 'Confirmar', ejecutar: f => this.confirmar(f.folio) },
      { desde: 'Confirmado', hacia: 'Autorizado', nombre: 'Autorizar', dialogo: FirmaPedido, ejecutar: f => this.autorizar(f.folio) },
    ];
  }
}
