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

/**
 * Aplica texto libre y facetas (FR-009): el texto se busca en los campos declarados (o en la
 * referencia si no hay campos); los filtros del mismo campo se combinan con O y los de campos
 * distintos con Y.
 */
export function aplicar<T>(view: SearchView<T>, items: readonly T[], texto: string | undefined, filtrosActivos: readonly string[]): T[] {
  let res = [...items];
  if (texto !== undefined && texto.trim() !== '') {
    const t = texto.toLowerCase();
    const selectores = view.campos.length > 0 ? view.campos.map(c => c.valor) : view.referencia ? [view.referencia] : [];
    res = res.filter(i => selectores.some(sel => (sel(i) ?? '').toLowerCase().includes(t)));
  }
  const activos = new Set(filtrosActivos.map(f => f.toLowerCase()));
  const porCampo = new Map<string, SearchFilter<T>[]>();
  for (const f of view.filtros.filter(f => activos.has(f.nombre.toLowerCase()))) {
    porCampo.set(f.campo, [...(porCampo.get(f.campo) ?? []), f]);
  }
  for (const grupo of porCampo.values()) {
    res = res.filter(i => grupo.some(f => f.condicion(i))); // O dentro del campo, Y entre campos
  }
  return res;
}
