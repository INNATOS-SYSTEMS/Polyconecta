import { LotBalance } from '../../models/inventario';
import { Delivery, DocumentoLogistica, Reception } from '../../models/logistica';
import { ProductionLot } from '../../models/produccion';
import type { InventoryState } from '../inventory-state';

/** Planta de una ubicación: `PIM/Stock/PT` → `PIM`, `TRANS/SC` → `SC`. */
export const plantaDe = (ubicacion: string): string => {
  const partes = ubicacion.toUpperCase().split('/');
  return partes[0] === 'TRANS' ? (partes[1] ?? '') : partes[0];
};

export const otraPlanta = (planta: string): string => (planta.toUpperCase() === 'PIM' ? 'SC' : 'PIM');

/**
 * Lotes que puede tomar un documento de logística libre, según su regla (FR-012):
 * recepción, solo en tránsito (D-56); traslado y entrega, solo liberados por Calidad en su planta.
 */
export function lotesPermitidos(doc: DocumentoLogistica, inv: InventoryState): LotBalance[] {
  if (doc instanceof Reception) return inv.lotesEnTransito().filter(l => plantaDe(l.ubicacion) === plantaDe(doc.destino));
  return inv.lotesLiberados(plantaDe(doc.origen));
}

/** Los lotes del inventario con la forma que usan el selector y el cierre de salida. */
export function comoLotes(lotes: readonly LotBalance[], inv: InventoryState): ProductionLot[] {
  return lotes.map(l => ({ lote: l.lote, real: l.cantidad, unidad: inv.getProducto(l.clave)?.unidad ?? '', estado: 'Aprobado' }));
}

/**
 * Al llegar a Hecho, el documento libre mueve el inventario real (el documento semilla no lo hace,
 * igual que el prototipo): el traslado deja los lotes en tránsito, la recepción los mete al almacén
 * destino y la entrega les da salida. También recibe su Contpaq ID simulado.
 */
export function aplicarMovimientoLibre(doc: DocumentoLogistica, inv: InventoryState): void {
  const permitidos = lotesPermitidos(doc, inv);
  for (const nombre of doc.lineas.flatMap(l => l.lotesSeleccionados)) {
    const lote = permitidos.find(l => l.lote === nombre);
    if (!lote) continue;
    if (doc instanceof Delivery) inv.darSalida(lote.lote, lote.ubicacion);
    else if (doc instanceof Reception) inv.moverLote(lote.lote, lote.ubicacion, doc.destino);
    else inv.moverLote(lote.lote, lote.ubicacion, `TRANS/${plantaDe(doc.destino)}`);
  }
  doc.contpaqId = `${doc instanceof Delivery ? 'REM' : 'TR'}-${doc.folio.split('/').at(-1)}`;
}
