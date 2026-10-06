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

/**
 * Tipo de operación de inventario: la naturaleza del movimiento y el evento que dispara en
 * CONTPAQi. Es el catálogo de la sección 5 de la arquitectura.
 */
export interface OperationType {
  codigo: string;
  nombre: string;
  origen: string;
  destino: string;
  eventoContpaq: string;
  /** Código del tipo que revierte este movimiento, si lo hay. */
  reversa?: string;
  esDevolucion: boolean;
}

/** Estados de una operación: 0 Borrador · 1 En espera (solicitada a almacén) · 2 Listo (lotes declarados) · 3 Hecho. */
export type PasoOperacion = 0 | 1 | 2 | 3;

/**
 * Operación de inventario. La Recolección (MP → WIP) es la formalidad con la que Producción le pide
 * materia prima a Almacén y Almacén le da salida del stock.
 */
export class StockOperation {
  folio = '';
  tipo!: OperationType;
  ofFolio = '';
  origen = '';
  destino = '';
  fecha = new Date(2026, 8, 22);
  backorderDe?: string;
  warning?: string;
  error?: string;
  lineas: StockOperationLine[] = [];
  step: PasoOperacion = 0;
  contpaqId = '';
  /** Creada con "Nuevo", sin OF: su saldo en WIP queda sin asignar (D-55). */
  libre = false;

  constructor(init: Partial<StockOperation> = {}) {
    Object.assign(this, init);
  }

  get state(): string {
    return ['Borrador', 'En espera', 'Listo', 'Hecho'][this.step];
  }

  get esParcial(): boolean {
    return this.lineas.some(l => pendiente(l) > 0);
  }

  get operacion(): string {
    return this.tipo.nombre;
  }

  get fechaLimite(): Date {
    const d = new Date(this.fecha);
    d.setDate(d.getDate() + 1);
    return d;
  }

  get totalSolicitado(): number {
    return this.lineas.reduce((t, l) => t + l.solicitado, 0);
  }

  get totalEntregado(): number {
    return this.lineas.reduce((t, l) => t + l.entregado, 0);
  }
}
