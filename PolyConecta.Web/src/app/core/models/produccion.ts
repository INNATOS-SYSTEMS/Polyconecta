/** Tipos de Services/OperationalFlowState.cs que usan los componentes compartidos. */
export type EstadoLote = 'En revisión' | 'Aprobado' | 'Rechazado';

export interface ProductionLot {
  lote: string;
  real: number;
  unidad: string;
  estado: EstadoLote;
}

/** Componente de una OF. */
export interface BomLine {
  clave: string;
  producto: string;
  cantidad: number;
  unidad: string;
}

export interface SubProductLine {
  clave: string;
  producto: string;
  cantidad: number;
  unidad: string;
  producido: boolean;
  almacenDestino: string;
}

export interface PlanningLine {
  centroTrabajo: string;
  producto: string;
  cantidad: number;
  unidad: string;
  horasAsignadas: number;
  fechaInicio: Date;
  fechaFin: Date;
  operador: string;
}

export type ProcessType = 'Extrusion' | 'Impresion' | 'Bolseo';
export type EstadoOf = 'Borrador' | 'Planeado' | 'En progreso' | 'Hecho';

/**
 * Orden de Fabricación, autorreferenciada: la que no tiene originFolio es la maestra (ligada al
 * pedido); las demás encadenan hacia atrás por originFolio.
 */
export class ManufacturingOrder {
  folio = '';
  processType: ProcessType = 'Extrusion';
  processLabel = '';
  producto = '';
  empresa = 'POLYEMPAQUES Y DERIVADOS S.A. DE C.V.';
  cantidad = 0;
  unidad = 'KGS';
  tiempoEstimadoHrs = 0;
  numeroRollos = 0;
  calidadRequerida = true;
  almacenFalla = 'PIM/Cuarentena';
  fechaEsperada = new Date(2026, 0, 1);
  state: EstadoOf = 'Borrador';
  originFolio?: string;
  pedidoFolio = 'IV310-26';
  componentes: BomLine[] = [];
  subproductos: SubProductLine[] = [];
  produccion: ProductionLot[] = [];
  planeacion: PlanningLine[] = [];
  sequenceCounter = 0;
  /** Creada con "Nuevo", sin pedido (Principio X). */
  libre = false;

  constructor(init: Partial<ManufacturingOrder> = {}) {
    Object.assign(this, init);
  }

  get numeroLabel(): string {
    return this.processType === 'Bolseo' ? 'Numero de empaques' : 'No. Rollos';
  }

  get producidoTotal(): number {
    return this.produccion.filter(p => p.estado !== 'Rechazado').reduce((t, p) => t + p.real, 0);
  }
}

export interface QualityControlState {
  manufacturingOrderFolio: string;
  folio: string;
  auditor: string;
  processLabel: string;
}

export interface Incidencia {
  fecha: Date;
  centroTrabajo: string;
  tipo: string;
  comentarios: string;
  horaInicio: string;
  horaFin: string;
}
