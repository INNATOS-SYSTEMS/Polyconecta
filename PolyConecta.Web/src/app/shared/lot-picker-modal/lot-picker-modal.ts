import { Component, computed, input, model, output, signal } from '@angular/core';
import { CdkTrapFocus } from '@angular/cdk/a11y';
import { n1 } from '../../core/format/numero';
import { OdooDialog } from '../odoo-dialog/odoo-dialog';
import { OdooIcon } from '../odoo-icon/odoo-icon';
import { ProductionLot } from '../../core/models/produccion';

/**
 * Réplica de Components/Forms/LotPickerModal.razor: lotes completos (producto terminado). Va en el marco de
 * `pc-odoo-dialog` (spec 011, P3): atrapa el foco y cierra con Esc o clic fuera.
 */
@Component({
  selector: 'pc-lot-picker-modal',
  imports: [OdooDialog, OdooIcon, CdkTrapFocus],
  templateUrl: './lot-picker-modal.html',
  styles: ':host { display: contents; }',
})
export class LotPickerModal {
  readonly show = input(false);
  readonly availableLots = input<readonly ProductionLot[]>([]);
  readonly selectedFolios = model<string[]>([]);
  readonly requiredQty = input(0);
  readonly unidad = input('');
  /** Lotes que existen pero Calidad no ha liberado: al capturarlos se explica el hard-stop en vez de "no encontrado". */
  readonly noLiberados = input<readonly ProductionLot[]>([]);
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
      const bloqueado = this.noLiberados().find(l => l.lote.toLowerCase() === lower);
      this.errorMsg.set(bloqueado
        ? `Hard-stop de Calidad: el lote ${bloqueado.lote} está ${bloqueado.estado.toLowerCase()}; no se puede mover hasta que Calidad lo libere.`
        : `Lote ${code} no encontrado o no aprobado.`);
    } else {
      this.selectedFolios.update(f => [...f, code]);
    }
    this.scanValue.set('');
  }

  protected remove(folio: string): void {
    this.selectedFolios.update(f => f.filter(x => x !== folio));
  }
}
