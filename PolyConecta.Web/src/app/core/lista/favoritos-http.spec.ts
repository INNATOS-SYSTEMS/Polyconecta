import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Favorito } from './favoritos';
import { FavoritosHttp } from './favoritos-http';

const RUTA = '/api/v1/plataforma/favoritos/ventas.pedidos';

const favorito = (cambios: Partial<Favorito> = {}): Favorito => ({
  id: '1',
  lista: 'ventas.pedidos',
  nombre: 'Por autorizar',
  filtros: [],
  nombrados: ['Confirmado'],
  busqueda: null,
  agruparPor: ['Cliente'],
  orden: [{ campo: 'folio', desc: true }],
  columnas: [{ campo: 'folio', visible: true }],
  tamano: 80,
  porOmision: true,
  ...cambios,
});

const json = (cuerpo: unknown, status = 200) => new Response(cuerpo === undefined ? null : JSON.stringify(cuerpo), { status });

describe('FavoritosHttp', () => {
  let fetchMock: ReturnType<typeof vi.fn>;
  const almacen = new FavoritosHttp();

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('lista los del usuario con su definición y el nombre como id', async () => {
    fetchMock.mockResolvedValueOnce(json([{ nombre: 'Por autorizar', porOmision: true, definicion: { nombrados: ['Confirmado'], agruparPor: ['Cliente'] } }]));

    const [f] = await almacen.listar('ventas.pedidos');

    expect(fetchMock).toHaveBeenCalledWith(RUTA, expect.anything());
    expect(f).toMatchObject({ id: 'Por autorizar', nombre: 'Por autorizar', porOmision: true, nombrados: ['Confirmado'], agruparPor: ['Cliente'], filtros: [], tamano: 80 });
  });

  it('guarda con PUT por nombre la definición completa, incluidos los filtros con nombre', async () => {
    fetchMock.mockResolvedValueOnce(json([])).mockResolvedValueOnce(json({}));

    await almacen.guardar(favorito());

    const [url, init] = fetchMock.mock.calls[1] as [string, RequestInit];
    expect(url).toBe(`${RUTA}/Por%20autorizar`);
    expect(init.method).toBe('PUT');
    const cuerpo = JSON.parse(init.body as string);
    expect(cuerpo.porOmision).toBe(true);
    expect(cuerpo.definicion).toMatchObject({ nombrados: ['Confirmado'], agruparPor: ['Cliente'], orden: [{ campo: 'folio', desc: true }], tamano: 80 });
  });

  it('no guarda un nombre repetido de otro favorito', async () => {
    fetchMock.mockResolvedValueOnce(json([{ nombre: 'Por autorizar', porOmision: false, definicion: {} }]));

    await expect(almacen.guardar(favorito({ id: 'nuevo' }))).rejects.toThrow('Ya existe');
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('borra por nombre', async () => {
    fetchMock.mockResolvedValueOnce(json(undefined, 204));

    await almacen.borrar('ventas.pedidos', 'Por autorizar');

    expect(fetchMock).toHaveBeenCalledWith(`${RUTA}/Por%20autorizar`, expect.objectContaining({ method: 'DELETE' }));
  });
});
