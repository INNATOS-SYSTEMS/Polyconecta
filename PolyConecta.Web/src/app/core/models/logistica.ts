export interface ShipmentLine {
  clave: string;
  producto: string;
  demanda: number;
  entregado: number;
  unidad: string;
  lotesSeleccionados: string[];
}

/**
 * Documento de logística (traslado, recepción o entrega). En el prototipo cada uno es un objeto
 * único; en la réplica, colecciones con su semilla (research R-02 de la spec 001).
 */
export abstract class DocumentoLogistica {
  folio = '';
  operacion = '';
  origen = '';
  destino = '';
  fechaProgramada = new Date(2026, 4, 5);
  fechaLimite = new Date(2026, 4, 6);
  contpaqId = '';
  step = 0;
  warning?: string;
  error?: string;
  lineas: ShipmentLine[] = [];
  libre = false;

  protected abstract readonly estados: readonly string[];

  get state(): string {
    return this.estados[Math.min(this.step, this.estados.length - 1)];
  }
}

export class InterplantTransfer extends DocumentoLogistica {
  protected readonly estados = ['Borrador', 'En espera de operación', 'En espera', 'Listo', 'Hecho'] as const;

  constructor(init: Partial<InterplantTransfer> = {}) {
    super();
    Object.assign(this, init);
  }
}

/**
 * Recepción: segundo paso del traspaso interplanta. Misma operación física que el traslado, pero
 * valida lo que entra a almacén, por eso origen y destino quedan invertidos.
 */
export class Reception extends DocumentoLogistica {
  protected readonly estados = ['Borrador', 'En espera', 'Listo', 'Hecho'] as const;

  constructor(init: Partial<Reception> = {}) {
    super();
    Object.assign(this, init);
  }
}

export class Delivery extends DocumentoLogistica {
  protected readonly estados = ['Borrador', 'En espera', 'Listo', 'Hecho'] as const;
  cliente = '';

  constructor(init: Partial<Delivery> = {}) {
    super();
    Object.assign(this, init);
  }
}
