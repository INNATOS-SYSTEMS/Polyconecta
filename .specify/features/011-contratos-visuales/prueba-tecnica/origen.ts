/** Origen de datos de una lista (spec 011): la tabla pide cada vista; en memoria ahora, HTTP en F1. */
export interface ConsultaLista {
  pagina: number;
  tamano: number;
  orden: { campo: string; desc: boolean }[];
  filtros: Record<string, string>;
  agruparPor: string | null;
}

export interface GrupoLista {
  valor: string;
  cantidad: number;
  totales: Record<string, number>;
}

export interface ResultadoLista<T> {
  filas: T[];
  grupos: GrupoLista[] | null;
  total: number;
}

export interface OrigenDeLista<T> {
  consultar(consulta: ConsultaLista): Promise<ResultadoLista<T>>;
}

export class OrigenEnMemoria<T extends object> implements OrigenDeLista<T> {
  /** Cuántas consultas recibió: la prueba verifica que la tabla no ordena ni pagina por su cuenta. */
  consultas = 0;

  constructor(private readonly datos: readonly T[], private readonly sumables: readonly (keyof T & string)[]) {}

  async consultar(c: ConsultaLista): Promise<ResultadoLista<T>> {
    this.consultas++;
    const valor = (f: T, campo: string) => (f as Record<string, unknown>)[campo];
    let filas = this.datos.filter(f =>
      Object.entries(c.filtros).every(([campo, v]) => String(valor(f, campo)).toLowerCase().includes(v.toLowerCase())));
    for (const o of [...c.orden].reverse()) {
      filas = [...filas].sort((a, b) => {
        const x = valor(a, o.campo) as string | number, y = valor(b, o.campo) as string | number;
        const r = typeof x === 'number' && typeof y === 'number' ? x - y : String(x).localeCompare(String(y), 'es-MX');
        return o.desc ? -r : r;
      });
    }
    if (c.agruparPor) {
      const mapa = new Map<string, T[]>();
      for (const f of filas) {
        const k = String(valor(f, c.agruparPor));
        mapa.set(k, [...(mapa.get(k) ?? []), f]);
      }
      const grupos = [...mapa.entries()].map(([v, fs]) => ({
        valor: v,
        cantidad: fs.length,
        totales: Object.fromEntries(this.sumables.map(s => [s, fs.reduce((t, f) => t + Number(valor(f, s)), 0)])),
      }));
      return { filas: [], grupos: grupos.slice(c.pagina * c.tamano, (c.pagina + 1) * c.tamano), total: grupos.length };
    }
    return { filas: filas.slice(c.pagina * c.tamano, (c.pagina + 1) * c.tamano), grupos: null, total: filas.length };
  }
}
