import { Component, computed, input, model, output, signal } from '@angular/core';
import { n1 } from '../../core/format/numero';
import { ProductionLot } from '../../core/models/produccion';

/** Réplica de Components/Forms/LotPickerModal.razor: lotes completos (producto terminado). */
@Component({
  selector: 'pc-lot-picker-modal',
  templateUrl: './lot-picker-modal.html',
  styles: ':host { display: contents; }',
})
export class LotPickerModal {
  readonly show = input(false);
  readonly availableLots = input<readonly ProductionLot[]>([]);
  readonly selectedFolios = model<string[]>([]);
  readonly requiredQty = input(0);
  readonly unidad = input('');
  readonly closed = output<void>();

  protected readonly scanValue = signal('');
  protected readonly errorMsg = signal<string | null>(null);
  protected readonly n1 = n1;

  protected readonly selectedWeight = computed(() =>
    this.selectedFolios().reduce((total, f) => total + (this.availableLots().find(l => l.lote === f)?.real ?? 0), 0),
  );

  protected lotDe(folio: string): ProductionLot | undefined {
    return this.availableLots().find(l => l.lote === folio);
  }

  protected onEnter(): void {
    const code = this.scanValue().trim();
    this.errorMsg.set(null);
    if (code === '') return;

    const lower = code.toLowerCase();
    if (this.selectedFolios().some(f => f.toLowerCase() === lower)) {
      this.errorMsg.set(`El lote ${code} ya fue capturado.`);
    } else if (!this.availableLots().some(l => l.lote.toLowerCase() === lower)) {
      this.errorMsg.set(`Lote ${code} no encontrado o no aprobado.`);
    } else {
      this.selectedFolios.update(f => [...f, code]);
    }
    this.scanValue.set('');
  }

  protected remove(folio: string): void {
    this.selectedFolios.update(f => f.filter(x => x !== folio));
  }
}
