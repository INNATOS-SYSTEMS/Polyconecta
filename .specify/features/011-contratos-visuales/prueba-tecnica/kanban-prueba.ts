import { Component, inject, signal, TemplateRef, viewChild } from '@angular/core';
import { CdkDrag, CdkDragDrop, CdkDropList, CdkDropListGroup } from '@angular/cdk/drag-drop';
import { Dialog, DialogRef } from '@angular/cdk/dialog';

interface Tarjeta { id: string; folio: string; estado: string }
interface Etapa { estado: string; titulo: string }
/** Transiciones con nombre: de → a. Las que piden datos abren su diálogo. */
const TRANSICIONES: Record<string, { nombre: string; pideDatos: boolean }> = {
  'Borrador>Confirmado': { nombre: 'Confirmar', pideDatos: false },
  'Confirmado>Autorizado': { nombre: 'Autorizar', pideDatos: true },
  'Autorizado>Hecho': { nombre: 'Terminar', pideDatos: false },
};

@Component({
  selector: 'app-kanban-prueba',
  imports: [CdkDropListGroup, CdkDropList, CdkDrag],
  template: `
    <div cdkDropListGroup class="d-flex gap-3" data-prueba="kanban">
      @for (e of etapas; track e.estado) {
        <div class="border rounded p-2" style="width:220px;min-height:240px" [attr.data-prueba]="'etapa-' + e.estado"
             cdkDropList [id]="e.estado" [cdkDropListData]="e.estado" (cdkDropListDropped)="soltar($event)">
          <div class="fw-bold mb-2">{{ e.titulo }} <span [attr.data-prueba]="'cuenta-' + e.estado">{{ deEtapa(e.estado).length }}</span></div>
          @for (t of deEtapa(e.estado); track t.id) {
            <div cdkDrag [cdkDragData]="t" class="border rounded bg-white p-2 mb-2" [attr.data-prueba]="'tarjeta-' + t.folio">{{ t.folio }}</div>
          }
        </div>
      }
    </div>
    <div data-prueba="motivo" class="text-danger mt-2">{{ motivo() }}</div>
    <ng-template #firma>
      <div class="bg-white border rounded p-3" data-prueba="dialogo-firma">
        <p>Firma para autorizar</p>
        <button type="button" class="btn btn-primary btn-sm" data-prueba="firmar" (click)="dialogo?.close(true)">Firmar</button>
        <button type="button" class="btn btn-outline-secondary btn-sm" data-prueba="cancelar-firma" (click)="dialogo?.close(false)">Cancelar</button>
      </div>
    </ng-template>`,
})
export class KanbanPrueba {
  private readonly dialog = inject(Dialog);
  private readonly firma = viewChild.required<TemplateRef<unknown>>('firma');
  dialogo?: DialogRef<boolean>;
  readonly etapas: Etapa[] = [
    { estado: 'Borrador', titulo: 'Borrador' }, { estado: 'Confirmado', titulo: 'Confirmado' },
    { estado: 'Autorizado', titulo: 'Autorizado' }, { estado: 'Hecho', titulo: 'Hecho' },
  ];
  readonly tarjetas = signal<Tarjeta[]>([
    { id: '1', folio: 'PV-1', estado: 'Borrador' }, { id: '2', folio: 'PV-2', estado: 'Borrador' },
    { id: '3', folio: 'PV-3', estado: 'Confirmado' },
  ]);
  readonly motivo = signal('');

  deEtapa(estado: string): Tarjeta[] { return this.tarjetas().filter(t => t.estado === estado); }

  async soltar(ev: CdkDragDrop<string, string, Tarjeta>): Promise<void> {
    const t = ev.item.data, destino = ev.container.data;
    if (t.estado === destino) return;
    const tr = TRANSICIONES[`${t.estado}>${destino}`];
    if (!tr) { this.motivo.set(`No se puede pasar de ${t.estado} a ${destino}`); return; }
    if (tr.pideDatos) {
      this.dialogo = this.dialog.open<boolean>(this.firma());
      const ok = await new Promise<boolean | undefined>(r => this.dialogo!.closed.subscribe(r));
      if (!ok) { this.motivo.set(`${tr.nombre} cancelado`); return; }
    }
    this.motivo.set('');
    this.tarjetas.update(ts => ts.map(x => x.id === t.id ? { ...x, estado: destino } : x));
  }
}
