import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';

interface AppTile {
  label: string;
  icon: string;
  color: string;
  route: string;
}

/** Réplica de Pages/Dashboard.razor, con los íconos del catálogo (spec 011, P8). */
@Component({
  selector: 'pc-dashboard',
  imports: [OdooIcon],
  template: `
<div class="o_app_hub">
    <div class="o_app_grid">
        @for (app of apps; track app.label) {
            <button class="o_app_tile" (click)="router.navigateByUrl(app.route)">
                <span class="o_app_icon" [style.background]="app.color"><pc-odoo-icon [nombre]="app.icon" contexto="aplicacion" /></span>
                <span class="o_app_label">{{ app.label }}</span>
            </button>
        }
    </div>
</div>
`,
  styles: ':host { display: contents; }',
})
export class Dashboard {
  protected readonly router = inject(Router);

  protected readonly apps: AppTile[] = [
    { label: 'Ventas', icon: 'pedido', color: '#017E84', route: '/pedidos' },
    { label: 'Fabricación', icon: 'fabricacion', color: '#2E3889', route: '/fabricacion' },
    { label: 'Calidad', icon: 'calidad', color: '#2C7A4B', route: '/calidad' },
    { label: 'Inventario', icon: 'inventario', color: '#8A5A2B', route: '/inventario' },
  ];
}
