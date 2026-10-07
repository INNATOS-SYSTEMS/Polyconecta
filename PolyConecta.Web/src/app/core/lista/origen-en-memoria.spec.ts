import { describe, expect, it } from 'vitest';
import { consultaInicial } from './origen';
import { OrigenEnMemoria } from './origen-en-memoria';

interface Pedido { id: string; cliente: string; estado: string; total: number; fecha: Date }

const pedidos: Pedido[] = [
  { id: 'a', cliente: 'NORTE', estado: 'Borrador', total: 300, fecha: new Date(2026, 9, 1) },
  { id: 'b', cliente: 'SUR', estado: 'Hecho', total: 100, fecha: new Date(2026, 9, 3) },
  { id: 'c', cliente: 'NORTE', estado: 'Hecho', total: 200, fecha: new Date(2026, 9, 2) },
  { id: 'd', cliente: 'SUR', estado: 'Borrador', total: 100, fecha: new Date(2026, 9, 4) },
  { id: 'e', cliente: '', estado: 'Hecho', total: 50, fecha: new Date(2026, 9, 5) },
];
const origen = () => new OrigenEnMemoria<Pedido>({ datos: () => pedidos, id: p => p.id, sumables: ['total'], buscables: ['cliente', 'estado'] });
const ids = (filas: Pedido[]) => filas.map(f => f.id);

describe('OrigenEnMemoria', () => {
  it('pagina y cuenta el total', () => {
    const r = origen().resolver(consultaInicial({ tamano: 2, pagina: 1 }));
    expect(ids(r.filas)).toEqual(['c', 'd']);
    expect(r.total).toBe(5);
    expect(r.totales).toEqual({ total: 750 });
  });

  it('ordena por varios campos con orden estable en empates', () => {
    const r = origen().resolver(consultaInicial({ orden: [{ campo: 'total', desc: false }] }));
    expect(ids(r.filas)).toEqual(['e', 'b', 'd', 'c', 'a']);
    const r2 = origen().resolver(consultaInicial({ orden: [{ campo: 'cliente', desc: false }, { campo: 'total', desc: true }] }));
    expect(ids(r2.filas)).toEqual(['a', 'c', 'b', 'd', 'e']);
  });

  it('aplica cada operador; mismo campo con O y campos distintos con Y', () => {
    const o = origen();
    expect(ids(o.resolver(consultaInicial({ filtros: [{ campo: 'cliente', operador: 'contiene', valor: 'nor' }] })).filas)).toEqual(['a', 'c']);
    expect(ids(o.resolver(consultaInicial({ filtros: [{ campo: 'estado', operador: 'igual', valor: 'hecho' }] })).filas)).toEqual(['b', 'c', 'e']);
    expect(ids(o.resolver(consultaInicial({ filtros: [{ campo: 'total', operador: 'entre', valor: [100, 200] }] })).filas)).toEqual(['b', 'c', 'd']);
    expect(ids(o.resolver(consultaInicial({ filtros: [{ campo: 'fecha', operador: 'entre', valor: [new Date(2026, 9, 2), new Date(2026, 9, 3)] }] })).filas)).toEqual(['b', 'c']);
    expect(ids(o.resolver(consultaInicial({ filtros: [{ campo: 'estado', operador: 'en', valor: ['Borrador'] }] })).filas)).toEqual(['a', 'd']);
    const oy = o.resolver(consultaInicial({ filtros: [
      { campo: 'cliente', operador: 'igual', valor: 'NORTE' }, { campo: 'cliente', operador: 'igual', valor: 'SUR' },
      { campo: 'estado', operador: 'igual', valor: 'Hecho' },
    ] }));
    expect(ids(oy.filas)).toEqual(['b', 'c']);
  });

  it('busca en los campos buscables', () => {
    expect(ids(origen().resolver(consultaInicial({ busqueda: 'borr' })).filas)).toEqual(['a', 'd']);
  });

  it('agrupa en dos niveles con conteo y totales, y abre un grupo', () => {
    const o = origen();
    const nivel1 = o.resolver(consultaInicial({ agruparPor: ['cliente', 'estado'] }));
    expect(nivel1.filas).toEqual([]);
    expect(nivel1.grupos!.map(g => [g.etiqueta, g.cantidad, g.totales['total']])).toEqual([['Ninguno', 1, 50], ['NORTE', 2, 500], ['SUR', 2, 200]]);
    const nivel2 = o.resolver(consultaInicial({ agruparPor: ['cliente', 'estado'], grupo: [{ campo: 'cliente', valor: 'NORTE' }] }));
    expect(nivel2.grupos!.map(g => [g.valor, g.cantidad])).toEqual([['Borrador', 1], ['Hecho', 1]]);
    const filas = o.resolver(consultaInicial({ agruparPor: ['cliente', 'estado'], grupo: [{ campo: 'cliente', valor: 'NORTE' }, { campo: 'estado', valor: 'Hecho' }] }));
    expect(ids(filas.filas)).toEqual(['c']);
    expect(filas.grupos).toBeNull();
  });

  it('pagina los grupos', () => {
    const r = origen().resolver(consultaInicial({ agruparPor: ['cliente'], tamano: 2, pagina: 1 }));
    expect(r.grupos!.map(g => g.valor)).toEqual(['SUR']);
    expect(r.total).toBe(3);
  });

  it('devuelve solo los ids pedidos', () => {
    expect(ids(origen().resolver(consultaInicial({ ids: ['d', 'a'] })).filas)).toEqual(['a', 'd']);
  });

  it('cuenta las consultas', async () => {
    const o = origen();
    await o.consultar(consultaInicial());
    await o.consultar(consultaInicial({ pagina: 1 }));
    expect(o.consultas).toBe(2);
  });

  it('responde 1,000 filas filtradas, ordenadas y agrupadas en menos de 100 ms', () => {
    const muchas = Array.from({ length: 1000 }, (_, i) => ({ id: `p${i}`, cliente: `C${i % 13}`, estado: ['Borrador', 'Hecho'][i % 2], total: (i * 37) % 500, fecha: new Date(2026, 0, 1 + (i % 300)) }));
    const o = new OrigenEnMemoria<Pedido>({ datos: () => muchas, id: p => p.id, sumables: ['total'], buscables: ['cliente'] });
    const inicio = performance.now();
    o.resolver(consultaInicial({ busqueda: 'c1', orden: [{ campo: 'total', desc: true }], agruparPor: ['cliente', 'estado'] }));
    o.resolver(consultaInicial({ orden: [{ campo: 'fecha', desc: false }], filtros: [{ campo: 'estado', operador: 'igual', valor: 'Hecho' }] }));
    expect(performance.now() - inicio).toBeLessThan(100);
  });
});

