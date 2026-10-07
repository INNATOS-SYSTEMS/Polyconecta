import { Injectable, inject } from '@angular/core';
import { ManufacturingOrder, ProcessType } from '../../models/produccion';
import { InventoryState } from '../inventory-state';
import { OperationalFlowState } from '../operational-flow-state';
import { crearLineaLibre, siguienteFolio } from './linea-libre';

const PREFIJO: Record<ProcessType, string> = { Extrusion: 'EXT', Impresion: 'IMP', Bolseo: 'BOL' };
const ETIQUETA: Record<ProcessType, string> = { Extrusion: 'Extrusión', Impresion: 'Impresión', Bolseo: 'Bolseo' };

/** OF raíz: se sube por originFolio hasta la que no tiene origen. */
export function ofRaiz(of: ManufacturingOrder, ordenes: readonly ManufacturingOrder[]): ManufacturingOrder {
  let actual = of;
  const vistos = new Set<string>();
  while (actual.originFolio && !vistos.has(actual.folio)) {
    vistos.add(actual.folio);
    actual = ordenes.find(o => o.folio === actual.originFolio) ?? actual;
  }
  return actual;
}

/** Nombre del lote n de una OF sin pedido: folio de la OF raíz con `/` → `-` (D-54), como R001-BOL-2026-0007. */
export function nombreLoteLibre(of: ManufacturingOrder, ordenes: readonly ManufacturingOrder[], n: number): string {
  return `R${String(n).padStart(3, '0')}-${ofRaiz(of, ordenes).folio.replaceAll('/', '-')}`;
}

/**
 * Orden de fabricación libre (FR-012): sin pedido, con proceso, producto, cantidad y la unidad base
 * del producto. Al confirmarla genera su recolección y su control igual que una ligada, porque usa
 * las mismas operaciones de OperationalFlowState.
 */
@Injectable({ providedIn: 'root' })
export class OfLibre {
  private readonly flow = inject(OperationalFlowState);
  private readonly inv = inject(InventoryState);

  static readonly Procesos: { tipo: ProcessType; etiqueta: string }[] = (Object.keys(ETIQUETA) as ProcessType[]).map(tipo => ({ tipo, etiqueta: ETIQUETA[tipo] }));

  crear(proceso: ProcessType, clave: string, cantidad: number): { of?: ManufacturingOrder; error?: string } {
    const { linea, error } = crearLineaLibre(this.inv.getProducto(clave), cantidad);
    if (error) return { error };
    const of = new ManufacturingOrder({
      folio: siguienteFolio(PREFIJO[proceso], this.flow.manufacturingOrders.map(o => o.folio)),
      processType: proceso,
      processLabel: ETIQUETA[proceso],
      producto: linea!.producto,
      cantidad: linea!.cantidad,
      unidad: linea!.unidad,
      almacenFalla: proceso === 'Extrusion' ? 'PIM/Cuarentena' : 'SC/Cuarentena',
      pedidoFolio: '',
      libre: true,
      componentes: [],
      subproductos: [],
      produccion: [],
      planeacion: [],
    });
    this.flow.manufacturingOrders.push(of);
    this.flow.notificar();
    return { of };
  }

  /**
   * "Nuevo" con un solo guardado (D-136, aclaración P2): el maestro con sus componentes y subproductos.
   * Valida todo antes de crear: si una línea no es válida, no crea nada.
   */
  crearConLineas(proceso: ProcessType, clave: string, cantidad: number,
    componentes: { clave: string; cantidad: number }[], subproductos: { clave: string; cantidad: number }[]): { of?: ManufacturingOrder; error?: string } {
    const validar = (l: { clave: string; cantidad: number }) => crearLineaLibre(this.inv.getProducto(l.clave), l.cantidad);
    for (const l of [...componentes, ...subproductos]) {
      const { error } = validar(l);
      if (error) return { error };
    }
    const { of, error } = this.crear(proceso, clave, cantidad);
    if (!of) return { error };
    for (const l of componentes) {
      const { linea } = validar(l);
      this.flow.agregarComponente(of.folio, linea!.clave, linea!.producto, linea!.cantidad, linea!.unidad);
    }
    for (const l of subproductos) {
      const { linea } = validar(l);
      this.flow.agregarSubproducto(of.folio, linea!.clave, linea!.producto, linea!.cantidad, linea!.unidad);
    }
    return { of };
  }

  /** Nombre que le toca al siguiente lote capturado en la OF (D-54). */
  siguienteLote(folio: string): string {
    const of = this.flow.getOrder(folio);
    if (!of) return '';
    return nombreLoteLibre(of, this.flow.manufacturingOrders, of.produccion.length + 1);
  }
}
