/** Tipos de Services/OperationalFlowState.cs que usan los componentes compartidos. */
export type EstadoLote = 'En revisión' | 'Aprobado' | 'Rechazado';

export interface ProductionLot {
  lote: string;
  real: number;
  unidad: string;
  estado: EstadoLote;
}
