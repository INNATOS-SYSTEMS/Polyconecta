import { ManufacturingOrder } from '../../core/models/produccion';

/** Folio del control de calidad de una OF. */
export const qcFolio = (folioOf: string): string => 'QC-' + folioOf.replaceAll('/', '-');

/** Estado del control: Planeado sin lotes, Parcial con revisión o rechazos, Aprobado si todo pasó. */
export function estadoQc(of: ManufacturingOrder | undefined): string {
  if (!of || of.produccion.length === 0) return 'Planeado';
  if (of.produccion.some(l => l.estado === 'En revisión')) return 'Parcial';
  return of.produccion.some(l => l.estado === 'Rechazado') ? 'Parcial' : 'Aprobado';
}
