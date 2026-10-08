import { NgTemplateOutlet } from '@angular/common';
import { etiquetaProducto } from '../../../core/format/producto-etiqueta';
import { Component, computed, inject, signal, WritableSignal } from '@angular/core';
import { Router } from '@angular/router';
import { n1 } from '../../../core/format/numero';
import { ProcessType } from '../../../core/models/produccion';
import { InventoryState } from '../../../core/state/inventory-state';
import { crearLineaLibre } from '../../../core/state/libre/linea-libre';
import { OfLibre } from '../../../core/state/libre/of-libre';
import { HojaNueva } from '../../../shared/hoja-nueva/hoja-nueva';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { emptyDraft, LineDraft, OdooLineCapture } from '../../../shared/odoo-line-capture/odoo-line-capture';
import { OdooTabs, PcPestana } from '../../../shared/odoo-tabs/odoo-tabs';
import { ETAPAS_OF } from '../fabricacion-acciones';

interface LineaNueva { clave: string; producto: string; cantidad: number; unidad: string }
const AL_GUARDAR = 'Se habilita al guardar la orden.';

/**
 * "Nueva" orden de fabricación sin pedido (FR-012) con la estructura completa del documento (D-136,
 * aclaración P2): Componentes y Subproductos se capturan antes de guardar y se guardan con el maestro;
 * Producción y Planeación se ven, deshabilitadas hasta guardar; el chatter se activa al guardar.
 */
