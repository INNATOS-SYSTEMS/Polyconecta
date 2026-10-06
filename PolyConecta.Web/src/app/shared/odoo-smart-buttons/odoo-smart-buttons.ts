import { Component, input, output } from '@angular/core';

export interface SmartButtonModel {
  label: string;
  countBadge: number;
  iconClass: string;
  targetRoute: string;
}

/** Réplica de Components/Forms/OdooSmartButtons.razor. */
@Component({
  selector: 'pc-odoo-smart-buttons',
  template: `
    <div class="d-flex gap-1 flex-wrap">
      @for (btn of buttons(); track btn.label) {
        <div class="o_smart_button" (click)="smartNavigate.emit(btn.targetRoute)">
          <div class="d-flex align-items-center gap-1">
            <i [class]="btn.iconClass"></i>
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
