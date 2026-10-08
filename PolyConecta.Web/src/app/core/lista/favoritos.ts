import { FiltroLista, OrdenLista } from './origen';

/** Vista guardada de una lista: la forma de `SavedSearch` (04 §3, contratos visuales §4.2). */
export interface Favorito {
  id: string;
  /** Llave de la lista, por ejemplo `ventas.pedidos`. */
  lista: string;
  nombre: string;
  filtros: FiltroLista[];
  busqueda: string | null;
  agruparPor: string[];
  orden: OrdenLista[];
  columnas: { campo: string; visible: boolean }[];
  tamano: number;
  porOmision: boolean;
}

export interface AlmacenDeFavoritos {
  listar(lista: string): Promise<Favorito[]>;
  /** Rechaza si no se pudo guardar; la lista avisa y sigue funcionando. */
  guardar(favorito: Favorito): Promise<void>;
  borrar(lista: string, id: string): Promise<void>;
}

/**
 * Favoritos en el navegador (spec 011, research R-08) hasta que F1 los guarde por usuario en la base.
 * Lo guardado aquí no se migra. Toda lectura y escritura va en try/catch: el almacenamiento puede no
 * existir o estar bloqueado.
 */
export class FavoritosEnNavegador implements AlmacenDeFavoritos {
  constructor(private readonly almacen: () => Storage | undefined = () => globalThis.localStorage) {}

  private llave(lista: string): string {
    return `polyconecta.favoritos.${lista}`;
  }

  async listar(lista: string): Promise<Favorito[]> {
    try {
      const crudo = this.almacen()?.getItem(this.llave(lista));
      const favoritos = crudo ? (JSON.parse(crudo) as Favorito[]) : [];
      return Array.isArray(favoritos) ? favoritos : [];
    } catch {
      return [];
    }
  }

  async guardar(favorito: Favorito): Promise<void> {
    const nombre = favorito.nombre.trim();
    if (!nombre) throw new Error('El favorito necesita un nombre.');
    const actuales = (await this.listar(favorito.lista)).filter(f => f.id !== favorito.id);
    if (actuales.some(f => f.nombre.trim().toLowerCase() === nombre.toLowerCase())) throw new Error(`Ya existe un favorito llamado "${nombre}".`);
    // Solo uno por omisión por lista.
    const siguientes = [...actuales.map(f => (favorito.porOmision ? { ...f, porOmision: false } : f)), { ...favorito, nombre }];
    this.escribir(favorito.lista, siguientes);
  }

  async borrar(lista: string, id: string): Promise<void> {
    this.escribir(lista, (await this.listar(lista)).filter(f => f.id !== id));
  }

  private escribir(lista: string, favoritos: Favorito[]): void {
    try {
      const almacen = this.almacen();
      if (!almacen) throw new Error('sin almacenamiento');
      almacen.setItem(this.llave(lista), JSON.stringify(favoritos));
    } catch {
      throw new Error('No se pudo guardar el favorito en este navegador.');
    }
  }
}
