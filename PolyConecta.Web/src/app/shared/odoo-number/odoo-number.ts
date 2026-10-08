import { Component, computed, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export type TipoNumero = 'cantidad' | 'moneda' | 'porcentaje';

/** Texto con formato es-MX: separador de miles y decimales fijos (research R-05). */
export function formatearNumero(v: number | null, tipo: TipoNumero, decimales: number): string {
  if (v == null || Number.isNaN(v)) return '';
  const n = new Intl.NumberFormat('es-MX', { minimumFractionDigits: decimales, maximumFractionDigits: decimales }).format(v);
  return tipo === 'porcentaje' ? `${n} %` : n;
}

/** Interpreta lo que se escribe: acepta separadores de miles y el signo de pesos. Vacío = null. */
export function interpretarNumero(texto: string): number | null {
  const limpio = texto.replace(/[$\s%]/g, '').replace(/,/g, '');
  if (limpio === '') return null;
  const n = Number(limpio);
  return Number.isNaN(n) ? Number.NaN : n;
}

/**
 * Número, moneda o cantidad con unidad (spec 011, research R-05). Muestra el valor con formato; al
 * enfocarlo lo deja editable sin separadores. La unidad es la base del producto y no se edita (D-127).
 */
@Component({
  selector: 'pc-odoo-number',
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => OdooNumber), multi: true }],
  template: `
    <div class="o_field" [class.o_field_solo_lectura]="soloLectura()" [class.o_field_invalido]="invalido()" [attr.data-numero]="nombre()">
      @if (tipo() === 'moneda') { <span class="o_field_extra">$</span> }
      <input type="text" inputmode="decimal" class="text-end" [attr.aria-label]="nombre()" [attr.aria-invalid]="invalido()"
             [readOnly]="soloLectura()" [disabled]="deshabilitado()" [value]="enEdicion() ? crudo() : mostrado()"
             (focus)="enfocar()" (input)="crudo.set($any($event.target).value)" (blur)="confirmar()" />
      @if (tipo() === 'moneda') { <span class="o_field_extra">{{ moneda() }}</span> }
      @if (tipo() === 'cantidad' && unidad()) { <span class="o_field_extra" data-numero-unidad title="Unidad base del producto en CONTPAQi">{{ unidad() }}</span> }
    </div>
    @if (invalido()) { <div class="text-danger small mt-1" role="alert">{{ error() }}</div> }
  `,
})
export class OdooNumber implements ControlValueAccessor {
  readonly nombre = input('Cantidad');
  readonly tipo = input<TipoNumero>('cantidad');
  readonly decimales = input(2);
  readonly unidad = input<string | null>(null);
  readonly moneda = input('MXN');
  readonly min = input<number | null>(null);
  readonly soloLectura = input(false);

  readonly valor = signal<number | null>(null);
  readonly deshabilitado = signal(false);
  protected readonly enEdicion = signal(false);
  protected readonly crudo = signal('');
  protected readonly error = signal('');
  protected readonly invalido = computed(() => this.error() !== '');
  protected readonly mostrado = computed(() => formatearNumero(this.valor(), this.tipo(), this.decimales()));
  private alCambiar: (v: number | null) => void = () => {};
  private alTocar: () => void = () => {};

  protected enfocar(): void {
    if (this.soloLectura()) return;
    this.enEdicion.set(true);
    this.crudo.set(this.valor() == null ? '' : String(this.valor()));
  }

  protected confirmar(): void {
    if (!this.enEdicion()) return;
    this.enEdicion.set(false);
    const n = interpretarNumero(this.crudo());
    if (n !== null && Number.isNaN(n)) {
      this.error.set('Escriba un número.');
      return;
    }
    if (n !== null && this.min() !== null && n < this.min()!) {
      this.error.set(`El valor mínimo es ${formatearNumero(this.min(), this.tipo(), this.decimales())}.`);
      return;
    }
    this.error.set('');
    this.valor.set(n);
    this.alCambiar(n);
    this.alTocar();
  }

  writeValue(v: number | null): void { this.valor.set(v ?? null); this.error.set(''); }
  registerOnChange(fn: (v: number | null) => void): void { this.alCambiar = fn; }
  registerOnTouched(fn: () => void): void { this.alTocar = fn; }
  setDisabledState(d: boolean): void { this.deshabilitado.set(d); }
}
