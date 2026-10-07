import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ProcessType } from '../../../core/models/produccion';
import { InventoryState } from '../../../core/state/inventory-state';
import { OfLibre } from '../../../core/state/libre/of-libre';
import { HojaNueva } from '../../../shared/hoja-nueva/hoja-nueva';

/** "Nueva" orden de fabricación sin pedido (FR-012): proceso, producto, cantidad y la unidad del producto. */
@Component({
  selector: 'pc-fabricacion-nueva',
  imports: [HojaNueva],
  template: `
    <pc-hoja-nueva lista="Órdenes de Fabricación" ruta="/fabricacion" titulo="Orden de fabricación" [stages]="stages" [error]="error()" (guardar)="guardar()" (descartar)="router.navigateByUrl('/fabricacion')">
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
          <option [value]="p.clave">{{ p.nombre }}</option>
        }
      </datalist>
    </pc-hoja-nueva>
  `,
  styles: ':host { display: contents; }',
})
export class FabricacionNueva {
  private readonly ofs = inject(OfLibre);
  protected readonly inv = inject(InventoryState);
  protected readonly router = inject(Router);
  protected readonly stages = ['Borrador', 'Planeado', 'En progreso', 'Hecho'];
  protected readonly procesos = OfLibre.Procesos;
  protected readonly proceso = signal<ProcessType>('Extrusion');
  protected readonly clave = signal('');
  protected readonly cantidad = signal(0);
  protected readonly producto = computed(() => this.inv.getProducto(this.clave()));
  protected readonly error = signal<string | undefined>(undefined);

  protected guardar(): void {
    const { of, error } = this.ofs.crear(this.proceso(), this.clave(), this.cantidad());
    this.error.set(error);
    if (of) void this.router.navigateByUrl(`/fabricacion/${of.folio}`);
  }
}
