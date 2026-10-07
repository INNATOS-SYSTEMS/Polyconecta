import { Component, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { n1 } from '../../../core/format/numero';
import { LotBalance } from '../../../core/models/inventario';
import { InventoryState } from '../../../core/state/inventory-state';
import { DocumentoLogistica } from '../../../core/models/logistica';
import { OperacionesLibres, PLANTAS, Planta } from '../../../core/state/libre/operaciones-libres';
import { HojaNueva } from '../../../shared/hoja-nueva/hoja-nueva';
import { CONFIG, TipoLogistica } from './tipos';

/**
 * "Nuevo" traslado, recepción o entrega (FR-012). Solo ofrece los lotes que permite su regla:
 * liberados por Calidad (traslado y entrega) o en tránsito (recepción, D-56). Cada lote muestra su
 * cantidad y la unidad base del producto (D-127).
 */
@Component({
  selector: 'pc-logistica-nuevo',
  imports: [HojaNueva],
  template: `
    <pc-hoja-nueva [lista]="cfg().tituloLista" [ruta]="cfg().ruta" [titulo]="cfg().tituloForm" [stages]="cfg().stages" [error]="error()" (guardar)="guardar()" (descartar)="router.navigateByUrl(cfg().ruta)">
      <div class="row g-4 mb-3">
        <div class="col-md-6">
          <div class="o_form_label_row">
            <span class="o_form_label">{{ tipo() === 'recepcion' ? 'Planta destino' : 'Planta origen' }}</span>
            <select class="form-select form-select-sm o_inline_input" name="planta" (change)="cambiarPlanta($any($event.target).value)">
              @for (p of plantas; track p) {
                <option [value]="p" [selected]="p === planta()">{{ p }}</option>
              }
            </select>
          </div>
          @if (tipo() === 'entrega') {
            <div class="o_form_label_row"><span class="o_form_label">Cliente</span><input class="form-control form-control-sm o_inline_input" name="cliente" [value]="cliente()" (input)="cliente.set($any($event.target).value)" /></div>
          }
        </div>
        <div class="col-md-6">
          <div class="o_form_label_row"><span class="o_form_label">Origen del documento</span><span class="o_form_value text-muted">— (libre)</span></div>
          <div class="o_form_label_row"><span class="o_form_label">Regla</span><span class="o_form_value small">{{ regla() }}</span></div>
        </div>
      </div>
      <ul class="nav nav-tabs mb-3" role="tablist">
        <li class="nav-item"><button class="nav-link active">Lotes</button></li>
      </ul>
      @if (lotes().length === 0) {
        <div class="text-muted small fst-italic">No hay lotes que cumplan la regla en {{ planta() }}.</div>
      } @else {
        <table class="table table-sm align-middle mb-0">
          <thead class="text-muted small"><tr><th style="width:36px;"></th><th>Lote</th><th>Producto</th><th>Ubicación</th><th class="text-end">Cantidad</th><th style="width:70px;">Unidad</th></tr></thead>
          <tbody>
            @for (l of lotes(); track l.lote + l.ubicacion) {
              <tr>
                <td><input type="checkbox" class="form-check-input" [attr.aria-label]="l.lote" [checked]="elegidos().has(l.lote)" (change)="alternar(l.lote)" /></td>
                <td><code>{{ l.lote }}</code></td>
                <td class="small">{{ inv.getProducto(l.clave)?.nombre ?? l.clave }}</td>
                <td class="small">{{ l.ubicacion }}</td>
                <td class="text-end">{{ n1(l.cantidad) }}</td>
                <td class="small">{{ inv.getProducto(l.clave)?.unidad }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </pc-hoja-nueva>
  `,
  styles: ':host { display: contents; }',
})
export class LogisticaNuevo {
  private readonly libres = inject(OperacionesLibres);
  protected readonly inv = inject(InventoryState);
  protected readonly router = inject(Router);
  readonly tipo = input.required<TipoLogistica>();
  protected readonly cfg = computed(() => CONFIG[this.tipo()]);
  protected readonly n1 = n1;
  protected readonly plantas = PLANTAS;
  protected readonly planta = signal<Planta>('PIM');
  protected readonly cliente = signal('');
  protected readonly elegidos = signal(new Set<string>());
  protected readonly error = signal<string | undefined>(undefined);

  protected readonly regla = computed(() =>
    this.tipo() === 'recepcion' ? 'Solo lotes en tránsito (TRANS/*)' : 'Solo lotes liberados por Calidad',
  );

  protected readonly lotes = computed<LotBalance[]>(() => {
    this.inv.cambios();
    return this.tipo() === 'recepcion' ? this.libres.lotesEnTransito(this.planta()) : this.libres.lotesLiberados(this.planta());
  });

  protected cambiarPlanta(p: Planta): void {
    this.planta.set(p);
    this.elegidos.set(new Set());
  }

  protected alternar(lote: string): void {
    this.elegidos.update(s => {
      const n = new Set(s);
      if (!n.delete(lote)) n.add(lote);
      return n;
    });
  }

  protected guardar(): void {
    const lotes = [...this.elegidos()];
    let r: { doc?: DocumentoLogistica; error?: string };
    if (this.tipo() === 'traslado') r = this.libres.crearTraslado(this.planta(), lotes);
    else if (this.tipo() === 'recepcion') r = this.libres.crearRecepcion(this.planta(), lotes);
    else r = this.libres.crearEntrega(this.planta(), this.cliente(), lotes);
    this.error.set(r.error);
    if (r.doc) void this.router.navigateByUrl(`${this.cfg().ruta}/${r.doc.folio}`);
  }
}
