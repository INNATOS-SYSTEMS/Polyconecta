import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { OdooAvisos } from '../odoo-dialog/odoo-avisos';
import { OdooTopbar } from '../odoo-topbar/odoo-topbar';

/** Réplica de Components/Layout/MainLayout.razor. */
@Component({
  selector: 'pc-main-layout',
  imports: [RouterOutlet, OdooTopbar, OdooAvisos],
  template: `
    <div class="d-flex flex-column vh-100 overflow-hidden">
      <pc-odoo-topbar />

      <main class="flex-grow-1 overflow-y-auto bg-light">
        <router-outlet />
      </main>
    </div>
    <pc-odoo-avisos />
  `,
  styles: ':host { display: contents; }',
})
export class MainLayout {}
