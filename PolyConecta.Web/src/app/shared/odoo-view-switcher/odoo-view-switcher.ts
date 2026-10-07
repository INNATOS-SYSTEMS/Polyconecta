import { Component, computed, inject, input } from '@angular/core';
import { UiViewState, ViewMode } from '../../core/state/ui-view-state';

/** Réplica de Components/Views/OdooViewSwitcher.razor. */
@Component({
  selector: 'pc-odoo-view-switcher',
  template: `
    <div class="btn-group btn-group-sm o_view_switcher">
      <button class="btn-view" [class.active]="isListActive()" title="Vista de lista" [disabled]="!kanbanEnabled()" (click)="setMode('List')">
        <i class="bi bi-list-ul"></i>
      </button>
      @if (kanbanEnabled()) {
        <button class="btn-view" [class.active]="viewState.viewMode() === 'Kanban'" title="Vista kanban" (click)="setMode('Kanban')">
          <i class="bi bi-kanban"></i>
        </button>
      }
    </div>
  `,
  styles: ':host { display: contents; }',
})
export class OdooViewSwitcher {
  protected readonly viewState = inject(UiViewState);

  /** Las vistas que ofrecen solo lista (captura masiva, incidencias) pasan false. */
  readonly kanbanEnabled = input(true);

  protected readonly isListActive = computed(() => !this.kanbanEnabled() || this.viewState.viewMode() === 'List');

  protected setMode(mode: ViewMode): void {
    this.viewState.setViewMode(mode);
  }
}
