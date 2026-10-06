/**
 * Formatos numéricos de .NET que usa el prototipo (`N0`, `N1`, `N2`), con separador de miles
 * y punto decimal. La fase 1 de la spec 001 los contrasta contra textos reales de Blazor.
 */
const CULTURA = 'es-MX';

export function formatN(valor: number, decimales: number): string {
  return valor.toLocaleString(CULTURA, { minimumFractionDigits: decimales, maximumFractionDigits: decimales });
}

export const n1 = (valor: number): string => formatN(valor, 1);
