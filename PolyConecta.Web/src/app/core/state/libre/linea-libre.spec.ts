import { describe, expect, it } from 'vitest';
import { ProductRef } from '../../models/inventario';
import { crearLineaLibre, siguienteFolio, validarLineaLibre } from './linea-libre';

const bolsa: ProductRef = { clave: 'PT1113 C567', nombre: 'BOLSA MEDIANA', clasificacion: 'Bolsa', unidad: 'MIL' };

describe('línea libre (D-127)', () => {
  it('toma la unidad base del producto', () => {
    expect(crearLineaLibre(bolsa, 12).linea).toEqual({ clave: 'PT1113 C567', producto: 'BOLSA MEDIANA', cantidad: 12, unidad: 'MIL' });
  });

  it('no se guarda sin producto del catálogo', () => {
    expect(crearLineaLibre(undefined, 5).error).toBe('Elija un producto del catálogo de CONTPAQi.');
  });

  it('no se guarda sin cantidad mayor que cero', () => {
    expect(crearLineaLibre(bolsa, 0).error).toBe('La cantidad debe ser mayor que cero.');
    expect(crearLineaLibre(bolsa, -3).error).toBe('La cantidad debe ser mayor que cero.');
    expect(crearLineaLibre(bolsa, Number.NaN).error).toBe('La cantidad debe ser mayor que cero.');
  });

  it('rechaza una unidad distinta a la del producto: no hay conversión', () => {
    expect(validarLineaLibre(bolsa, 10, 'KGS')).toBe('La unidad de PT1113 C567 es MIL: no se captura otra ni se convierte.');
    expect(validarLineaLibre(bolsa, 10, 'mil')).toBeUndefined();
  });

  it('numera folios consecutivos por prefijo', () => {
    expect(siguienteFolio('BOL', ['BOL-2026-0001', 'EXT-2026-0009', 'BOL-2026-0006'])).toBe('BOL-2026-0007');
    expect(siguienteFolio('PV', [])).toBe('PV-2026-0001');
  });
});
