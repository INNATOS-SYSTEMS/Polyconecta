/**
 * Origen de datos de una lista (spec 011, data-model §1). La tabla y el kanban piden cada vista a un
 * origen y nunca ordenan, filtran, agrupan ni paginan por su cuenta. Hasta F1 el origen es en memoria;
 * en F1 se agrega uno por HTTP con la misma interfaz.
 */
export interface ConsultaLista {
  /** Desde 0. Con agrupación, es la página de grupos del nivel que se pide. */
  pagina: number;
  tamano: number;
  orden: OrdenLista[];
  filtros: FiltroLista[];
  /**
   * Filtros con nombre de la vista de búsqueda ("Borrador", "Mis pedidos"), como en Odoo: el origen
   * los evalúa por su nombre. Los del mismo campo de la vista se unen con O; los demás, con Y.
   */
  nombrados: string[];
  /** Texto libre de la barra de búsqueda, sobre los campos que la lista declara buscables. */
  busqueda: string | null;
  /** Niveles de agrupación, en orden: un campo o la etiqueta de una agrupación de la vista. Vacío = sin agrupar. */
  agruparPor: string[];
  /** Ruta del grupo que se abre. Vacío = primer nivel. */
  grupo: { campo: string; valor: string }[];
  /** Solo esas filas: para exportar las seleccionadas. */
  ids: string[] | null;
}

export interface OrdenLista {
  campo: string;
  desc: boolean;
}

export type OperadorFiltro = 'contiene' | 'igual' | 'entre' | 'en';

export interface FiltroLista {
  campo: string;
  operador: OperadorFiltro;
  valor: unknown;
}

export interface GrupoLista {
  campo: string;
  valor: string;
  etiqueta: string;
  cantidad: number;
  totales: Record<string, number>;
  /** Textos del grupo por campo que no se suman, como la unidad común (Inventario Actual). */
  textos?: Record<string, string>;
}

export interface ResultadoLista<T> {
  /** Vacío cuando la respuesta es de grupos. */
  filas: T[];
  /** Presente cuando en esa ruta queda un nivel de agrupación por abrir. */
  grupos: GrupoLista[] | null;
  /** Total de filas, o de grupos, para el paginador. */
  total: number;
  /** Sumas de las columnas sumables sobre todo el filtro. */
  totales: Record<string, number>;
}

export interface OrigenDeLista<T> {
  consultar(consulta: ConsultaLista): Promise<ResultadoLista<T>>;
}

export const TAMANOS_PAGINA = [20, 40, 80, 200] as const;

/** Consulta vacía: primera página de 80, como Odoo. */
export function consultaInicial(parcial: Partial<ConsultaLista> = {}): ConsultaLista {
  return { pagina: 0, tamano: 80, orden: [], filtros: [], nombrados: [], busqueda: null, agruparPor: [], grupo: [], ids: null, ...parcial };
}
