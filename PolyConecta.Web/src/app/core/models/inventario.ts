/** Tipos de Services/InventoryState.cs que usan los componentes compartidos (spec 001, data-model §1). */
export type ProductClass = 'Bolsa' | 'RolloImpreso' | 'RolloLiso' | 'RolloMaestro' | 'MateriaPrima' | 'Scrap';

export type LotStatus = 'Libre' | 'Reservado' | 'EnWip' | 'Cuarentena';

export interface ProductRef {
  clave: string;
  nombre: string;
  clasificacion: ProductClass;
  /** Unidad base del producto en CONTPAQi (D-127). */
  unidad: string;
}

/** Un lote concreto en una ubicación concreta. La reserva es por lote, no por cantidad agregada. */
export interface LotBalance {
  lote: string;
  clave: string;
  ubicacion: string;
  cantidad: number;
  estado: LotStatus;
  /** Folio del documento que lo tiene comprometido (pedido u OF), si aplica. */
  comprometidoPor?: string;
}

/** Renglón del Inventario Actual: la existencia de un lote en una ubicación. */
export interface StockQuant {
  producto: ProductRef;
  ubicacion: string;
  lote: string;
  cantidad: number;
}
