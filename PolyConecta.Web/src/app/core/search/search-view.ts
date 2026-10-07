/** Tipos de Services/SearchViews.cs. `aplicar` lo implementa la fase 1 de la spec 001. */
export interface SearchField<T> {
  etiqueta: string;
  valor: (item: T) => string | undefined;
}

/** Filtro predefinido con nombre. `campo` agrupa filtros de la misma dimensión. */
export interface SearchFilter<T> {
  nombre: string;
  campo: string;
  condicion: (item: T) => boolean;
}

export interface SearchGroupBy<T> {
  etiqueta: string;
  clave: (item: T) => string;
}

/** Vista de búsqueda de un modelo: declarativa y separada de la pantalla. */
export interface SearchView<T> {
  campos: SearchField<T>[];
  filtros: SearchFilter<T>[];
  agrupaciones: SearchGroupBy<T>[];
  /** Etiquetas de las agrupaciones con las que abre la lista, en orden de anidamiento. */
  agrupacionesPorDefecto?: string[];
  /** Campo de referencia: el respaldo cuando no hay campos declarados. */
  referencia?: (item: T) => string | undefined;
}

export const etiquetasCampos = <T>(view: SearchView<T>): string => view.campos.map(c => c.etiqueta).join(', ');
