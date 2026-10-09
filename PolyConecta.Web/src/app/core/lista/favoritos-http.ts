import { AlmacenDeFavoritos, Favorito } from './favoritos';
import { pedirApi } from '../sesion/api';

type Definicion = Omit<Favorito, 'id' | 'lista' | 'nombre' | 'porOmision'>;

interface FavoritoDelServidor {
  nombre: string;
  definicion: Partial<Definicion>;
  porOmision: boolean;
}

/**
 * Favoritos por usuario en la base (`/api/v1/plataforma/favoritos`, contracts/api-listas.md; L2-T029).
 * El servidor los identifica por nombre dentro de la lista, así que el `id` es el nombre: renombrar
 * guarda el nuevo y borra el anterior. Solo el dueño los ve.
 */
export class FavoritosHttp implements AlmacenDeFavoritos {
  private url(lista: string, nombre?: string): string {
    const base = `/api/v1/plataforma/favoritos/${encodeURIComponent(lista)}`;
    return nombre === undefined ? base : `${base}/${encodeURIComponent(nombre)}`;
  }

  async listar(lista: string): Promise<Favorito[]> {
    const favoritos = await pedirApi<FavoritoDelServidor[]>(this.url(lista));
    return favoritos.map(f => ({
      id: f.nombre,
      lista,
      nombre: f.nombre,
      porOmision: f.porOmision,
      filtros: f.definicion.filtros ?? [],
      nombrados: f.definicion.nombrados ?? [],
      busqueda: f.definicion.busqueda ?? null,
      agruparPor: f.definicion.agruparPor ?? [],
      orden: f.definicion.orden ?? [],
      columnas: f.definicion.columnas ?? [],
      tamano: f.definicion.tamano ?? 80,
    }));
  }

  async guardar(favorito: Favorito): Promise<void> {
    const nombre = favorito.nombre.trim();
    if (!nombre) throw new Error('El favorito necesita un nombre.');
    const actuales = await this.listar(favorito.lista);
    if (actuales.some(f => f.id !== favorito.id && f.nombre.toLowerCase() === nombre.toLowerCase())) {
      throw new Error(`Ya existe un favorito llamado "${nombre}".`);
    }
    const { filtros, nombrados, busqueda, agruparPor, orden, columnas, tamano } = favorito;
    await pedirApi(this.url(favorito.lista, nombre), {
      method: 'PUT',
      body: JSON.stringify({ definicion: { filtros, nombrados, busqueda, agruparPor, orden, columnas, tamano }, porOmision: favorito.porOmision }),
    });
    const anterior = actuales.find(f => f.id === favorito.id);
    if (anterior && anterior.nombre !== nombre) await this.borrar(favorito.lista, anterior.nombre);
  }

  async borrar(lista: string, id: string): Promise<void> {
    await pedirApi(this.url(lista, id), { method: 'DELETE' });
  }
}
