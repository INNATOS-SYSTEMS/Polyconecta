import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { OrigenHttp } from './origen-http';
import { consultaInicial } from './origen';

describe('OrigenHttp', () => {
  const filasPrueba = [
    { id: '1', nombre: 'Ana', activo: true, _filtros: ['Activos'] },
    { id: '2', nombre: 'Carlos', activo: false, _filtros: ['Archivados'] },
    { id: '3', nombre: 'Beatriz', activo: true, _filtros: ['Activos'] },
  ];

  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('cuando el conjunto está completo, filtra y ordena en memoria sin peticiones subsecuentes (D-151)', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        completo: true,
        total: 3,
        filas: filasPrueba,
      }),
    });

    const origen = new OrigenHttp<{ id: string; nombre: string; activo: boolean }>({
      modulo: 'plataforma',
      lista: 'usuarios',
      id: f => f.id,
      buscables: ['nombre'],
    });

    // Primera consulta: llama a conjunto
    const res1 = await origen.consultar(consultaInicial());
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/v1/plataforma/usuarios/conjunto',
      expect.objectContaining({ method: 'POST' })
    );
    expect(res1.filas.length).toBe(3);

    // Segunda consulta: búsqueda en memoria, no debe llamar a fetch
    const res2 = await origen.consultar(consultaInicial({ busqueda: 'Beatriz' }));
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(res2.filas.length).toBe(1);
    expect(res2.filas[0].nombre).toBe('Beatriz');

    // Tercera consulta: paginación en memoria, tampoco llama a fetch
    const res3 = await origen.consultar(consultaInicial({ pagina: 0, tamano: 2 }));
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(res3.filas.length).toBe(2);

    // Cuarta consulta: filtrado por _filtros nombrados
    const res4 = await origen.consultar(consultaInicial({ nombrados: ['Archivados'] }));
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(res4.filas.length).toBe(1);
    expect(res4.filas[0].nombre).toBe('Carlos');
  });

  it('cuando el conjunto excede el umbral (completo === false), consulta al servidor', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        completo: false,
        total: 6000,
      }),
    });

    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        filas: [{ id: '99', nombre: 'Servidor', activo: true }],
        grupos: null,
        total: 1,
        totales: {},
      }),
    });

    const origen = new OrigenHttp<{ id: string; nombre: string; activo: boolean }>({
      modulo: 'plataforma',
      lista: 'usuarios',
      id: f => f.id,
    });

    const res = await origen.consultar(consultaInicial());
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      '/api/v1/plataforma/usuarios/conjunto',
      expect.anything()
    );
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      '/api/v1/plataforma/usuarios/consulta',
      expect.anything()
    );
    expect(res.filas[0].nombre).toBe('Servidor');
  });

  it('invalidar vuelve a pedir el conjunto en la siguiente consulta', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      json: async () => ({
        completo: true,
        total: 3,
        filas: filasPrueba,
      }),
    });

    const origen = new OrigenHttp<{ id: string; nombre: string; activo: boolean }>({
      modulo: 'plataforma',
      lista: 'usuarios',
      id: f => f.id,
    });

    await origen.consultar(consultaInicial());
    expect(fetchMock).toHaveBeenCalledTimes(1);

    origen.invalidar();

    await origen.consultar(consultaInicial());
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});
