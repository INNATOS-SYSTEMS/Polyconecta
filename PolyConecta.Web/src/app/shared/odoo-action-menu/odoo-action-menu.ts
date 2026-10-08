import { Component, input } from '@angular/core';
import { CdkMenu, CdkMenuItem, CdkMenuTrigger } from '@angular/cdk/menu';
import { OdooIcon } from '../odoo-icon/odoo-icon';

export interface AccionMenu {
  nombre: string;
  icono?: string;
  deshabilitada?: boolean;
  ejecutar(): void;
}

/**
 * Engranaje de acciones del documento (spec 011, contratos visuales §1.3): solo el ícono, junto a donde
 * terminan las migas, como en Odoo. Abre el menú de acciones secundarias (duplicar, imprimir,
 * archivar…) con `CdkMenu`: flechas, Enter y Esc, y cierra al elegir.
 */
@Component({
  selector: 'pc-odoo-action-menu',
  imports: [CdkMenuTrigger, CdkMenu, CdkMenuItem, OdooIcon],
  template: `
    <button type="button" class="btn o_btn_icon" [cdkMenuTriggerFor]="menu" [attr.aria-label]="titulo()" [title]="titulo()" data-acciones>
      <pc-odoo-icon nombre="acciones" contexto="icono" />
    </button>
    <ng-template #menu>
      <div class="o_dropdown_panel" cdkMenu>
        @for (a of acciones(); track a.nombre) {
          <button type="button" class="o_search_menu_item" cdkMenuItem [cdkMenuItemDisabled]="!!a.deshabilitada" (cdkMenuItemTriggered)="a.ejecutar()" [attr.data-accion]="a.nombre">
            @if (a.icono) { <pc-odoo-icon [nombre]="a.icono" /> }{{ a.nombre }}
          </button>
        }
      </div>
    </ng-template>
  `,
  styles: ':host { display: inline-block; }',
})
export class OdooActionMenu {
  readonly titulo = input('Acciones');
  readonly acciones = input.required<AccionMenu[]>();
}
