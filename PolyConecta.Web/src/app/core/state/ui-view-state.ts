import { Injectable, signal } from '@angular/core';

export type ViewMode = 'List' | 'Kanban';

/** Réplica de Services/UiViewState.cs, con signals en lugar del evento OnStateChanged. */
@Injectable({ providedIn: 'root' })
export class UiViewState {
  readonly documentType = signal('Pedido');
  readonly viewMode = signal<ViewMode>('List');
  readonly searchQuery = signal('');
  readonly preconfiguredStages = signal<readonly string[]>(['Pedido Sincronizado', 'Confirmado', 'En manufactura', 'Hecho']);

  setViewMode(mode: ViewMode): void {
    this.viewMode.set(mode);
  }

  setDocumentType(docType: string, stages: readonly string[]): void {
    this.documentType.set(docType);
    this.preconfiguredStages.set(stages);
  }

  setSearchQuery(query: string): void {
    this.searchQuery.set(query);
  }
}
