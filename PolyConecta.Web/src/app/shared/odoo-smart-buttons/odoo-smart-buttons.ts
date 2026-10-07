import { Component, input, output } from '@angular/core';
import { OdooIcon } from '../odoo-icon/odoo-icon';

export interface SmartButtonModel {
  label: string;
  countBadge: number;
  /** Nombre del catálogo de íconos (`entrega`) o, mientras dura la migración, la clase de Bootstrap (`bi bi-truck`). */
  iconClass: string;
  targetRoute: string;
  /** Documento libre sin ese origen (FR-014): se ve atenuado y no navega. */
  deshabilitado?: boolean;
}

/** Réplica de Components/Forms/OdooSmartButtons.razor. */
@Component({
  selector: 'pc-odoo-smart-buttons',
  imports: [OdooIcon],
  template: `
    <div class="d-flex gap-1 flex-wrap">
      @for (btn of buttons(); track btn.label) {
        <div class="o_smart_button" [class.opacity-50]="btn.deshabilitado" [attr.title]="btn.deshabilitado ? 'Documento libre: sin documento de origen' : null" (click)="btn.deshabilitado || smartNavigate.emit(btn.targetRoute)">
          <div class="d-flex align-items-center gap-1">
            <pc-odoo-icon [nombre]="btn.iconClass" contexto="inteligente" />
            <span class="stat-label">{{ btn.label }}</span>
          </div>
          <span class="stat-count">{{ btn.countBadge }}</span>
        </div>
      }
    </div>
  `,
  styles: ':host { display: contents; }',
})
export class OdooSmartButtons {
  readonly buttons = input<readonly SmartButtonModel[]>([]);
  readonly smartNavigate = output<string>();
}
