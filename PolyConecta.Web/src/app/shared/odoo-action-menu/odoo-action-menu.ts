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
 * Botón "⚙ Acciones" con su menú (spec 011): `CdkMenu` del CDK, que se maneja con teclado (flechas,
 * Enter y Esc) y cierra al elegir. Lleva las acciones secundarias de un documento.
 */
@Component({
  selector: 'pc-odoo-action-menu',
  imports: [CdkMenuTrigger, CdkMenu, CdkMenuItem, OdooIcon],
  template: `
    <button type="button" class="btn btn-outline-secondary" [cdkMenuTriggerFor]="menu" [attr.aria-label]="titulo()" data-acciones>
      <pc-odoo-icon nombre="acciones" />{{ titulo() }}
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
