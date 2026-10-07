import { describe, expect, it } from 'vitest';
import { fechaCorta, fechaGuion, fechaHora, formatN, horaCorta, n0, n1 } from './numero';

/** Valores medidos con .NET 8 en la cultura del prototipo (es-419). */
describe('formatos de .NET', () => {
  it('números N0, N1 y N', () => {
    expect(n1(1234567.891)).toBe('1,234,567.9');
    expect(n0(0.5)).toBe('1');
    expect(n0(1.5)).toBe('2');
    expect(n0(2.5)).toBe('3');
    expect(formatN(1234.5)).toBe('1,234.500');
    expect(n1(2516.25)).toBe('2,516.3');
  });

  it('fechas y hora', () => {
    const d = new Date(2026, 8, 23, 19, 5);
    expect(fechaCorta(d)).toBe('23 sept 26');
    expect(fechaGuion(new Date(2026, 4, 5))).toBe('05-may-26');
    expect(fechaHora(d)).toBe('23/09/2026 19:05');
    expect(horaCorta(d)).toBe('7:05 p.m.');
  });
});
