import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { n1 } from '../../../core/format/numero';
import { InventoryState } from '../../../core/state/inventory-state';
import { LineaLibre, crearLineaLibre } from '../../../core/state/libre/linea-libre';
import { OperacionesLibres, PLANTAS, Planta } from '../../../core/state/libre/operaciones-libres';
import { HojaNueva } from '../../../shared/hoja-nueva/hoja-nueva';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { OdooTabs, PcPestana } from '../../../shared/odoo-tabs/odoo-tabs';
import { ETAPAS_RECOLECCION } from '../recoleccion-acciones';
import { LineDraft, OdooLineCapture, emptyDraft } from '../../../shared/odoo-line-capture/odoo-line-capture';

type Tipo = 'recoleccion' | 'devolucion';

/**
 * "Nueva" recolección o devolución (FR-012) con la estructura completa del documento (D-136, spec 011
 * P3): planta y líneas con cantidad y la unidad del producto, que se guardan con el maestro; el chatter
 * se activa al guardar. Los lotes los declara Almacén después, en el formulario.
 * Sin OF: la recolección deja el saldo en WIP sin asignar (D-55) y la devolución regresa ese saldo.
 */
@Component({
  selector: 'pc-recoleccion-nueva',
  imports: [HojaNueva, OdooLineCapture, OdooTabs, PcPestana, OdooIcon],
  template: `
    <pc-hoja-nueva lista="Recolecciones" ruta="/recolecciones" [titulo]="tipo() === 'recoleccion' ? 'Recolección' : 'Devolución de recolección'" [stages]="stages" [error]="error()" [conChatter]="true" (guardar)="guardar()" (descartar)="router.navigateByUrl('/recolecciones')">
      <div class="row g-4 mb-3">
        <div class="col-md-6">
          <div class="o_form_label_row">
            <span class="o_form_label">Tipo</span>
            <select class="form-select form-select-sm o_inline_input" name="tipo" (change)="tipo.set($any($event.target).value)">
              <option value="recoleccion" [selected]="tipo() === 'recoleccion'">Recolección (MP → WIP)</option>
              <option value="devolucion" [selected]="tipo() === 'devolucion'">Devolución (WIP → MP)</option>
            </select>
          </div>
          <div class="o_form_label_row">
            <span class="o_form_label">Planta</span>
            <select class="form-select form-select-sm o_inline_input" name="planta" (change)="planta.set($any($event.target).value)">
              @for (p of plantas; track p) {
                <option [value]="p" [selected]="p === planta()">{{ p }}</option>
              }
            </select>
          </div>
        </div>
        <div class="col-md-6">
          <div class="o_form_label_row"><span class="o_form_label">Orden</span><span class="o_form_value text-muted">— (saldo sin asignar)</span></div>
        </div>
      </div>
      <pc-odoo-tabs [pestanas]="pestanas">
        <ng-template pcPestana="operaciones">
      <pc-odoo-line-capture [(draft)]="draft" [catalogo]="inv.catalogo" [unidadFija]="true" (submitted)="agregar($event)" />
      <table class="table table-sm align-middle mb-0">
        <thead><tr><th>Clave</th><th>Producto</th><th class="text-end">Cantidad</th><th style="width:70px;">Unidad</th><th class="text-center" style="width:60px;"></th></tr></thead>
        <tbody>
          @if (lineas().length === 0) {
            <tr><td colspan="5" class="text-muted small fst-italic">Sin líneas todavía.</td></tr>
          }
          @for (l of lineas(); track $index) {
            <tr>
              <td class="small">{{ l.clave }}</td>
              <td>{{ l.producto }}</td>
              <td class="text-end">{{ n1(l.cantidad) }}</td>
              <td class="small">{{ l.unidad }}</td>
              <td class="text-center"><button type="button" class="btn o_btn_icon" aria-label="Quitar línea" title="Quitar línea" (click)="quitar($index)"><pc-odoo-icon nombre="quitar-linea" contexto="icono" /></button></td>
            </tr>
          }
        </tbody>
      </table>
        </ng-template>
      </pc-odoo-tabs>
    </pc-hoja-nueva>
  `,
  styles: ':host { display: contents; }',
})
export class RecoleccionNueva {
  private readonly libres = inject(OperacionesLibres);
  protected readonly inv = inject(InventoryState);
  protected readonly router = inject(Router);
  protected readonly n1 = n1;
  protected readonly stages = ETAPAS_RECOLECCION;
  protected readonly pestanas = [{ id: 'operaciones', titulo: 'Operaciones' }];
  protected readonly plantas = PLANTAS;
  protected readonly tipo = signal<Tipo>('recoleccion');
  protected readonly planta = signal<Planta>('PIM');
  protected readonly lineas = signal<LineaLibre[]>([]);
  protected readonly draft = signal<LineDraft>(emptyDraft());
  protected readonly error = signal<string | undefined>(undefined);

  protected agregar(d: LineDraft): void {
    const { linea, error } = crearLineaLibre(this.inv.getProducto(d.clave), d.cantidad);
    this.error.set(error);
    if (!linea) return;
    this.lineas.update(ls => [...ls, linea]);
    this.draft.set(emptyDraft());
  }

  protected quitar(i: number): void {
    this.lineas.update(ls => ls.filter((_, j) => j !== i));
  }

  protected guardar(): void {
    const { op, error } = this.tipo() === 'recoleccion'
      ? this.libres.crearRecoleccion(this.planta(), this.lineas())
      : this.libres.crearDevolucion(this.planta(), this.lineas());
    this.error.set(error);
    if (op) void this.router.navigateByUrl(`/recolecciones/${op.folio}`);
  }
}
