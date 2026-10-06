/** Tipos de Services/StockOperationState.cs que usan los componentes compartidos. */
export interface LotAllocation {
  lote: string;
  cantidad: number;
}

export interface StockOperationLine {
  clave: string;
  producto: string;
  unidad: string;
  solicitado: number;
  /** Lotes y cantidades que el almacenista declara que salen. */
  asignaciones: LotAllocation[];
  entregado: number;
}

export const declarado = (linea: StockOperationLine): number =>
  linea.asignaciones.reduce((total, a) => total + a.cantidad, 0);

export const pendiente = (linea: StockOperationLine): number => Math.max(0, linea.solicitado - linea.entregado);
