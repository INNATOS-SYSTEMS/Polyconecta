import { Injectable, inject } from '@angular/core';
import { LotBalance } from '../../models/inventario';
import { Delivery, DocumentoLogistica, InterplantTransfer, Reception, ShipmentLine } from '../../models/logistica';
import { StockOperation } from '../../models/operaciones';
import { InventoryState } from '../inventory-state';
import { OperationalFlowState } from '../operational-flow-state';
import { StockOperationState } from '../stock-operation-state';
import { LineaLibre } from './linea-libre';
import { lotesPermitidos, otraPlanta, plantaDe } from './movimientos-logistica';

export const PLANTAS = ['PIM', 'SC'] as const;
export type Planta = (typeof PLANTAS)[number];

/**
 * Operaciones de inventario y logística en modo libre (FR-012):
 * - Recolección libre: MP → WIP sin OF; el saldo queda sin asignar (D-55).
 * - Devolución `REC-RET` libre: WIP → MP del saldo sin asignar, con cantidad capturada a mano.
 * - Traslado libre: solo lotes liberados por Calidad.
 * - Recepción libre: solo lotes en tránsito, `TRANS/*` (D-56).
 * - Entrega libre: cliente y lotes liberados, sin pedido.
 */
@Injectable({ providedIn: 'root' })
export class OperacionesLibres {
  private readonly flow = inject(OperationalFlowState);
  private readonly ops = inject(StockOperationState);
  private readonly inv = inject(InventoryState);

  // ------------------------------------------------------------ recolección y devolución

  crearRecoleccion(planta: Planta, lineas: LineaLibre[]): { op?: StockOperation; error?: string } {
    return this.crearOperacion(planta, lineas, false);
  }

  crearDevolucion(planta: Planta, lineas: LineaLibre[]): { op?: StockOperation; error?: string } {
    return this.crearOperacion(planta, lineas, true);
  }

  private crearOperacion(planta: Planta, lineas: LineaLibre[], devolucion: boolean): { op?: StockOperation; error?: string } {
    if (lineas.length === 0) return { error: 'Agregue al menos una línea.' };
    const codigo = `${planta === 'PIM' ? 'PIM' : 'STC'}-REC-${devolucion ? 'RET' : 'OUT'}`;
    const tipo = this.ops.getTipo(codigo);
    const op = new StockOperation({
      folio: this.ops.nuevoFolio(planta, !devolucion),
      tipo,
      ofFolio: '',
      origen: tipo.origen,
      destino: tipo.destino,
      libre: true,
      // Sin OF que confirmar: nace liberada a Almacén.
      step: 1,
      lineas: lineas.map(l => ({ clave: l.clave, producto: l.producto, unidad: l.unidad, solicitado: l.cantidad, asignaciones: [], entregado: 0 })),
    });
    this.ops.operaciones.push(op);
    this.ops.notificar();
    return { op };
  }

  // ------------------------------------------------------------ traslado, recepción y entrega

  /** Lotes liberados por Calidad en una planta: lo que ofrece un traslado o una entrega libres. */
  lotesLiberados(planta: Planta): LotBalance[] {
    return this.inv.lotesLiberados(planta);
  }

  /** Lotes en tránsito hacia una planta: lo único que ofrece una recepción libre. */
  lotesEnTransito(planta: Planta): LotBalance[] {
    return this.inv.lotesEnTransito().filter(l => plantaDe(l.ubicacion) === planta);
  }

  crearTraslado(origen: Planta, lotes: string[]): { doc?: InterplantTransfer; error?: string } {
    const destino = otraPlanta(origen);
    const doc = new InterplantTransfer({
      folio: this.ops.nuevoFolio(origen, true),
      operacion: `Traspaso ${origen} a ${destino}`,
      origen: `${origen}/Stock`,
      destino: `${destino}/Stock/MP`,
      libre: true,
    });
    return this.armar(doc, lotes, this.lotesLiberados(origen), 'liberado por Calidad en ' + origen, this.flow.traslados);
  }

  crearRecepcion(planta: Planta, lotes: string[]): { doc?: Reception; error?: string } {
    const doc = new Reception({
      folio: this.ops.nuevoFolio(planta, false),
      operacion: 'Recepción en almacén',
      origen: `TRANS/${planta}`,
      destino: `${planta}/Stock/MP`,
      libre: true,
    });
    return this.armar(doc, lotes, this.lotesEnTransito(planta), `en tránsito hacia ${planta}`, this.flow.recepciones);
  }

  crearEntrega(planta: Planta, cliente: string, lotes: string[]): { doc?: Delivery; error?: string } {
    if (cliente.trim() === '') return { error: 'Capture el cliente.' };
    const doc = new Delivery({
      folio: this.ops.nuevoFolio(planta, true),
      operacion: 'Entrega a cliente',
      origen: `${planta}/Stock/PT`,
      destino: cliente.trim(),
      cliente: cliente.trim(),
      libre: true,
    });
    return this.armar(doc, lotes, this.lotesLiberados(planta), `liberado por Calidad en ${planta}`, this.flow.entregas);
  }

  /** Lotes que el documento libre puede seleccionar después de creado (para el selector del formulario). */
  permitidos(doc: DocumentoLogistica): LotBalance[] {
    return lotesPermitidos(doc, this.inv);
  }

  /** Valida los lotes contra la regla y agrupa las líneas por producto, con cantidad y unidad (D-127). */
  private armar<T extends DocumentoLogistica>(doc: T, lotes: string[], permitidos: LotBalance[], regla: string, coleccion: T[]): { doc?: T; error?: string } {
    if (lotes.length === 0) return { error: 'Elija al menos un lote.' };
    const lineas = new Map<string, ShipmentLine>();
    for (const nombre of lotes) {
      const lote = permitidos.find(l => l.lote === nombre);
      if (!lote) return { error: `El lote ${nombre} no está ${regla}.` };
      const producto = this.inv.getProducto(lote.clave);
      const linea = lineas.get(lote.clave) ?? { clave: lote.clave, producto: producto?.nombre ?? lote.clave, demanda: 0, entregado: 0, unidad: producto?.unidad ?? '', lotesSeleccionados: [] };
      linea.demanda += lote.cantidad;
      linea.lotesSeleccionados.push(lote.lote);
      lineas.set(lote.clave, linea);
    }
    doc.lineas = [...lineas.values()];
    coleccion.push(doc);
    this.flow.notificar();
    return { doc };
  }
}
