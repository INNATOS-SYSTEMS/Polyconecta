import { Component, computed, effect, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

export interface Crumb {
  label: string;
  url?: string;
}

/**
 * Réplica de Components/Views/OdooBreadcrumb.razor: muestra el nivel actual y uno hacia atrás;
 * lo anterior se resume en "…", que despliega la ruta completa al pulsarlo.
 */
@Component({
  selector: 'pc-odoo-breadcrumb',
  imports: [RouterLink],
  template: `
    <nav class="d-flex align-items-center gap-2 o_breadcrumb">
      @if (hayOcultos() && !expandido()) {
        <button type="button" class="parent-item o_breadcrumb_more" [title]="tituloOcultos()" (click)="expandido.set(true)">…</button>
        <i class="bi bi-chevron-right text-muted small"></i>
      }
      @for (item of visibles(); track $index; let isLast = $last) {
        @if (!isLast && item.url) {
          <a [routerLink]="item.url" class="parent-item">{{ item.label }}</a>
          <i class="bi bi-chevron-right text-muted small"></i>
        } @else if (!isLast) {
          <span class="parent-item text-muted">{{ item.label }}</span>
          <i class="bi bi-chevron-right text-muted small"></i>
        } @else {
          <span class="current-item">{{ item.label }}</span>
        }
      }
    </nav>
  `,
  styles: ':host { display: contents; }',
})
export class OdooBreadcrumb {
  readonly items = input<readonly Crumb[]>([]);
  /** Niveles visibles sin colapsar: el actual y uno hacia atrás. */
  readonly maxVisible = input(2);

  protected readonly expandido = signal(false);
  protected readonly hayOcultos = computed(() => this.items().length > this.maxVisible());
  protected readonly visibles = computed(() => {
    const items = this.items();
    const desde = this.hayOcultos() && !this.expandido() ? items.length - this.maxVisible() : 0;
    return items.slice(desde);
  });
  protected readonly tituloOcultos = computed(
    () =>
      'Mostrar ruta completa: ' +
      this.items()
        .slice(0, this.items().length - this.maxVisible())
        .map(i => i.label)
        .join(' › '),
  );

  constructor() {
    // Como OnParametersSet: al cambiar las migas, vuelve a colapsar.
    effect(() => {
      this.items();
      this.expandido.set(false);
    });
  }
}
