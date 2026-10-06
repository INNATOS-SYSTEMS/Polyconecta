import { Component, input, output, signal } from '@angular/core';
import { n1 } from '../../core/format/numero';
import { LotBalance } from '../../core/models/inventario';
import { LotAllocation, StockOperationLine, declarado, pendiente } from '../../core/models/operaciones';

/**
 * Réplica de Components/Forms/LotQuantityPickerModal.razor: hermano de LotPickerModal para
 * operaciones donde el lote se fracciona (la materia prima sale por cantidad, no por lote completo).
 */
@Component({
  selector: 'pc-lot-quantity-picker-modal',
  templateUrl: './lot-quantity-picker-modal.html',
  styles: ':host { display: contents; }',
})
export class LotQuantityPickerModal {
  readonly show = input(false);
  readonly line = input<StockOperationLine | null>(null);
  readonly availableLots = input<readonly LotBalance[]>([]);
  readonly origen = input('');
  readonly added = output<{ lote: string; cantidad: number }>();
  readonly removed = output<LotAllocation>();
  readonly closed = output<void>();

  protected readonly scanValue = signal('');
  protected readonly scanQty = signal(0);
  protected readonly errorMsg = signal<string | null>(null);
  protected readonly n1 = n1;
  protected readonly declarado = declarado;
  protected readonly pendiente = pendiente;

  /** Propuesta: lo que falta o lo que tiene el lote, lo que sea menor. Siempre editable. */
  protected proponer(lote: LotBalance): void {
    const linea = this.line()!;
    this.scanValue.set(lote.lote);
    this.scanQty.set(Math.min(pendiente(linea) - declarado(linea), lote.cantidad));
    this.errorMsg.set(null);
  }

  protected agregar(): void {
    this.errorMsg.set(null);
    const code = this.scanValue().trim();
    if (code === '') return;

    const lote = this.availableLots().find(l => l.lote.toLowerCase() === code.toLowerCase());
    if (!lote) {
      this.errorMsg.set(`Lote ${code} no encontrado o no disponible en ${this.origen()}.`);
      return;
    }
    const cantidad = this.scanQty();
    if (cantidad <= 0) {
      this.errorMsg.set('Capture la cantidad que sale del lote.');
      return;
    }
    if (cantidad > lote.cantidad) {
      this.errorMsg.set(`El lote ${code} solo tiene ${n1(lote.cantidad)}.`);
      return;
    }

    this.added.emit({ lote: lote.lote, cantidad });
    this.scanValue.set('');
    this.scanQty.set(0);
  }

  protected onQty(event: Event): void {
    const valor = Number.parseFloat((event.target as HTMLInputElement).value);
    this.scanQty.set(Number.isFinite(valor) ? valor : 0);
  }
}
