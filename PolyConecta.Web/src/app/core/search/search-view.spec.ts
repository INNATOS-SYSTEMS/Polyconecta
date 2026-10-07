import { describe, expect, it } from 'vitest';
import { SearchView, aplicar } from './search-view';
import { CALIDAD, FABRICACION } from './views';
import { carteraExtrusion, ordenesSemilla } from '../seed/flujo';

interface Fila {
  folio: string;
  estado: string;
  proceso: string;
}

const VISTA: SearchView<Fila> = {
  campos: [{ etiqueta: 'Folio', valor: f => f.folio }],
  filtros: [
    { nombre: 'Borrador', campo: 'Estado', condicion: f => f.estado === 'Borrador' },
    { nombre: 'Hecho', campo: 'Estado', condicion: f => f.estado === 'Hecho' },
    { nombre: 'Bolseo', campo: 'Proceso', condicion: f => f.proceso === 'Bolseo' },
  ],
  agrupaciones: [],
};

const FILAS: Fila[] = [
  { folio: 'BOL-1', estado: 'Borrador', proceso: 'Bolseo' },
  { folio: 'BOL-2', estado: 'Hecho', proceso: 'Bolseo' },
  { folio: 'EXT-1', estado: 'Hecho', proceso: 'Extrusion' },
  { folio: 'EXT-2', estado: 'Planeado', proceso: 'Extrusion' },
];

describe('aplicar (FR-009)', () => {
  it('busca texto sin distinguir mayúsculas', () => {
    expect(aplicar(VISTA, FILAS, 'ext', []).map(f => f.folio)).toEqual(['EXT-1', 'EXT-2']);
  });

  it('O dentro del mismo campo', () => {
    expect(aplicar(VISTA, FILAS, '', ['Borrador', 'Hecho']).map(f => f.folio)).toEqual(['BOL-1', 'BOL-2', 'EXT-1']);
  });

  it('Y entre campos distintos', () => {
    expect(aplicar(VISTA, FILAS, '', ['Hecho', 'Bolseo']).map(f => f.folio)).toEqual(['BOL-2']);
  });

  it('sin campos declarados busca en la referencia', () => {
    const v: SearchView<Fila> = { ...VISTA, campos: [], referencia: f => f.folio };
    expect(aplicar(v, FILAS, 'bol-2', []).map(f => f.folio)).toEqual(['BOL-2']);
  });

  it('las vistas por modelo filtran la semilla como el prototipo', () => {
    const ordenes = [...ordenesSemilla(), ...carteraExtrusion()];
    expect(aplicar(FABRICACION, ordenes, '', ['Órdenes maestras', 'Bolseo']).map(o => o.folio)).toEqual(['BOL-2026-0001']);
    expect(aplicar(CALIDAD, ordenes, '', ['Con lotes en revisión']).map(o => o.folio)).toEqual(['IMP-2026-0001', 'EXT-2026-0001']);
  });
});
