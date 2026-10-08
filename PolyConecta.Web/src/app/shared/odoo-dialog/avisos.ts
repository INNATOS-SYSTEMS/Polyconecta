import { Injectable, signal } from '@angular/core';

export type TipoAviso = 'exito' | 'aviso' | 'error';

export interface Aviso {
  id: number;
  tipo: TipoAviso;
  texto: string;
}

/**
 * Avisos flotantes (spec 011): éxito y aviso se cierran solos a los 4 s; el de error, solo con su botón.
 * Los pinta `pc-odoo-avisos`, que vive una vez en el layout.
 */
@Injectable({ providedIn: 'root' })
export class AvisosService {
  static readonly DURACION_MS = 4000;
  private siguiente = 1;
  readonly avisos = signal<Aviso[]>([]);

  exito(texto: string): void { this.mostrar('exito', texto); }
  aviso(texto: string): void { this.mostrar('aviso', texto); }
  error(texto: string): void { this.mostrar('error', texto); }

  cerrar(id: number): void {
    this.avisos.update(a => a.filter(x => x.id !== id));
  }

  private mostrar(tipo: TipoAviso, texto: string): void {
    const id = this.siguiente++;
    this.avisos.update(a => [...a, { id, tipo, texto }]);
    if (tipo !== 'error') setTimeout(() => this.cerrar(id), AvisosService.DURACION_MS);
  }
}
