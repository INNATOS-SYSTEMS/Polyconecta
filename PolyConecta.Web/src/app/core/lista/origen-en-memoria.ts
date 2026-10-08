import { ordenCultural } from '../format/numero';
import { SearchView, aplicar } from '../search/search-view';
import { ConsultaLista, FiltroLista, GrupoLista, OrigenDeLista, ResultadoLista } from './origen';

export interface OpcionesOrigenEnMemoria<T> {
  /** Lee la colección al consultar: así refleja los cambios del servicio de estado. */
  datos: () => readonly T[];
  /** Identificador de una fila, para `ids` y la selección. */
  id: (fila: T) => string;
  /** Lee un campo; por omisión, la propiedad con ese nombre. */
  leer?: (fila: T, campo: string) => unknown;
  /** Columnas que se suman en los totales. */
  sumables?: readonly string[];
  /**
   * Unidad de cada fila: si se declara, un total solo se calcula cuando todas sus filas comparten
   * unidad (no se suman kilos con piezas). Así lo hacía Inventario Actual.
   */
  unidad?: (fila: T) => string;
  /** Campo donde el grupo muestra su unidad común; por omisión, `unidad`. */
  campoUnidad?: string;
  /** Campos en los que busca la barra de búsqueda, si no hay vista. */
  buscables?: readonly string[];
  /**
   * Vista de búsqueda del modelo (core/search): con ella el origen resuelve la búsqueda libre, los
   * filtros con nombre y las agrupaciones por etiqueta, igual que la barra de búsqueda de hoy.
   */
  vista?: SearchView<T>;
  /** Texto a mostrar de un grupo, por ejemplo el nombre del cliente. */
  etiqueta?: (campo: string, valor: string) => string;
}

const leerPropiedad = (fila: unknown, campo: string): unknown => (fila as Record<string, unknown>)[campo];
const texto = (v: unknown): string => (v instanceof Date ? v.toISOString() : String(v ?? '')).toLowerCase();

function cumple(valor: unknown, f: FiltroLista): boolean {
  switch (f.operador) {
    case 'contiene':
      return texto(valor).includes(texto(f.valor));
    case 'igual':
      return texto(valor) === texto(f.valor);
    case 'en':
      return (f.valor as unknown[]).some(v => texto(v) === texto(valor));
    case 'entre': {
      const [desde, hasta] = f.valor as [unknown, unknown];
      const n = valor instanceof Date ? valor.getTime() : Number(valor);
      const a = desde instanceof Date ? desde.getTime() : desde == null ? -Infinity : Number(desde);
      const b = hasta instanceof Date ? hasta.getTime() : hasta == null ? Infinity : Number(hasta);
      return n >= a && n <= b;
    }
  }
}

function comparar(a: unknown, b: unknown): number {
  const vacioA = a == null || a === '', vacioB = b == null || b === '';
  if (vacioA && vacioB) return 0;
  if (vacioA) return 1; // los vacíos al final, como Odoo
  if (vacioB) return -1;
  if (a instanceof Date && b instanceof Date) return a.getTime() - b.getTime();
  if (typeof a === 'number' && typeof b === 'number') return a - b;
  return String(a).localeCompare(String(b), 'es-MX', { numeric: true, sensitivity: 'base' });
}

/**
 * Origen en memoria (spec 011, research R-02): filtra, busca, ordena, agrupa en varios niveles con
 * totales y pagina sobre la colección de un servicio de estado. En F1 lo sustituye un origen HTTP.
 */
export class OrigenEnMemoria<T> implements OrigenDeLista<T> {
  /** Consultas recibidas: las pruebas verifican que la tabla no procesa por su cuenta. */
  consultas = 0;
  private readonly leer: (fila: T, campo: string) => unknown;

  constructor(private readonly opciones: OpcionesOrigenEnMemoria<T>) {
    this.leer = opciones.leer ?? leerPropiedad;
  }

  async consultar(c: ConsultaLista): Promise<ResultadoLista<T>> {
    this.consultas++;
    return this.resolver(c);
  }

