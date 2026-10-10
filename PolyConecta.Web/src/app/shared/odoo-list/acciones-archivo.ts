import { AccionMasiva } from './columnas';

interface Archivable {
  id: number | string;
  activo: boolean;
}

/** Qué hace cada acción y cómo se nombra lo que archiva ("usuario", "usuarios"). */
export interface OperacionesDeArchivo {
  singular: string;
  plural: string;
  archivar(id: string): Promise<unknown>;
  restaurar(id: string): Promise<unknown>;
  exito(mensaje: string): void;
}

/**
 * "Archivar" y "Restaurar" de una lista (07 §1.1): cada una aplica solo a los seleccionados que cambian
 * (los activos o los archivados) y se deshabilita, con su razón, si ninguno cambia. Lo referenciado se
 * archiva, no se borra (04 §1).
 */
export function accionesDeArchivo(op: OperacionesDeArchivo): AccionMasiva[] {
  const cambiar = async (filas: Archivable[], hacer: (id: string) => Promise<unknown>, participio: string): Promise<void> => {
    let hechos = 0;
    try {
      for (const f of filas) {
        await hacer(String(f.id));
        hechos++;
      }
    } finally {
      if (hechos > 0) op.exito(hechos === 1 ? `1 ${op.singular} ${participio}.` : `${hechos} ${op.plural} ${participio}s.`);
    }
  };
  return [
    {
      nombre: 'Archivar', icono: 'archivar',
      razonDeshabilitada: filas => ((filas as Archivable[]).some(f => f.activo) ? null : `Los ${op.plural} seleccionados ya están archivados.`),
      ejecutar: (_ids, filas) => cambiar((filas as Archivable[]).filter(f => f.activo), op.archivar, 'archivado'),
    },
    {
      nombre: 'Restaurar', icono: 'reintentar',
      razonDeshabilitada: filas => ((filas as Archivable[]).some(f => !f.activo) ? null : `Ninguno de los ${op.plural} seleccionados está archivado.`),
      ejecutar: (_ids, filas) => cambiar((filas as Archivable[]).filter(f => !f.activo), op.restaurar, 'restaurado'),
    },
  ];
}
