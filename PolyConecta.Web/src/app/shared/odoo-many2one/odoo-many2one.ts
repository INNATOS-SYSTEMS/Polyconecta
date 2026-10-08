import { Component, effect, forwardRef, inject, input, signal, untracked } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Dialog } from '@angular/cdk/dialog';
import { firstValueFrom } from 'rxjs';
// BrnComboboxImports trae BrnCombobox y BrnComboboxMultiple con el mismo selector: se importan una por una (research R-04).
import {
  BrnCombobox, BrnComboboxAnchor, BrnComboboxContent, BrnComboboxEmpty, BrnComboboxInput, BrnComboboxItem, BrnComboboxList,
  BrnComboboxPopoverTrigger,
} from '@spartan-ng/brain/combobox';
import { BrnPopover, BrnPopoverContent, provideBrnPopoverConfig, provideBrnPopoverDefaultOptions } from '@spartan-ng/brain/popover';
import { consultaInicial, OrigenDeLista } from '../../core/lista/origen';
import { abrirDialogo } from '../odoo-dialog/odoo-dialog';
import { OdooBuscarMas } from './odoo-buscar-mas';

/**
 * Selección de registro, el many2one de Odoo (contratos visuales, docs/diseno/07): busca en el origen
 * mientras se escribe, muestra hasta `limite` opciones y "Buscar más…" abre la lista completa.
 */
@Component({
  selector: 'pc-odoo-many2one',
  imports: [BrnCombobox, BrnComboboxAnchor, BrnComboboxContent, BrnComboboxEmpty, BrnComboboxInput, BrnComboboxItem, BrnComboboxList,
    BrnComboboxPopoverTrigger, BrnPopover, BrnPopoverContent],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => OdooMany2one), multi: true },
    // El foco se queda en el campo: si el popover enfoca su primer botón ("Buscar más…"), se pierde lo que se escribe.
    provideBrnPopoverConfig({ align: 'start', sideOffset: 4 }),
    provideBrnPopoverDefaultOptions({ role: null, autoFocus: false }),
  ],
  template: `
    <div class="o_many2one" brnCombobox brnPopover [value]="valor()" (valueChange)="elegir($event)" [itemToString]="aTexto()"
         [filter]="sinFiltro" [disabled]="soloLectura() || deshabilitado()" (searchChange)="buscar($event)" [attr.data-many2one]="nombre()">
      <div brnComboboxAnchor class="o_field" [class.o_field_solo_lectura]="soloLectura()">
        <input brnComboboxInput brnComboboxPopoverTrigger [closeOnTriggerClick]="false"
               [placeholder]="placeholder()" [attr.aria-label]="nombre()" [attr.aria-required]="obligatorio()" />
      </div>
      <div *brnPopoverContent class="o_dropdown_panel" style="min-width:280px">
        <div brnComboboxContent>
          <div brnComboboxList class="o_many2one_lista" [attr.aria-label]="nombre()">
            @for (o of opciones(); track idDe()(o)) {
              <div brnComboboxItem [value]="o" class="o_many2one_opcion" [attr.data-opcion]="idDe()(o)">{{ aTexto()(o) }}</div>
            }
          </div>
          <div brnComboboxEmpty class="o_many2one_vacio">Sin resultados</div>
          @if (hayMas()) {
            <button type="button" class="o_many2one_mas btn btn-link w-100 text-start" data-many2one-mas (click)="buscarMas()">Buscar más…</button>
          }
        </div>
      </div>
    </div>
  `,
})
export class OdooMany2one<T> implements ControlValueAccessor {
  private readonly dialog = inject(Dialog);

  readonly origen = input.required<OrigenDeLista<T>>();
  readonly aTexto = input.required<(r: T) => string>();
  readonly idDe = input.required<(r: T) => string>();
  readonly nombre = input('Registro');
  readonly placeholder = input('');
  readonly limite = input(8);
  readonly soloLectura = input(false);
  readonly obligatorio = input(false);

  readonly valor = signal<T | null>(null);
  readonly opciones = signal<T[]>([]);
  readonly hayMas = signal(false);
  readonly deshabilitado = signal(false);
  private readonly texto = signal('');
  private alCambiar: (v: T | null) => void = () => {};
  private alTocar: () => void = () => {};
  /** El origen ya filtra: el combobox no vuelve a filtrar. */
  protected readonly sinFiltro = () => true;

  constructor() {
    effect(() => {
      const t = this.texto();
      untracked(() => void this.cargar(t));
    });
  }

  private async cargar(texto: string): Promise<void> {
    const r = await this.origen().consultar(consultaInicial({ tamano: this.limite(), busqueda: texto.trim() || null }));
    this.opciones.set(r.filas);
    this.hayMas.set(r.total > r.filas.length);
  }

  protected buscar(texto: string): void {
    // Al elegir, el combobox escribe el texto de la opción: no es una búsqueda nueva.
    const v = this.valor();
    this.texto.set(v && texto === this.aTexto()(v) ? '' : texto);
  }

  protected elegir(v: T | null | undefined): void {
    this.valor.set(v ?? null);
    this.alCambiar(v ?? null);
    this.alTocar();
  }

  async buscarMas(): Promise<void> {
    const ref = abrirDialogo<T>(this.dialog, OdooBuscarMas, { origen: this.origen(), aTexto: this.aTexto(), idDe: this.idDe(), titulo: `Buscar: ${this.nombre()}` });
    const elegido = await firstValueFrom(ref.closed);
    if (elegido) this.elegir(elegido);
  }

  writeValue(v: T | null): void { this.valor.set(v ?? null); }
  registerOnChange(fn: (v: T | null) => void): void { this.alCambiar = fn; }
  registerOnTouched(fn: () => void): void { this.alTocar = fn; }
  setDisabledState(d: boolean): void { this.deshabilitado.set(d); }
}