  /** Igual que `consultar`, sin esperar: para pruebas y para el kanban, que lee varias etapas a la vez. */
  resolver(c: ConsultaLista): ResultadoLista<T> {
    const filtradas = this.filtrar(c);
    const totales = this.sumar(filtradas);
    const nivel = c.grupo.length;
    if (nivel < c.agruparPor.length) {
      const campo = c.agruparPor[nivel];
      const grupos = this.agrupar(this.ordenar(filtradas, c), campo, c);
      return { filas: [], grupos: grupos.slice(c.pagina * c.tamano, (c.pagina + 1) * c.tamano), total: grupos.length, totales };
    }
    const ordenadas = this.ordenar(filtradas, c);
    return { filas: ordenadas.slice(c.pagina * c.tamano, (c.pagina + 1) * c.tamano), grupos: null, total: ordenadas.length, totales };
  }

  private filtrar(c: ConsultaLista): T[] {
    const ids = c.ids ? new Set(c.ids) : null;
    const buscables = this.opciones.buscables ?? [];
    const busqueda = c.busqueda?.trim() ? texto(c.busqueda.trim()) : null;
    // Filtros del mismo campo se unen con O; de campos distintos, con Y (05 §7.1).
    const porCampo = new Map<string, FiltroLista[]>();
    for (const f of c.filtros) porCampo.set(f.campo, [...(porCampo.get(f.campo) ?? []), f]);
    const vista = this.opciones.vista;
    const base = vista ? aplicar(vista, this.opciones.datos(), c.busqueda ?? undefined, c.nombrados) : [...this.opciones.datos()];
    return base.filter(fila =>
      (!ids || ids.has(this.opciones.id(fila))) &&
      c.grupo.every(g => this.valorDeGrupo(fila, g.campo) === g.valor) &&
      [...porCampo.values()].every(fs => fs.some(f => cumple(this.leer(fila, f.campo), f))) &&
      (vista || !busqueda || buscables.some(b => texto(this.leer(fila, b)).includes(busqueda))));
  }

  /** Valor de agrupación: la clave de una agrupación de la vista, por su etiqueta, o el campo. */
  private valorDeGrupo(fila: T, campo: string): string {
    const agrupacion = this.opciones.vista?.agrupaciones.find(a => a.etiqueta === campo);
    return String((agrupacion ? agrupacion.clave(fila) : this.leer(fila, campo)) ?? '');
  }

  /** Orden estable: con empates conserva el orden de la colección. */
  private ordenar(filas: T[], c: ConsultaLista): T[] {
    if (!c.orden.length) return filas;
    return filas
      .map((fila, i) => ({ fila, i }))
      .sort((x, y) => {
        for (const o of c.orden) {
          const r = comparar(this.leer(x.fila, o.campo), this.leer(y.fila, o.campo));
          if (r !== 0) return o.desc ? -r : r;
        }
        return x.i - y.i;
      })
      .map(x => x.fila);
  }

  private agrupar(filas: T[], campo: string, c: ConsultaLista): GrupoLista[] {
    const mapa = new Map<string, T[]>();
    for (const fila of filas) {
      const valor = this.valorDeGrupo(fila, campo);
      mapa.set(valor, [...(mapa.get(valor) ?? []), fila]);
    }
    const grupos = [...mapa.entries()].map(([valor, fs]) => ({
      campo,
      valor,
      etiqueta: valor === '' ? 'Ninguno' : (this.opciones.etiqueta?.(campo, valor) ?? valor),
      cantidad: fs.length,
      totales: this.sumar(fs),
      textos: this.unidadComun(fs),
    }));
    // Si se ordena por el campo agrupado, los grupos siguen ese orden; si no, por etiqueta.
    const orden = c.orden.find(o => o.campo === campo);
    // Etiquetas con el orden cultural del prototipo (es-419, sin orden numérico): C4235 antes que C455.
    return orden ? grupos : grupos.sort((a, b) => ordenCultural(a.etiqueta, b.etiqueta));
  }

  private unidadComun(filas: T[]): Record<string, string> | undefined {
    const unidad = this.opciones.unidad;
    if (!unidad) return undefined;
    const unidades = new Set(filas.map(unidad));
    return unidades.size === 1 ? { [this.opciones.campoUnidad ?? 'unidad']: [...unidades][0] } : undefined;
  }

  private sumar(filas: T[]): Record<string, number> {
    const unidad = this.opciones.unidad;
    if (unidad && new Set(filas.map(unidad)).size > 1) return {};
    return Object.fromEntries((this.opciones.sumables ?? []).map(s => [s, filas.reduce((t, f) => t + (Number(this.leer(f, s)) || 0), 0)]));
  }
}
