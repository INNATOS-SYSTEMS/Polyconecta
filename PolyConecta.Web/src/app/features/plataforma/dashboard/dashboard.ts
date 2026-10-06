import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

interface AppTile {
  label: string;
  icon: string;
  color: string;
  route: string;
}

/** Réplica de Pages/Dashboard.razor. */
@Component({
  selector: 'pc-dashboard',
  template: `
<div class="o_app_hub">
    <div class="o_app_grid">
        @for (app of apps; track app.label) {
            <button class="o_app_tile" (click)="router.navigateByUrl(app.route)">
                <span class="o_app_icon" [style.background]="app.color"><i class="bi {{ app.icon }}"></i></span>
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
    { label: 'Ventas', icon: 'bi-cart-check-fill', color: '#017E84', route: '/pedidos' },
    { label: 'Fabricación', icon: 'bi-gear-wide-connected', color: '#2E3889', route: '/fabricacion' },
    { label: 'Calidad', icon: 'bi-shield-check', color: '#2C7A4B', route: '/calidad' },
    { label: 'Inventario', icon: 'bi-boxes', color: '#8A5A2B', route: '/inventario' },
  ];
}
