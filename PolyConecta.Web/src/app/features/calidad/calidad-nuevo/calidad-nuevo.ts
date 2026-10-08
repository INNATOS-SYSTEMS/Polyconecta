import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { n1 } from '../../../core/format/numero';
import { CalidadLibre, LoteControlado } from '../../../core/state/libre/calidad-libre';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { HojaNueva } from '../../../shared/hoja-nueva/hoja-nueva';
import { OdooTabs, PcPestana } from '../../../shared/odoo-tabs/odoo-tabs';
import { ETAPAS_CALIDAD } from '../calidad-acciones';

/**
 * "Nuevo" control de calidad (FR-012) con la estructura completa del documento (D-136, spec 011 P4):
 * solo sobre lotes que ya existen y están en revisión, elegidos en la pestaña Controles; el chatter se
 * activa al guardar. Aprobar o fallar se hace después, en el formulario.
 */
@Component({
  selector: 'pc-calidad-nuevo',
  imports: [HojaNueva, OdooTabs, PcPestana],
  template: `
    <pc-hoja-nueva lista="Control de Calidad" ruta="/calidad" titulo="Control de calidad" [stages]="stages" [error]="error()" [conChatter]="true" (guardar)="guardar()" (descartar)="router.navigateByUrl('/calidad')">
      <pc-odoo-tabs [pestanas]="pestanas">
        <ng-template pcPestana="controles">
      <h6 class="text-uppercase text-muted small fw-bold mb-2">Lotes en revisión</h6>
      @if (elegibles().length === 0) {
        <div class="text-muted small fst-italic">No hay lotes en revisión. Un control libre solo se crea sobre lotes existentes.</div>
      } @else {
        <table class="table table-sm align-middle mb-0">
          <thead><tr><th style="width:36px;"></th><th>Lote</th><th>Orden de fabricación</th><th class="text-end">Cantidad</th><th>Unidad</th></tr></thead>
          <tbody>
            @for (e of elegibles(); track clave(e)) {
              <tr>
                <td><input type="checkbox" class="form-check-input" [attr.aria-label]="e.ofFolio + ' ' + e.lote.lote" [checked]="elegidos().has(clave(e))" (change)="alternar(clave(e))" /></td>
                <td><code>{{ e.lote.lote }}</code></td>
                <td class="small">{{ e.ofFolio }}</td>
                <td class="text-end">{{ n1(e.lote.real) }}</td>
                <td class="small">{{ e.lote.unidad }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
        </ng-template>
      </pc-odoo-tabs>
    </pc-hoja-nueva>
  `,
  styles: ':host { display: contents; }',
})
export class CalidadNuevo {
  private readonly qc = inject(CalidadLibre);
  private readonly flow = inject(OperationalFlowState);
  protected readonly router = inject(Router);
  protected readonly n1 = n1;
  protected readonly stages = ETAPAS_CALIDAD;
  protected readonly pestanas = [{ id: 'controles', titulo: 'Controles' }];
  protected readonly elegibles = signal(this.qc.elegibles());
  protected readonly elegidos = signal(new Set<string>());
  protected readonly error = signal<string | undefined>(undefined);

  protected clave(e: LoteControlado): string {
    return `${e.ofFolio}|${e.lote.lote}`;
  }

  protected alternar(lote: string): void {
    this.elegidos.update(s => {
      const n = new Set(s);
      if (!n.delete(lote)) n.add(lote);
      return n;
    });
  }

  protected guardar(): void {
    const elegidos = [...this.elegidos()].map(k => {
      const [ofFolio, lote] = k.split('|');
      return { ofFolio, lote };
    });
    const { control, error } = this.qc.crear(elegidos);
    this.error.set(error);
    if (control) void this.router.navigateByUrl(`/calidad/${control.folio}`);
    this.flow.notificar();
  }
}
