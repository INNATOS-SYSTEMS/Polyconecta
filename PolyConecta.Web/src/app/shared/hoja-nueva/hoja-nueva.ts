import { Component, input, output } from '@angular/core';
import { OdooBreadcrumb } from '../odoo-breadcrumb/odoo-breadcrumb';
import { OdooChatterDrawer } from '../odoo-chatter-drawer/odoo-chatter-drawer';
import { OdooIcon } from '../odoo-icon/odoo-icon';
import { OdooStatusPipeline } from '../odoo-status-pipeline/odoo-status-pipeline';

/**
 * Hoja de un documento nuevo en modo libre (D-59): la misma estructura que el formulario del
 * documento (panel de control, barra de acciones, etapas y hoja), en su estado inicial y sin origen.
 * "Guardar" crea el documento; "Descartar" vuelve a la lista sin crear nada. Con `conChatter` lleva el
 * chatter a la derecha, inactivo hasta guardar (D-136).
 */
@Component({
  selector: 'pc-hoja-nueva',
  imports: [OdooBreadcrumb, OdooStatusPipeline, OdooChatterDrawer, OdooIcon],
  template: `
    <div class="o_control_panel">
      <div class="d-flex align-items-center gap-3">
        <pc-odoo-breadcrumb [items]="[{ label: lista(), url: ruta() }, { label: 'Nuevo' }]" />
      </div>
    </div>
    <div class="p-4">
      <div class="o_statusbar">
        <div class="d-flex align-items-center gap-2">
          <button class="btn btn-primary btn-sm fw-bold px-3" (click)="guardar.emit()">Guardar</button>
          <button class="btn btn-outline-secondary btn-sm px-3" (click)="descartar.emit()">Descartar</button>
        </div>
        <span class="badge bg-secondary-subtle text-secondary-emphasis px-3 py-2" title="Creado con Nuevo: sin documento de origen">Libre</span>
      </div>
      <div class="d-flex gap-3 align-items-start">
      <div class="o_form_sheet flex-grow-1">
        <div class="o_sheet_body">
          <pc-odoo-status-pipeline [stages]="stages()" [currentStage]="stages()[0]" />
          <h3 class="fw-bold mb-1">{{ titulo() }}</h3>
          <h4 class="fw-bold text-muted mb-3">Nuevo</h4>
          @if (error()) {
            <div class="alert alert-danger py-2 px-3 small mb-3"><pc-odoo-icon nombre="hard-stop" />{{ error() }}</div>
          }
          <ng-content />
        </div>
      </div>
      @if (conChatter()) {
        <pc-odoo-chatter-drawer [inactivo]="true" />
      }
      </div>
    </div>
  `,
  styles: ':host { display: contents; }',
})
export class HojaNueva {
  readonly lista = input.required<string>();
  readonly ruta = input.required<string>();
  readonly titulo = input.required<string>();
  readonly stages = input.required<readonly string[]>();
  readonly error = input<string | undefined>(undefined);
  /** Estructura completa de D-136: el chatter se ve desde el inicio y se activa al guardar. */
  readonly conChatter = input(false);
  readonly guardar = output<void>();
  readonly descartar = output<void>();
}
