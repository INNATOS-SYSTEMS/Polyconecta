import { NgTemplateOutlet } from '@angular/common';
import { Component, effect, inject, input, output, signal, TemplateRef, untracked } from '@angular/core';
import { CdkDrag, CdkDragDrop, CdkDropList, CdkDropListGroup } from '@angular/cdk/drag-drop';
import { Dialog } from '@angular/cdk/dialog';
import { firstValueFrom } from 'rxjs';
import { EtapaKanban, motivoSinTransicion, TransicionKanban } from '../../core/kanban/kanban';
import { consultaInicial, FiltroLista, OrigenDeLista } from '../../core/lista/origen';
import { abrirDialogo } from '../odoo-dialog/odoo-dialog';

interface Columna<T> {
  etapa: EtapaKanban;
  filas: T[];
  total: number;
}

/**
 * Kanban (spec 011, research R-03): una columna por etapa, cada una leída del mismo `OrigenDeLista` que
 * la lista. Arrastrar ejecuta la transición declarada; si no existe o no procede, la tarjeta regresa
 * con el motivo. Sin transiciones, solo agrupa y no se puede arrastrar.
 */
@Component({
  selector: 'pc-odoo-kanban',
  imports: [CdkDropListGroup, CdkDropList, CdkDrag, NgTemplateOutlet],
  template: `
    <div class="o_kanban_board" cdkDropListGroup [attr.data-kanban]="campoEtapa()">
      @for (col of columnas(); track col.etapa.valor) {
        @if (plegadas().has(col.etapa.valor)) {
          <div class="o_kanban_column o_kanban_plegada" [attr.data-etapa]="col.etapa.valor" (click)="alternarPlegada(col.etapa.valor)"
               role="button" tabindex="0" [attr.aria-label]="'Mostrar ' + col.etapa.titulo" (keydown.enter)="alternarPlegada(col.etapa.valor)">
            <div class="o_kanban_column_title"><span>{{ col.etapa.titulo }}</span> <span class="badge bg-secondary">{{ col.total }}</span></div>
          </div>
        } @else {
          <div class="o_kanban_column" [attr.data-etapa]="col.etapa.valor">
            <div class="o_kanban_column_title">
              <span (dblclick)="alternarPlegada(col.etapa.valor)">{{ col.etapa.titulo }}</span>
              <span class="badge bg-secondary" [attr.data-cuenta]="col.etapa.valor">{{ col.total }}</span>
            </div>
            @if (motivos()[col.etapa.valor]; as m) {
              <div class="o_kanban_motivo" role="alert" [attr.data-motivo]="col.etapa.valor">{{ m }}</div>
            }
            <div cdkDropList [id]="'etapa-' + col.etapa.valor" [cdkDropListData]="col.etapa.valor"
                 [cdkDropListDisabled]="transiciones().length === 0" (cdkDropListDropped)="soltar($event)">
              @for (f of col.filas; track idDe()(f)) {
                <div cdkDrag [cdkDragData]="f" [cdkDragDisabled]="transiciones().length === 0" class="kanban-card"
                     [attr.data-tarjeta]="idDe()(f)" (click)="abrir.emit(f)">
                  <ng-container *ngTemplateOutlet="tarjeta(); context: { $implicit: f }" />
                </div>
              }
            </div>
          </div>
        }
      }
    </div>
  `,
  styles: ':host { display: block; }',
})
export class OdooKanban<T> {
  private readonly dialog = inject(Dialog);

  readonly origen = input.required<OrigenDeLista<T>>();
  readonly campoEtapa = input.required<string>();
  readonly etapas = input.required<EtapaKanban[]>();
  readonly transiciones = input<TransicionKanban<T>[]>([]);
  readonly tarjeta = input.required<TemplateRef<{ $implicit: T }>>();
  readonly idDe = input.required<(fila: T) => string>();
  /** Lee la etapa actual de una fila, para saber de dónde viene al arrastrar. */
  readonly etapaDe = input.required<(fila: T) => string>();
  readonly nombrados = input<string[]>([]);
  readonly busqueda = input('');
  readonly filtros = input<FiltroLista[]>([]);

  readonly abrir = output<T>();
  readonly movida = output<{ fila: T; desde: string; hacia: string }>();
  readonly rechazada = output<{ fila: T; motivo: string }>();

  readonly columnas = signal<Columna<T>[]>([]);
  readonly motivos = signal<Record<string, string>>({});
  readonly plegadas = signal(new Set<string>());

  constructor() {
    effect(() => {
      this.plegadas.set(new Set(this.etapas().filter(e => e.plegada).map(e => e.valor)));
    });
    effect(() => {
      this.nombrados();
      this.busqueda();
      this.filtros();
      this.etapas();
      untracked(() => void this.cargar());
    });
  }

  /** Una consulta por etapa, con el mismo filtro que la lista. */
  async cargar(): Promise<void> {
    const columnas = await Promise.all(this.etapas().map(async etapa => {
      const r = await this.origen().consultar(consultaInicial({
        tamano: 200,
        nombrados: this.nombrados(),
        busqueda: this.busqueda().trim() || null,
        filtros: [...this.filtros(), { campo: this.campoEtapa(), operador: 'igual', valor: etapa.valor }],
      }));
      return { etapa, filas: r.filas, total: r.total };
    }));
    this.columnas.set(columnas);
  }

  alternarPlegada(valor: string): void {
    this.plegadas.update(p => {
      const n = new Set(p);
      if (!n.delete(valor)) n.add(valor);
      return n;
    });
  }

  async soltar(ev: CdkDragDrop<string, string, T>): Promise<void> {
    const fila = ev.item.data;
    const desde = this.etapaDe()(fila);
    const hacia = ev.container.data;
    if (desde === hacia) return;
    const motivo = await this.mover(fila, desde, hacia);
    if (motivo) {
      this.motivos.set({ [desde]: motivo });
      this.rechazada.emit({ fila, motivo });
      return;
    }
    this.motivos.set({});
    this.movida.emit({ fila, desde, hacia });
    await this.cargar();
  }

  /** Ejecuta la transición; devuelve el motivo si no procede. */
  async mover(fila: T, desde: string, hacia: string): Promise<string | undefined> {
    const tr = this.transiciones().find(t => t.desde === desde && t.hacia === hacia);
    if (!tr) return motivoSinTransicion(desde, hacia);
    let datos: unknown;
    if (tr.dialogo) {
      const ref = abrirDialogo<unknown>(this.dialog, tr.dialogo, { fila, transicion: tr.nombre });
      datos = await firstValueFrom(ref.closed);
      if (datos === undefined || datos === false) return `${tr.nombre}: cancelado`;
    }
    return tr.ejecutar(fila, datos);
  }
}
