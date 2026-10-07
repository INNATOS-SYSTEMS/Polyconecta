import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AvisosService } from '../odoo-dialog/avisos';
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
    <!-- Los avisos (y sus íconos) se descargan la primera vez que hay uno: no pesan en la carga inicial. -->
    @defer (when avisos.avisos().length > 0) { <pc-odoo-avisos /> }
  `,
  styles: ':host { display: contents; }',
})
export class MainLayout {
  protected readonly avisos = inject(AvisosService);
}
