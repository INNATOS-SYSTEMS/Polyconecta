import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';

interface AppTile {
  label: string;
  icon: string;
  /** Variante B, tonal (D-144): fondo claro del color del módulo, ícono y borde en sus tonos. */
  fondo: string;
  icono: string;
  borde: string;
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
                <span class="o_app_icon" [style.background]="app.fondo" [style.color]="app.icono" [style.border-color]="app.borde"><pc-odoo-icon [nombre]="app.icon" contexto="aplicacion" /></span>
                <span class="o_app_label">{{ app.label }}</span>
            </button>
        }
    </div>
</div>
`,
  // Estilos del hub aquí y no en app.css: el Inicio carga aparte y no pesa en la carga inicial.
  styles: `
    :host { display: contents; }
    .o_app_hub {
        padding: 3rem 2rem;
        display: flex;
        justify-content: center;
    }
    .o_app_grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, 130px);
        gap: 2rem;
        max-width: 820px;
    }
    .o_app_tile {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 0.5rem;
        background: none;
        border: none;
        cursor: pointer;
        padding: 0.5rem;
        border-radius: 8px;
    }
    .o_app_tile:hover {
        background: rgba(0,0,0,0.03);
    }
    .o_app_icon {
        width: 88px;
        height: 88px;
        border-radius: 18px;
        display: flex;
        align-items: center;
        justify-content: center;
        border: 1px solid transparent;
        box-sizing: border-box;
        box-shadow: 0 1px 2px rgba(17, 24, 39, 0.06);
    }
    .o_app_label {
        font-size: 0.82rem;
        font-weight: 600;
        color: var(--text-dark, #2b2b33);
        text-align: center;
    }
  `,
})
export class Dashboard {
  protected readonly router = inject(Router);

  protected readonly apps: AppTile[] = [
    { label: 'Ventas', icon: 'pedido', fondo: '#E3F0F1', icono: '#17676C', borde: '#C9E2E4', route: '/pedidos' },
    { label: 'Fabricación', icon: 'fabricacion', fondo: '#E8EAF6', icono: '#2E3889', borde: '#D3D7EE', route: '/fabricacion' },
    { label: 'Calidad', icon: 'calidad', fondo: '#E4F1EA', icono: '#276B47', borde: '#CBE4D6', route: '/calidad' },
    { label: 'Inventario', icon: 'inventario', fondo: '#F4ECE1', icono: '#80552A', borde: '#E6D6C0', route: '/inventario' },
  ];
}
