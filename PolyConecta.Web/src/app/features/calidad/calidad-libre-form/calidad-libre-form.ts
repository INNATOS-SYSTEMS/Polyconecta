import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { n1 } from '../../../core/format/numero';
import { CalidadLibre, LoteControlado } from '../../../core/state/libre/calidad-libre';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { ChatterEntry, OdooChatterDrawer } from '../../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { OdooTabs } from '../../../shared/odoo-tabs/odoo-tabs';
import { CalidadAcciones } from '../calidad-acciones';

/**
 * Control de calidad libre (FR-012): el mismo formulario que el control de una OF, sobre los lotes
 * elegidos al crearlo. No tiene smart buttons de origen (FR-014): cada lote enlaza a su OF.
 * Fallar un lote pide confirmación (aclaración P4), igual que en el control ligado.
 */
@Component({
  selector: 'pc-calidad-libre-form',
  imports: [BotonNuevo, OdooBreadcrumb, OdooChatterDrawer, OdooTabs, OdooIcon, RouterLink],
  template: `
    @if (control(); as control) {
      <div class="o_control_panel">
        <div class="d-flex align-items-center gap-3">
          <pc-boton-nuevo [ruta]="'/calidad'" />
          <pc-odoo-breadcrumb [items]="[{ label: 'Control de Calidad', url: '/calidad' }, { label: control.folio }]" />
        </div>
      </div>
      <div class="p-4">
        <div class="o_statusbar">
          <div class="d-flex align-items-center gap-2">
            <button class="btn btn-outline-success btn-sm px-3" (click)="aprobarSiguiente()" [disabled]="pendientes() === 0"><pc-odoo-icon nombre="confirmar" /> Aprueba</button>
            <button class="btn btn-outline-danger btn-sm px-3" (click)="fallarSiguiente()" [disabled]="pendientes() === 0"><pc-odoo-icon nombre="cancelar" /> Falla</button>
          </div>
          <span class="badge badge-brand px-3 py-2">{{ estado() }}</span>
        </div>
        <div class="d-flex gap-3 align-items-start">
          <div class="o_form_sheet flex-grow-1">
            <div class="o_sheet_body">
              <h3 class="fw-bold mb-1">Control de calidad</h3>
              <h4 class="fw-bold text-primary mb-3">{{ control.folio }}</h4>
              <div class="row g-4 mb-4">
                <div class="col-md-6">
                  <div class="o_form_label_row"><span class="o_form_label">Origen</span><span class="o_form_value text-muted">— (control libre)</span></div>
                  <div class="o_form_label_row"><span class="o_form_label">Lotes</span><span class="o_form_value">{{ control.lotes.length }}</span></div>
                </div>
                <div class="col-md-6">
                  <div class="o_form_label_row"><span class="o_form_label">Auditor</span><span class="o_form_value">Armando Silva</span></div>
                  <div class="o_form_label_row"><span class="o_form_label">Estado</span><span class="o_form_value">{{ estado() }}</span></div>
                </div>
              </div>
              <pc-odoo-tabs [pestanas]="pestanas" activa="controles" />
              <table class="table table-sm align-middle mb-0">
                <thead><tr><th>#</th><th>Orden de Fabricación</th><th>Lote</th><th class="text-end">Real</th><th>Aprueba</th></tr></thead>
                <tbody>
                  @for (l of control.lotes; track $index; let i = $index) {
                    <tr>
                      <td>{{ i + 1 }}</td>
                      <td class="small"><a [routerLink]="'/produccion/fabricacion/' + l.ofFolio" class="text-decoration-none">{{ l.ofFolio }}</a></td>
                      <td><code>{{ l.lote.lote }}</code></td>
                      <td class="text-end">{{ n1(l.lote.real) }} {{ l.lote.unidad }}</td>
                      <td>
                        @if (l.lote.estado === 'En revisión') {
                          <button class="btn btn-sm btn-outline-success me-1" (click)="qc.aprobar(l)">Aprueba</button>
                          <button class="btn btn-sm btn-outline-danger" (click)="fallar(l)">Falla</button>
                        } @else {
                          <span class="badge" [class.bg-success]="l.lote.estado === 'Aprobado'" [class.bg-danger]="l.lote.estado !== 'Aprobado'">{{ l.lote.estado }}</span>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
          <pc-odoo-chatter-drawer [messages]="chatter" [documentId]="control.folio" />
        </div>
      </div>
    } @else {
      <div class="p-4">Control de calidad no encontrado.</div>
    }
  `,
  styles: ':host { display: contents; }',
})
export class CalidadLibreForm {
  protected readonly qc = inject(CalidadLibre);
  private readonly flow = inject(OperationalFlowState);
  readonly folio = input('');
  private readonly acciones = inject(CalidadAcciones);
  protected readonly n1 = n1;
  protected readonly pestanas = [{ id: 'controles', titulo: 'Controles' }];
  protected readonly chatter: ChatterEntry[] = [{ author: 'Sistema', timestamp: 'hoy', text: 'Control de calidad creado con Nuevo, sin orden de origen.' }];

  /** Devuelve el mismo objeto mutado: sin equal:false no avisaría a sus dependientes. */
  protected readonly control = computed(() => {
    this.qc.cambios();
    this.flow.cambios();
    return this.qc.control(this.folio());
  }, { equal: () => false });
  protected readonly pendientes = computed(() => this.control()?.lotes.filter(l => l.lote.estado === 'En revisión').length ?? 0);
  protected readonly estado = computed(() => {
    const c = this.control();
    return c ? this.qc.estado(c) : '';
  });

  protected aprobarSiguiente(): void {
    const l = this.control()?.lotes.find(x => x.lote.estado === 'En revisión');
    if (l) this.qc.aprobar(l);
  }

  protected fallarSiguiente(): void {
    const l = this.control()?.lotes.find(x => x.lote.estado === 'En revisión');
    if (l) void this.fallar(l);
  }

  protected async fallar(l: LoteControlado): Promise<void> {
    if (await this.acciones.confirmarFalla(l.lote)) this.qc.rechazar(l);
  }
}