@Component({
  selector: 'pc-fabricacion-nueva',
  imports: [HojaNueva, OdooTabs, PcPestana, OdooLineCapture, OdooIcon, NgTemplateOutlet],
  template: `
    <pc-hoja-nueva lista="Órdenes de Fabricación" ruta="/produccion/fabricacion" titulo="Orden de fabricación" [stages]="stages" [error]="error()" [conChatter]="true"
                   (guardar)="guardar()" (descartar)="router.navigateByUrl('/produccion/fabricacion')">
      <div class="row g-4 mb-2">
        <div class="col-md-6">
          <div class="o_form_label_row">
            <span class="o_form_label">Proceso</span>
            <select class="form-select form-select-sm o_inline_input" name="proceso" (change)="proceso.set($any($event.target).value)">
              @for (p of procesos; track p.tipo) {
                <option [value]="p.tipo" [selected]="p.tipo === proceso()">{{ p.etiqueta }}</option>
              }
            </select>
          </div>
          <div class="o_form_label_row">
            <span class="o_form_label">Producto</span>
            <input class="form-control form-control-sm o_inline_input" name="producto" list="of-nueva-catalogo" placeholder="Clave / Producto" (change)="clave.set($any($event.target).value)" />
          </div>
          <div class="o_form_label_row"><span class="o_form_label">Pedido</span><span class="o_form_value text-muted">— (sin pedido)</span></div>
        </div>
        <div class="col-md-6">
          <div class="o_form_label_row">
            <span class="o_form_label">Cantidad</span>
            <input type="number" step="0.1" class="form-control form-control-sm o_inline_input" name="cantidad" (change)="cantidad.set(+$any($event.target).value)" />
          </div>
          <div class="o_form_label_row">
            <span class="o_form_label">Unidad</span>
            <span class="o_form_value" title="Unidad base del producto en CONTPAQi: no se edita">{{ producto()?.unidad ?? '—' }}</span>
          </div>
        </div>
      </div>
      <datalist id="of-nueva-catalogo">
        @for (p of inv.catalogo; track p.clave) {
          <option [value]="p.clave">{{ etiqueta(p.clave, p.nombre) }}</option>
        }
      </datalist>

      <pc-odoo-tabs [pestanas]="pestanas">
        <ng-template pcPestana="componentes">
          <pc-odoo-line-capture [(draft)]="borradorComponente" [catalogo]="inv.catalogo" [unidadFija]="true" (submitted)="agregar(componentes, $event)" />
          <ng-container *ngTemplateOutlet="tabla; context: { $implicit: componentes, nombre: 'componentes' }" />
        </ng-template>
        <ng-template pcPestana="subproductos">
          <pc-odoo-line-capture [(draft)]="borradorSubproducto" [catalogo]="inv.catalogo" [unidadFija]="true" (submitted)="agregar(subproductos, $event)" />
          <ng-container *ngTemplateOutlet="tabla; context: { $implicit: subproductos, nombre: 'subproductos' }" />
        </ng-template>
      </pc-odoo-tabs>
      @if (errorLinea()) {
        <div class="alert alert-danger py-2 px-3 small mt-2 mb-0"><pc-odoo-icon nombre="hard-stop" />{{ errorLinea() }}</div>
      }

      <ng-template #tabla let-lineas let-nombre="nombre">
        <table class="table table-sm align-middle mb-0" [attr.data-lineas-nuevas]="nombre">
          <thead class="text-muted small"><tr><th>Producto</th><th class="text-center" style="width:110px;">Cantidad</th><th class="text-center" style="width:80px;">Unidad</th><th style="width:60px;"></th></tr></thead>
          <tbody>
            @for (l of lineas(); track $index) {
              <tr>
                <td>{{ etiqueta(l.clave, l.producto) }}</td>
                <td class="text-center">{{ n1(l.cantidad) }}</td>
                <td class="text-center small text-muted">{{ l.unidad }}</td>
                <td class="text-center"><button type="button" class="btn o_btn_icon" title="Eliminar" (click)="quitar(lineas, $index)"><pc-odoo-icon nombre="quitar-linea" contexto="icono" /></button></td>
              </tr>
            } @empty {
              <tr><td colspan="4" class="small text-muted">Sin líneas. Se pueden agregar ahora o después de guardar.</td></tr>
            }
          </tbody>
        </table>
      </ng-template>
    </pc-hoja-nueva>
  `,
  styles: ':host { display: contents; }',
})
export class FabricacionNueva {
  protected readonly etiqueta = (claveONombre?: string | null, nombre?: string) => etiquetaProducto(this.inv.catalogo, claveONombre, nombre);
  private readonly ofs = inject(OfLibre);
  protected readonly inv = inject(InventoryState);
  protected readonly router = inject(Router);
  protected readonly stages = ETAPAS_OF;
  protected readonly procesos = OfLibre.Procesos;
  protected readonly pestanas = [
    { id: 'componentes', titulo: 'Componentes' }, { id: 'subproductos', titulo: 'Subproductos' },
    { id: 'produccion', titulo: 'Producción', deshabilitada: AL_GUARDAR }, { id: 'planeacion', titulo: 'Planeación', deshabilitada: AL_GUARDAR },
  ];
  protected readonly proceso = signal<ProcessType>('Extrusion');
  protected readonly clave = signal('');
  protected readonly cantidad = signal(0);
  protected readonly producto = computed(() => this.inv.getProducto(this.clave()));
  protected readonly error = signal<string | undefined>(undefined);
  protected readonly errorLinea = signal<string | undefined>(undefined);
  protected readonly componentes = signal<LineaNueva[]>([]);
  protected readonly subproductos = signal<LineaNueva[]>([]);
  protected borradorComponente: LineDraft = emptyDraft();
  protected borradorSubproducto: LineDraft = emptyDraft();
  protected readonly n1 = n1;

  /** Valida la línea con las reglas del modo libre (producto del catálogo, cantidad, unidad base) y la agrega al borrador. */
  protected agregar(destino: WritableSignal<LineaNueva[]>, d: LineDraft): void {
    const { linea, error } = crearLineaLibre(this.inv.getProducto(d.clave), d.cantidad);
    this.errorLinea.set(error);
    if (!linea) return;
    destino.update(ls => [...ls, linea]);
    this.borradorComponente = emptyDraft();
    this.borradorSubproducto = emptyDraft();
  }

  protected quitar(destino: WritableSignal<LineaNueva[]>, i: number): void {
    destino.update(ls => ls.filter((_, j) => j !== i));
  }

  protected guardar(): void {
    const { of, error } = this.ofs.crearConLineas(this.proceso(), this.clave(), this.cantidad(), this.componentes(), this.subproductos());
    this.error.set(error);
    if (of) void this.router.navigateByUrl(`/produccion/fabricacion/${of.folio}`);
  }
}
