import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { OrigenHttp } from './origen-http';
import { consultaInicial } from './origen';

type Fila = { id: string; nombre: string; activo: boolean; estado: string };

const filas = [
  { id: '1', nombre: 'Ana', activo: true, estado: 'Borrador', _filtros: ['Activos', 'Mis pedidos'] },
  { id: '2', nombre: 'Carlos', activo: false, estado: 'Confirmado', _filtros: ['Archivados'] },
  { id: '3', nombre: 'Beatriz', activo: true, estado: 'Confirmado', _filtros: ['Activos'] },
];

const vista = {
  lista: 'plataforma.usuarios',
  campos: [{ campo: 'nombre', etiqueta: 'Nombre' }],
  filtros: [
    { nombre: 'Activos', campo: 'Estado' },
    { nombre: 'Archivados', campo: 'Estado' },
    { nombre: 'Mis pedidos', campo: 'Responsable' },
  ],
  agrupaciones: [
    { etiqueta: 'Estado', campo: 'activo' },
    { etiqueta: 'Etapa', campo: 'estado' },
  ],
};

const json = (cuerpo: unknown, status = 200) => new Response(JSON.stringify(cuerpo), { status });

describe('OrigenHttp', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  const origen = () => new OrigenHttp<Fila>({ modulo: 'plataforma', lista: 'usuarios', id: f => f.id });

  /** Responde la vista y el conjunto; cualquier otra ruta es un error de la prueba. */
  const servidor = (conjunto: unknown) =>
    fetchMock.mockImplementation(async (url: string) => {
      if (url.endsWith('/vista')) return json(vista);
      if (url.endsWith('/conjunto')) return json(conjunto);
      if (url.endsWith('/consulta')) return json({ filas: [filas[0]], grupos: null, total: 1, totales: {} });
      throw new Error(`Petición inesperada: ${url}`);
    });

  const pedidas = (fin: string) => fetchMock.mock.calls.filter(([url]) => (url as string).endsWith(fin)).length;

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('D-151: tras el conjunto, filtrar, buscar, agrupar, ordenar y paginar no hacen ninguna petición', async () => {
    servidor({ completo: true, total: 3, filas });
    const o = origen();

    await o.consultar(consultaInicial());
    const despuesDelConjunto = fetchMock.mock.calls.length;

    const filtrada = await o.consultar(consultaInicial({ nombrados: ['Activos'] }));
    const buscada = await o.consultar(consultaInicial({ busqueda: 'bea' }));
    const agrupada = await o.consultar(consultaInicial({ agruparPor: ['Estado', 'Etapa'] }));
    const ordenada = await o.consultar(consultaInicial({ orden: [{ campo: 'nombre', desc: true }] }));
    const pagina2 = await o.consultar(consultaInicial({ tamano: 2, pagina: 1 }));

    expect(fetchMock.mock.calls.length).toBe(despuesDelConjunto);
    expect(pedidas('/conjunto')).toBe(1);
    expect(pedidas('/vista')).toBe(1);
    expect(filtrada.filas.map(f => f.id)).toEqual(['1', '3']);
    expect(buscada.filas.map(f => f.id)).toEqual(['3']);
    expect(agrupada.grupos?.map(g => g.valor).sort()).toEqual(['Activo', 'Archivado']);
    expect(ordenada.filas.map(f => f.nombre)).toEqual(['Carlos', 'Beatriz', 'Ana']);
    expect(pagina2.filas).toHaveLength(1);
  });

  it('un filtro con nombre que depende del usuario se evalúa con el _filtros de la fila ("Mis pedidos")', async () => {
    servidor({ completo: true, total: 3, filas });

    const r = await origen().consultar(consultaInicial({ nombrados: ['Mis pedidos'] }));

    expect(r.filas.map(f => f.id)).toEqual(['1']);
  });

  it('sobre el umbral, cada consulta va al servidor', async () => {
    servidor({ completo: false, total: 6000 });
    const o = origen();

    await o.consultar(consultaInicial());
    await o.consultar(consultaInicial({ busqueda: 'ana' }));

    expect(pedidas('/conjunto')).toBe(1);
    expect(pedidas('/consulta')).toBe(2);
    const cuerpo = JSON.parse((fetchMock.mock.calls.find(([u]) => (u as string).endsWith('/consulta'))![1] as RequestInit).body as string);
    expect(cuerpo.busqueda).toBeNull();
  });

  it('invalidar() vuelve a pedir el conjunto tras un cambio propio', async () => {
    servidor({ completo: true, total: 3, filas });
    const o = origen();

    await o.consultar(consultaInicial());
    o.invalidar();
    await o.consultar(consultaInicial());

    expect(pedidas('/conjunto')).toBe(2);
    expect(pedidas('/vista')).toBe(1);
  });

  it('un conjunto fallido no deja la lista en modo servidor: la siguiente consulta lo vuelve a pedir', async () => {
    fetchMock.mockImplementation(async (url: string) => (url.endsWith('/vista') ? json(vista) : json({}, 500)));
    const o = origen();

    await expect(o.consultar(consultaInicial())).rejects.toThrow();
    servidor({ completo: true, total: 3, filas });
    const r = await o.consultar(consultaInicial());

    expect(r.total).toBe(3);
    expect(pedidas('/consulta')).toBe(0);
  });

  it('la vista del servidor llega al panel como SearchView', async () => {
    servidor({ completo: true, total: 3, filas });

    const v = await origen().vista();

    expect(v.campos.map(c => c.etiqueta)).toEqual(['Nombre']);
    expect(v.filtros.map(f => f.nombre)).toEqual(['Activos', 'Archivados', 'Mis pedidos']);
    expect(v.agrupaciones.map(a => a.etiqueta)).toEqual(['Estado', 'Etapa']);
  });
});
