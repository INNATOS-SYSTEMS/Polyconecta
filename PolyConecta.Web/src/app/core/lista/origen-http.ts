import { ConsultaLista, OrigenDeLista, ResultadoLista } from './origen';
import { OrigenEnMemoria } from './origen-en-memoria';
import { SearchView } from '../search/search-view';

export interface RespuestaConjunto<T> {
  completo: boolean;
  total: number;
  generado?: string;
  filas?: (T & { _filtros?: string[] })[];
}

export interface OpcionesOrigenHttp<T> {
  modulo: string;
  lista: string;
  id: (fila: T) => string;
  buscables?: readonly string[];
  vista?: SearchView<T>;
  sumables?: readonly string[];
  etiqueta?: (campo: string, valor: string) => string;
  baseUrl?: string;
}

/**
 * Origen de lista híbrido (contratos visuales §4.1, D-151, FR-029).
 * Pide el conjunto completo; si cabe en el umbral (completo === true), delega en
 * OrigenEnMemoria sin hacer peticiones adicionales al filtrar, agrupar, ordenar o paginar.
 * Si excede el umbral (completo === false), envía cada consulta al servidor.
 */
export class OrigenHttp<T extends Record<string, unknown>> implements OrigenDeLista<T> {
  private conjunto: (T & { _filtros?: string[] })[] | null = null;
  private delegadoMemoria: OrigenEnMemoria<T & { _filtros?: string[] }> | null = null;
  private modoServidor = false;
  private readonly baseUrl: string;

  constructor(private readonly opciones: OpcionesOrigenHttp<T>) {
    this.baseUrl = opciones.baseUrl ?? `/api/v1/${opciones.modulo}/${opciones.lista}`;
  }

  /**
   * Invalida el conjunto cargado para forzar una nueva lectura en la siguiente consulta.
   */
  invalidar(): void {
    this.conjunto = null;
    this.delegadoMemoria = null;
    this.modoServidor = false;
  }

  async consultar(consulta: ConsultaLista): Promise<ResultadoLista<T>> {
    if (this.conjunto === null && !this.modoServidor) {
      await this.cargarConjunto();
    }

    if (this.delegadoMemoria) {
      return this.consultarEnMemoria(consulta);
    }

    return this.consultarEnServidor(consulta);
  }

  private async cargarConjunto(): Promise<void> {
    try {
      const res = await fetch(`${this.baseUrl}/conjunto`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Requested-With': 'PolyConecta',
        },
        body: JSON.stringify({}),
        credentials: 'same-origin',
      });

      if (!res.ok) {
        this.modoServidor = true;
        return;
      }

      const respuesta = (await res.json()) as RespuestaConjunto<T>;
      if (respuesta.completo && respuesta.filas) {
        this.conjunto = respuesta.filas;
        this.delegadoMemoria = new OrigenEnMemoria<T & { _filtros?: string[] }>({
          datos: () => this.conjunto ?? [],
          id: f => this.opciones.id(f),
          buscables: this.opciones.buscables,
          vista: this.opciones.vista as unknown as SearchView<T & { _filtros?: string[] }>,
          sumables: this.opciones.sumables,
          etiqueta: this.opciones.etiqueta,
        });
      } else {
        this.modoServidor = true;
      }
    } catch {
      this.modoServidor = true;
    }
  }

  private async consultarEnMemoria(consulta: ConsultaLista): Promise<ResultadoLista<T>> {
    if (!this.delegadoMemoria || !this.conjunto) {
      return { filas: [], grupos: null, total: 0, totales: {} };
    }

    // Si hay nombrados y las filas tienen _filtros, pre-filtramos por _filtros si la vista no lo resolvió
    if (consulta.nombrados.length > 0 && !this.opciones.vista) {
      const filasFiltradas = this.conjunto.filter(f => {
        const filtrosFila = f._filtros ?? [];
        return consulta.nombrados.some(n => filtrosFila.includes(n));
      });
      const tempDelegado = new OrigenEnMemoria<T & { _filtros?: string[] }>({
        datos: () => filasFiltradas,
        id: f => this.opciones.id(f),
        buscables: this.opciones.buscables,
        sumables: this.opciones.sumables,
        etiqueta: this.opciones.etiqueta,
      });
      const res = await tempDelegado.consultar({ ...consulta, nombrados: [] });
      return res as unknown as ResultadoLista<T>;
    }

    const res = await this.delegadoMemoria.consultar(consulta);
    return res as unknown as ResultadoLista<T>;
  }

  private async consultarEnServidor(consulta: ConsultaLista): Promise<ResultadoLista<T>> {
    const res = await fetch(`${this.baseUrl}/consulta`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Requested-With': 'PolyConecta',
      },
      body: JSON.stringify(consulta),
      credentials: 'same-origin',
    });

    if (!res.ok) {
      throw new Error(`Error en consulta de lista: ${res.status} ${res.statusText}`);
    }

    return (await res.json()) as ResultadoLista<T>;
  }
}
