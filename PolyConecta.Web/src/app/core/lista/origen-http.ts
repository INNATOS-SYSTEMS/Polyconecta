import { ConsultaLista, OrigenDeLista, ResultadoLista } from './origen';
import { OrigenEnMemoria } from './origen-en-memoria';
import { SearchView } from '../search/search-view';
import { pedirApi, respuestaApi } from '../sesion/api';

type ConFiltros<T> = T & { _filtros?: string[] };

export interface RespuestaConjunto<T> {
  completo: boolean;
  total: number;
  generado?: string;
  filas?: ConFiltros<T>[];
}

/** `GET …/vista` (contracts/api-listas.md): la vista de búsqueda la declara el servidor. */
export interface VistaDelServidor {
  lista: string;
  campos: { campo: string; etiqueta: string }[];
  filtros: { nombre: string; campo: string }[];
  agrupaciones: { etiqueta: string; campo: string }[];
}

export interface OpcionesOrigenHttp<T> {
  modulo: string;
  lista: string;
  id: (fila: T) => string;
  sumables?: readonly string[];
  etiqueta?: (campo: string, valor: string) => string;
  baseUrl?: string;
}

/**
 * La vista del servidor como `SearchView` para el panel de búsqueda y el modo en memoria: la búsqueda
 * libre sobre sus campos, cada filtro con nombre por el `_filtros` que el servidor calculó para la fila
 * (incluidos los que dependen del usuario, como "Mis pedidos", D-151) y cada agrupación por su campo.
 */
export function vistaDesdeServidor<T>(v: VistaDelServidor): SearchView<ConFiltros<T>> {
  const leer = (f: ConFiltros<T>, campo: string) => {
    const valor = (f as Record<string, unknown>)[campo];
    return valor === null || valor === undefined ? undefined : String(valor);
  };
  return {
    campos: v.campos.map(c => ({ etiqueta: c.etiqueta, valor: f => leer(f, c.campo) })),
    filtros: v.filtros.map(x => ({ nombre: x.nombre, campo: x.campo, condicion: f => (f._filtros ?? []).includes(x.nombre) })),
    agrupaciones: v.agrupaciones.map(a => ({
      etiqueta: a.etiqueta,
      // `activo` agrupa como lo muestra la lista, no como true o false.
      clave: f => (a.campo === 'activo' ? ((f as Record<string, unknown>)['activo'] ? 'Activo' : 'Archivado') : (leer(f, a.campo) ?? '')),
    })),
  };
}

/** Mientras llega la vista del servidor, el panel se pinta sin campos, filtros ni agrupaciones. */
export const vistaVacia = <T>(): SearchView<T> => ({ campos: [], filtros: [], agrupaciones: [] });

/**
 * Origen de lista híbrido (contratos visuales §4.1, D-151, FR-029).
 * Pide el conjunto completo; si cabe en el umbral (completo === true), delega en
 * OrigenEnMemoria sin hacer peticiones adicionales al filtrar, agrupar, ordenar o paginar.
 * Si excede el umbral (completo === false), envía cada consulta al servidor.
 * Tras un cambio propio, `invalidar()` hace que la siguiente consulta vuelva a pedir el conjunto.
 */
export class OrigenHttp<T extends Record<string, unknown>> implements OrigenDeLista<T> {
  private conjunto: Promise<OrigenEnMemoria<ConFiltros<T>> | null> | null = null;
  private vistaCargada: Promise<SearchView<ConFiltros<T>>> | null = null;
  private readonly baseUrl: string;

  constructor(private readonly opciones: OpcionesOrigenHttp<T>) {
    this.baseUrl = opciones.baseUrl ?? `/api/v1/${opciones.modulo}/${opciones.lista}`;
  }

  /** La vista de búsqueda del servidor; se pide una vez. */
  vista(): Promise<SearchView<T>> {
    this.vistaCargada ??= pedirApi<VistaDelServidor>(`${this.baseUrl}/vista`).then(v => vistaDesdeServidor<T>(v));
    this.vistaCargada.catch(() => (this.vistaCargada = null));
    return this.vistaCargada as Promise<SearchView<T>>;
  }

  /** Olvida el conjunto: la siguiente consulta lo vuelve a pedir. */
  invalidar(): void {
    this.conjunto = null;
  }

  async consultar(consulta: ConsultaLista): Promise<ResultadoLista<T>> {
    this.conjunto ??= this.cargarConjunto();
    const enMemoria = await this.conjunto;
    if (enMemoria) {
      return (await enMemoria.consultar(consulta)) as unknown as ResultadoLista<T>;
    }
    return this.consultarEnServidor(consulta);
  }

  /** En memoria si el conjunto cabe en el umbral; `null` para consultar al servidor. */
  private async cargarConjunto(): Promise<OrigenEnMemoria<ConFiltros<T>> | null> {
    const res = await respuestaApi(`${this.baseUrl}/conjunto`, { method: 'POST', body: JSON.stringify({}) });
    if (!res.ok) {
      this.conjunto = null; // un fallo no deja la lista en modo servidor para siempre
      throw new Error(`Error al pedir el conjunto de la lista: ${res.status}`);
    }
    const respuesta = (await res.json()) as RespuestaConjunto<T>;
    if (!respuesta.completo || !respuesta.filas) return null;
    const filas = respuesta.filas;
    return new OrigenEnMemoria<ConFiltros<T>>({
      datos: () => filas,
      id: f => this.opciones.id(f),
      vista: (await this.vista()) as SearchView<ConFiltros<T>>,
      sumables: this.opciones.sumables,
      etiqueta: this.opciones.etiqueta,
    });
  }

  private async consultarEnServidor(consulta: ConsultaLista): Promise<ResultadoLista<T>> {
    const res = await respuestaApi(`${this.baseUrl}/consulta`, { method: 'POST', body: JSON.stringify(consulta) });

    if (!res.ok) {
      throw new Error(`Error en consulta de lista: ${res.status} ${res.statusText}`);
    }

    return (await res.json()) as ResultadoLista<T>;
  }
}
