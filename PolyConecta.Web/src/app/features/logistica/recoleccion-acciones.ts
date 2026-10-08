import { Component, inject, Injectable } from '@angular/core';
import { InventoryState } from '../../core/state/inventory-state';
import { etiquetaProducto } from '../../core/format/producto-etiqueta';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { n1 } from '../../core/format/numero';
import { TransicionKanban } from '../../core/kanban/kanban';
import { OrigenEnMemoria } from '../../core/lista/origen-en-memoria';
import { declarado, pendiente, StockOperation } from '../../core/models/operaciones';
import { adaptarVista } from '../../core/search/search-view';
import { OPERACIONES } from '../../core/search/views';
import { OperationalFlowState } from '../../core/state/operational-flow-state';
import { StockOperationState } from '../../core/state/stock-operation-state';
import { OdooDialog } from '../../shared/odoo-dialog/odoo-dialog';

export const ETAPAS_RECOLECCION = ['Borrador', 'En espera', 'Listo', 'Hecho'];

/** Fila de la lista de recolecciones y devoluciones: la operación con lo que muestran la tabla y la tarjeta. */
export interface FilaRecoleccion {
  id: string;
  op: StockOperation;
  folio: string;
  operacion: string;
  orden: string;
  origen: string;
  destino: string;
  estado: string;
  fechaLimite: Date;
  backorder: boolean;
}

/**
 * Acciones de la recolección (spec 011, P3): las usan el formulario y el kanban, para que validar tenga
 * una sola regla. Borrador → En espera lo avanza el sistema al confirmar la OF, y En espera → Listo, al
 * declarar lotes; solo Listo → Hecho (Validar) se arrastra.
 */
@Injectable({ providedIn: 'root' })
export class RecoleccionAcciones {
  private readonly ops = inject(StockOperationState);

  constructor() {
    // Las recolecciones nacen con las Órdenes de Fabricación: inyectar el flujo garantiza que existan.
    inject(OperationalFlowState);
  }

  /** Parcial: lo declarado no cubre lo pendiente de alguna línea, así que el remanente irá a un backorder. */
  esParcial(op: StockOperation): boolean {
    return op.lineas.some(l => declarado(l) < pendiente(l));
  }

  /** Validar (→ Hecho). Devuelve el backorder del remanente, si lo hubo, o el motivo si no procede. */
  validar(folio: string): { backorder?: StockOperation; error?: string } {
    const op = this.ops.get(folio);
    if (!op) return { error: 'La recolección no existe.' };
    return this.ops.validar(op);
  }

  origen(): OrigenEnMemoria<FilaRecoleccion> {
    return new OrigenEnMemoria<FilaRecoleccion>({
      datos: () => this.ops.operaciones.map(fila),
      id: f => f.id,
      vista: adaptarVista(OPERACIONES, (f: FilaRecoleccion) => f.op),
    });
  }

  /** Transiciones que se arrastran en el kanban (aclaración P3): Validar, con el diálogo de cantidades si es parcial. */
  transiciones(): TransicionKanban<FilaRecoleccion>[] {
    return [
      {
        desde: 'Listo', hacia: 'Hecho', nombre: 'Validar', dialogo: ValidarRecoleccion,
        pideDialogo: f => this.esParcial(f.op),
        ejecutar: f => this.validar(f.folio).error,
      },
    ];
  }
}

function fila(op: StockOperation): FilaRecoleccion {
  return {
    id: op.folio, op, folio: op.folio, operacion: op.operacion, orden: op.ofFolio, origen: op.origen, destino: op.destino,
    estado: op.state, fechaLimite: op.fechaLimite, backorder: op.backorderDe !== undefined,
  };
}

/**
 * Diálogo de validación parcial (aclaración P3): por línea, lo pendiente, lo declarado y lo que irá al
 * backorder. Lo abren el botón "Validar" del formulario y el arrastre a Hecho; devuelve `true` si se confirma.
 */
@Component({
  selector: 'pc-validar-recoleccion',
  imports: [OdooDialog],
  template: `
    <pc-odoo-dialog titulo="Validar parcialmente" textoPrimario="Validar y crear backorder" (confirmar)="ref.close(true)" (cancelar)="ref.close(false)">
      <p class="mb-2">Lo declarado en <strong>{{ datos.fila.folio }}</strong> no cubre todo lo pedido. El remanente se va a un backorder ligado a la misma orden.</p>
      <table class="table table-sm align-middle mb-0" data-validar-parcial>
        <thead><tr><th>Producto</th><th class="text-end">Pendiente</th><th class="text-end">Declarado</th><th class="text-end">Al backorder</th><th>Unidad</th></tr></thead>
        <tbody>
          @for (l of lineas; track l.clave) {
            <tr>
              <td>{{ producto(l.clave) }}</td>
              <td class="text-end">{{ n1(l.pendiente) }}</td>
              <td class="text-end">{{ n1(l.declarado) }}</td>
              <td class="text-end" [class.fw-semibold]="l.remanente > 0">{{ n1(l.remanente) }}</td>
              <td>{{ l.unidad }}</td>
            </tr>
          }
        </tbody>
      </table>
    </pc-odoo-dialog>
  `,
})
export class ValidarRecoleccion {
  private readonly invProductos = inject(InventoryState);
  protected readonly producto = (claveONombre?: string | null, nombre?: string) => etiquetaProducto(this.invProductos.catalogo, claveONombre, nombre);
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  protected readonly datos = inject<{ fila: { folio: string } }>(DIALOG_DATA);
  protected readonly n1 = n1;
  protected readonly lineas = (inject(StockOperationState).get(this.datos.fila.folio)?.lineas ?? []).map(l => ({
    clave: l.clave, unidad: l.unidad, pendiente: pendiente(l), declarado: declarado(l), remanente: Math.max(0, pendiente(l) - declarado(l)),
  }));
}
