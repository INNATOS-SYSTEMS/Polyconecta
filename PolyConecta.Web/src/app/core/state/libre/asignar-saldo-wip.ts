import { Injectable, inject } from '@angular/core';
import { LotBalance } from '../../models/inventario';
import { InventoryState } from '../inventory-state';
import { OperationalFlowState } from '../operational-flow-state';

/**
 * "Asignar saldo de WIP" (FR-013): liga a una OF el saldo sin asignar que dejó una recolección libre
 * (D-55). Solo ocurre con esta acción explícita; ninguna otra operación la llama (008-FR-004).
 */
@Injectable({ providedIn: 'root' })
export class AsignarSaldoWip {
  private readonly flow = inject(OperationalFlowState);
  private readonly inv = inject(InventoryState);

  /** WIP de la planta de la OF, como en sus recolecciones. */
  wipDe(folio: string): string {
    return this.flow.getOrder(folio)?.processType === 'Extrusion' ? InventoryState.WipPim : InventoryState.WipStc;
  }

  /** Saldo sin asignar de los componentes de la OF, en su WIP. */
  disponibles(folio: string): LotBalance[] {
    const of = this.flow.getOrder(folio);
    if (!of || of.state === 'Hecho') return [];
    const claves = new Set(of.componentes.map(c => c.clave.toLowerCase()));
    return this.inv.saldoSinAsignar(this.wipDe(folio)).filter(l => claves.has(l.clave.toLowerCase()));
  }

  asignar(folio: string, lote: string, cantidad: number): string | undefined {
    const saldo = this.disponibles(folio).find(l => l.lote === lote);
    if (!saldo) return `El lote ${lote} no tiene saldo sin asignar para los componentes de ${folio}.`;
    if (!(cantidad > 0) || cantidad > saldo.cantidad) return `Cantidad no válida: hay ${saldo.cantidad} sin asignar.`;
    return this.inv.asignarSaldoWip(lote, this.wipDe(folio), folio, cantidad) ? undefined : 'El inventario rechazó la asignación.';
  }
}
