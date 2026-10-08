import { ColumnaLista, esNumerica, leerColumna } from './columnas';

/**
 * Exporta filas a `.xlsx` en el navegador (spec 011, research R-06): las columnas en el orden dado,
 * los números como números y las fechas `dd/mm/yyyy`. La librería se carga solo al exportar.
 */
export async function exportarExcel<T>(nombre: string, columnas: ColumnaLista<T>[], filas: readonly T[]): Promise<void> {
  const hoja = [
    columnas.map(c => c.titulo),
    ...filas.map(f => columnas.map(c => {
      const v = leerColumna(c, f);
      if (v == null) return null;
      if (esNumerica(c.tipo)) return Number(v);
      if (v instanceof Date) return v;
      return c.texto ? c.texto(f) : String(v);
    })),
  ];
  const { default: writeExcelFile } = await import('write-excel-file/browser');
  await writeExcelFile(hoja, { dateFormat: 'dd/mm/yyyy' }).toFile(`${nombre}-${new Date().toISOString().slice(0, 10)}.xlsx`);
}