describe('OrigenEnMemoria con vista de búsqueda', () => {
  const vista = {
    campos: [{ etiqueta: 'Cliente', valor: (p: Pedido) => p.cliente }],
    filtros: [
      { nombre: 'Borrador', campo: 'Estado', condicion: (p: Pedido) => p.estado === 'Borrador' },
      { nombre: 'Hecho', campo: 'Estado', condicion: (p: Pedido) => p.estado === 'Hecho' },
      { nombre: 'Grandes', campo: 'Total', condicion: (p: Pedido) => p.total >= 200 },
    ],
    agrupaciones: [{ etiqueta: 'Estado', clave: (p: Pedido) => p.estado }],
  };
  const o = () => new OrigenEnMemoria<Pedido>({ datos: () => pedidos, id: p => p.id, vista, sumables: ['total'] });

  it('evalúa los filtros con nombre: O en el mismo campo, Y entre campos', () => {
    expect(ids(o().resolver(consultaInicial({ nombrados: ['Borrador', 'Hecho'] })).filas)).toEqual(['a', 'b', 'c', 'd', 'e']);
    expect(ids(o().resolver(consultaInicial({ nombrados: ['Hecho', 'Grandes'] })).filas)).toEqual(['c']);
  });

  it('busca en los campos de la vista y agrupa por la etiqueta de una agrupación', () => {
    expect(ids(o().resolver(consultaInicial({ busqueda: 'sur' })).filas)).toEqual(['b', 'd']);
    const r = o().resolver(consultaInicial({ agruparPor: ['Estado'] }));
    expect(r.grupos!.map(g => [g.valor, g.cantidad])).toEqual([['Borrador', 2], ['Hecho', 3]]);
    expect(ids(o().resolver(consultaInicial({ agruparPor: ['Estado'], grupo: [{ campo: 'Estado', valor: 'Borrador' }] })).filas)).toEqual(['a', 'd']);
  });
});
