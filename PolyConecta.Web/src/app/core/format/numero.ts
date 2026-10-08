/**
 * Formatos de .NET que usa el prototipo, con la cultura con la que corre (es-419, la del sistema;
 * medidos con un programa de .NET 8 el 2026-10-06): coma de miles, punto decimal, redondeo
 * alejándose de cero, meses abreviados en español y "a.m."/"p.m.".
 */
const NUMERO = (decimales: number) =>
  new Intl.NumberFormat('en-US', { minimumFractionDigits: decimales, maximumFractionDigits: decimales, roundingMode: 'halfExpand' } as Intl.NumberFormatOptions);

/** `N{decimales}` de .NET. Sin decimales explícitos (`N`), es-419 usa 3. */
export function formatN(valor: number, decimales = 3): string {
  return NUMERO(decimales).format(valor);
}

export const n0 = (valor: number): string => formatN(valor, 0);
export const n1 = (valor: number): string => formatN(valor, 1);
export const n2 = (valor: number): string => formatN(valor, 2);

const MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sept', 'oct', 'nov', 'dic'];
const dos = (n: number) => String(n).padStart(2, '0');

/** `dd MMM yy`: 23 sept 26. */
export const fechaCorta = (d: Date): string => `${dos(d.getDate())} ${MESES[d.getMonth()]} ${dos(d.getFullYear() % 100)}`;

/** Como el campo de fecha (contratos visuales §1.6): 7 oct 2026. */
export const fechaCampo = (d: Date): string => `${d.getDate()} ${MESES[d.getMonth()]} ${d.getFullYear()}`;

/** `dd-MMM-yy`: 05-may-26. */
export const fechaGuion = (d: Date): string => `${dos(d.getDate())}-${MESES[d.getMonth()]}-${dos(d.getFullYear() % 100)}`;

/** `dd/MM/yyyy HH:mm`: 23/09/2026 19:05. */
export const fechaHora = (d: Date): string =>
  `${dos(d.getDate())}/${dos(d.getMonth() + 1)}/${d.getFullYear()} ${dos(d.getHours())}:${dos(d.getMinutes())}`;

/** `h:mm tt`: 7:05 p.m. */
export const horaCorta = (d: Date): string => {
  const h = d.getHours();
  return `${h % 12 === 0 ? 12 : h % 12}:${dos(d.getMinutes())} ${h < 12 ? 'a.m.' : 'p.m.'}`;
};

/**
 * Orden de cadenas de .NET con la cultura del prototipo (OrderBy sin comparador): ICU, es-419. El
 * orden ordinal difiere, por ejemplo, con guiones y mayúsculas.
 */
export const ordenCultural = new Intl.Collator('es-419').compare;
