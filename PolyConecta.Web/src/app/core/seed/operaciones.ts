import { OperationType } from '../models/operaciones';
import { ALMACEN_MATERIA_PRIMA, WIP_PIM, WIP_STC } from './inventario';

/** Semilla de Services/StockOperationState.cs. */
export const RECOLECCION_PIM = 'PIM-REC-OUT';
export const RECOLECCION_STC = 'STC-REC-OUT';
export const SECUENCIA_INICIAL = 48213;

export const tiposSemilla = (): OperationType[] => [
  {
    codigo: RECOLECCION_PIM,
    nombre: 'Recolección de materia prima',
    origen: ALMACEN_MATERIA_PRIMA,
    destino: WIP_PIM,
    eventoContpaq: 'Traspaso de almacén MP → WIP al validar',
    reversa: 'PIM-REC-RET',
    esDevolucion: false,
  },
  {
    codigo: 'PIM-REC-RET',
    nombre: 'Devolución de recolección',
    origen: WIP_PIM,
    destino: ALMACEN_MATERIA_PRIMA,
    eventoContpaq: 'Traspaso de almacén WIP → MP al validar',
    esDevolucion: true,
  },
  {
    codigo: RECOLECCION_STC,
    nombre: 'Recolección de rollos a conversión',
    origen: 'SC/Stock/MP',
    destino: WIP_STC,
    eventoContpaq: 'Traspaso de almacén MP → WIP al validar',
    reversa: 'STC-REC-RET',
    esDevolucion: false,
  },
  {
    codigo: 'STC-REC-RET',
    nombre: 'Devolución de recolección',
    origen: WIP_STC,
    destino: 'SC/Stock/MP',
    eventoContpaq: 'Traspaso de almacén WIP → MP al validar',
    esDevolucion: true,
  },
];
