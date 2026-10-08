import { Component, computed, input, output, signal } from '@angular/core';
import { OdooIcon } from '../odoo-icon/odoo-icon';

/**
 * Réplica de Components/Views/OdooPager.razor. Con `inicio` y `fin` pagina de verdad (spec 011):
 * el rango "inicio-fin / total" abre el menú de filas por página y las flechas cambian de página.
 * Sin ellos se comporta como el del prototipo: "1-N / N" y flechas deshabilitadas.
 */
@Component({
  selector: 'pc-odoo-pager',
  imports: [OdooIcon],
  template: `
    <div class="o_pager position-relative">
      @if (pagina()) {
        <button type="button" class="btn btn-sm btn-link p-0 text-reset text-decoration-none" data-pager="rango" (click)="menu.set(!menu())" aria-label="Filas por página">{{ rango() }}</button> / {{ count() }}
        @if (menu()) {
          <div class="o_dropdown_panel position-absolute end-0" style="top:100%;z-index:20;" role="menu">
            @for (n of tamanos(); track n) {
              <button type="button" class="o_search_menu_item" role="menuitem" [class.active]="n === tamano()" [attr.data-tamano]="n" (click)="elegir(n)">{{ n }} por página</button>
            }
          </div>
        }
      } @else {
        <span>{{ count() === 0 ? '0-0' : '1-' + count() }} / {{ count() }}</span>
      }
      <button type="button" class="btn btn-sm btn-light" [disabled]="!pagina() || inicio()! <= 1" (click)="anterior.emit()" aria-label="Página anterior" data-pager="anterior"><pc-odoo-icon nombre="anterior" /></button>
      <button type="button" class="btn btn-sm btn-light" [disabled]="!pagina() || fin()! >= count()" (click)="siguiente.emit()" aria-label="Página siguiente" data-pager="siguiente"><pc-odoo-icon nombre="siguiente" /></button>
    </div>
  `,
  styles: ':host { display: contents; }',
})
export class OdooPager {
  readonly count = input(0);
  readonly inicio = input<number | null>(null);
  readonly fin = input<number | null>(null);
  readonly tamano = input(80);
  readonly tamanos = input<readonly number[]>([20, 40, 80, 200]);
  readonly anterior = output<void>();
  readonly siguiente = output<void>();
  readonly cambiarTamano = output<number>();
  protected readonly menu = signal(false);
  protected readonly pagina = computed(() => this.inicio() !== null && this.fin() !== null);
  protected readonly rango = computed(() => (this.count() === 0 ? '0-0' : `${this.inicio()}-${this.fin()}`));

  protected elegir(n: number): void {
    this.menu.set(false);
    this.cambiarTamano.emit(n);
  }
}
