import { Component, input } from '@angular/core';

/** Réplica de Components/Views/OdooPager.razor. */
@Component({
  selector: 'pc-odoo-pager',
  template: `
    <div class="o_pager">
      <span>{{ count() === 0 ? '0-0' : '1-' + count() }} / {{ count() }}</span>
      <button class="btn btn-sm btn-light" disabled><i class="bi bi-chevron-left"></i></button>
      <button class="btn btn-sm btn-light" disabled><i class="bi bi-chevron-right"></i></button>
    </div>
  `,
  styles: ':host { display: contents; }',
})
export class OdooPager {
  readonly count = input(0);
}
