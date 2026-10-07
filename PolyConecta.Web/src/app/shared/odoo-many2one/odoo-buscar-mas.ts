import { Component, inject, signal } from '@angular/core';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { OrigenDeLista } from '../../core/lista/origen';
import { ColumnaLista } from '../odoo-list/columnas';
import { OdooDialog } from '../odoo-dialog/odoo-dialog';
import { OdooList } from '../odoo-list/odoo-list';

interface DatosBuscarMas<T> {
  origen: OrigenDeLista<T>;
  aTexto: (r: T) => string;
  idDe: (r: T) => string;
  titulo: string;
}

/** "Buscar más…" del many2one: la lista completa en un diálogo; elegir una fila la devuelve. */
@Component({
  selector: 'pc-odoo-buscar-mas',
  imports: [OdooDialog, OdooList],
  template: `
    <pc-odoo-dialog [titulo]="datos.titulo" [textoPrimario]="null" (cancelar)="ref.close()">
      <input type="text" class="form-control form-control-sm mb-2" placeholder="Buscar…" aria-label="Buscar" data-buscar-mas
             (input)="busqueda.set($any($event.target).value)" />
      <pc-odoo-list lista="buscar-mas" [origen]="datos.origen" [columnas]="columnas" [idDe]="datos.idDe" [seleccion]="false"
                    [(busqueda)]="busqueda" (abrir)="ref.close($event)" />
    </pc-odoo-dialog>
  `,
})
export class OdooBuscarMas<T> {
  protected readonly ref = inject<DialogRef<T>>(DialogRef);
  protected readonly datos = inject<DatosBuscarMas<T>>(DIALOG_DATA);
  protected readonly busqueda = signal('');
  protected readonly columnas: ColumnaLista<T>[] = [{ campo: 'nombre', titulo: 'Nombre', texto: this.datos.aTexto, valor: this.datos.aTexto }];
}
