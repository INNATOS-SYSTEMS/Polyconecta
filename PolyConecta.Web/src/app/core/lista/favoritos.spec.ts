import { describe, expect, it } from 'vitest';
import { Favorito, FavoritosEnNavegador } from './favoritos';

class AlmacenFalso implements Storage {
  private datos = new Map<string, string>();
  constructor(private readonly falla = false) {}
  get length(): number { return this.datos.size; }
  clear(): void { this.datos.clear(); }
  getItem(k: string): string | null { return this.datos.get(k) ?? null; }
  key(i: number): string | null { return [...this.datos.keys()][i] ?? null; }
  removeItem(k: string): void { this.datos.delete(k); }
  setItem(k: string, v: string): void {
    if (this.falla) throw new Error('QuotaExceededError');
    this.datos.set(k, v);
  }
}

const favorito = (id: string, nombre: string, porOmision = false): Favorito => ({
  id, lista: 'ventas.pedidos', nombre, filtros: [], busqueda: null, agruparPor: ['cliente'], orden: [], columnas: [], tamano: 80, porOmision,
});

describe('FavoritosEnNavegador', () => {
  it('guarda, lista y borra por lista', async () => {
    const almacen = new AlmacenFalso();
    const f = new FavoritosEnNavegador(() => almacen);
    await f.guardar(favorito('1', 'Por cliente'));
    await f.guardar({ ...favorito('2', 'Otro'), lista: 'produccion.of' });
    expect((await f.listar('ventas.pedidos')).map(x => x.nombre)).toEqual(['Por cliente']);
    expect(almacen.getItem('polyconecta.favoritos.ventas.pedidos')).toContain('Por cliente');
    await f.borrar('ventas.pedidos', '1');
    expect(await f.listar('ventas.pedidos')).toEqual([]);
  });

  it('deja un solo favorito por omisión por lista', async () => {
    const almacen = new AlmacenFalso();
    const f = new FavoritosEnNavegador(() => almacen);
    await f.guardar(favorito('1', 'A', true));
    await f.guardar(favorito('2', 'B', true));
    expect((await f.listar('ventas.pedidos')).map(x => [x.nombre, x.porOmision])).toEqual([['A', false], ['B', true]]);
  });

  it('exige nombre y no repite nombres en la misma lista', async () => {
    const almacen = new AlmacenFalso();
    const f = new FavoritosEnNavegador(() => almacen);
    await expect(f.guardar(favorito('1', '  '))).rejects.toThrow('El favorito necesita un nombre.');
    await f.guardar(favorito('1', 'Borradores'));
    await expect(f.guardar(favorito('2', 'borradores'))).rejects.toThrow('Ya existe un favorito llamado "borradores".');
  });

  it('si el navegador no deja guardar, avisa; si no deja leer, devuelve vacío', async () => {
    const f = new FavoritosEnNavegador(() => new AlmacenFalso(true));
    await expect(f.guardar(favorito('1', 'A'))).rejects.toThrow('No se pudo guardar el favorito en este navegador.');
    const sinAlmacen = new FavoritosEnNavegador(() => undefined);
    expect(await sinAlmacen.listar('ventas.pedidos')).toEqual([]);
    const roto = new AlmacenFalso();
    roto.setItem('polyconecta.favoritos.ventas.pedidos', '{no es json');
    expect(await new FavoritosEnNavegador(() => roto).listar('ventas.pedidos')).toEqual([]);
  });
});
