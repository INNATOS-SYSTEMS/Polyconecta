import { Component, computed, input, model, output, signal } from '@angular/core';
import { ProductRef } from '../../core/models/inventario';

export interface LineDraft {
  clave: string;
  producto: string;
  cantidad: number;
  unidad: string;
  /** Solo en pedidos libres (D-74). */
  precioUnitario?: number;
  moneda?: string;
}

export const emptyDraft = (): LineDraft => ({ clave: '', producto: '', cantidad: 0, unidad: '' });

export const MONEDAS_CAPTURA = ['MXN', 'USD'];

const SEPARADOR = ' — ';
let siguienteId = 0;

/**
 * Réplica de Components/Forms/OdooLineCapture.razor: [Clave / Producto] [Cantidad] [Unidad] [Agregar].
 * Si el producto existe en el catálogo de CONTPAQi, la unidad se precarga sola.
 */
@Component({
  selector: 'pc-odoo-line-capture',
  templateUrl: './odoo-line-capture.html',
  styles: ':host { display: contents; }',
})
export class OdooLineCapture {
  readonly draft = model<LineDraft>(emptyDraft());
  /** Catálogo para autocompletar y precargar la unidad. Vacío = captura libre. */
  readonly catalogo = input<readonly ProductRef[]>([]);
  /** Unidad cuando el producto no está en el catálogo (por ejemplo, la unidad de la orden). */
  readonly unidadPorDefecto = input<string | undefined>(undefined);
  readonly placeholder = input('Clave / Producto');
  readonly enEdicion = input(false);
  /** Modo libre (D-127): solo productos del catálogo; la unidad es la del producto y no se edita. */
  readonly unidadFija = input(false);
  /** Pedido libre (D-74): agrega precio unitario y moneda. */
  readonly conPrecio = input(false);
  readonly submitted = output<LineDraft>();
  readonly cancelled = output<void>();

  protected readonly listId = `cat-${(++siguienteId).toString(16).padStart(8, '0')}`;
  protected readonly unidadPrecargada = signal(false);

  /** Confirmado contra el catálogo: el campo muestra clave y descripción. */
  protected readonly resuelto = computed(() => this.draft().producto.trim() !== '');
  protected readonly texto = computed(() =>
    this.resuelto() ? this.draft().clave + SEPARADOR + this.draft().producto : this.draft().clave,
  );
  protected readonly monedas = MONEDAS_CAPTURA;
  protected readonly valido = computed(() => {
    const d = this.draft();
    if (d.clave.trim() === '' || !(d.cantidad > 0)) return false;
    if (this.unidadFija() && !this.resuelto()) return false;
    return !this.conPrecio() || (d.precioUnitario ?? 0) > 0;
  });

  protected onClaveChanged(event: Event): void {
    let texto = ((event.target as HTMLInputElement).value ?? '').trim();
    // Si viene ya confirmado ("CLAVE — Descripción"), la clave es lo anterior al separador.
    const corte = texto.indexOf(SEPARADOR);
    if (corte > 0) texto = texto.slice(0, corte).trim();

    const lower = texto.toLowerCase();
    // Por clave exacta primero, luego por nombre.
    const prod =
      this.catalogo().find(p => p.clave.toLowerCase() === lower) ??
      this.catalogo().find(p => texto.length > 2 && p.nombre.toLowerCase().includes(lower));

    const actual = this.draft();
    if (prod) {
      this.draft.set({ ...actual, clave: prod.clave, producto: prod.nombre, unidad: prod.unidad });
      this.unidadPrecargada.set(true);
    } else {
      const porDefecto = this.unidadPorDefecto();
      const unidad = actual.unidad.trim() === '' && porDefecto ? porDefecto : actual.unidad;
      this.draft.set({ ...actual, clave: texto, producto: '', unidad });
      this.unidadPrecargada.set(false);
    }
  }

  protected onCantidadChanged(event: Event): void {
    const valor = Number.parseFloat((event.target as HTMLInputElement).value);
    this.draft.update(d => ({ ...d, cantidad: Number.isFinite(valor) ? valor : 0 }));
  }

  protected onPrecioChanged(event: Event): void {
    const valor = Number.parseFloat((event.target as HTMLInputElement).value);
    this.draft.update(d => ({ ...d, precioUnitario: Number.isFinite(valor) ? valor : 0 }));
  }

  protected onMonedaChanged(event: Event): void {
    this.draft.update(d => ({ ...d, moneda: (event.target as HTMLSelectElement).value }));
  }

  protected onUnidadChanged(event: Event): void {
    this.draft.update(d => ({ ...d, unidad: (event.target as HTMLInputElement).value ?? '' }));
    this.unidadPrecargada.set(false);
  }

  protected submit(): void {
    if (!this.valido()) return;
    const porDefecto = this.unidadPorDefecto();
    const d = this.draft();
    const unidad = d.unidad.trim() === '' && porDefecto ? porDefecto : d.unidad;
    this.submitted.emit({ ...d, unidad, ...(this.conPrecio() ? { moneda: d.moneda ?? MONEDAS_CAPTURA[0] } : {}) });
  }
}
