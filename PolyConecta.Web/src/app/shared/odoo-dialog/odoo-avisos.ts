import { Component, inject } from '@angular/core';
import { OdooIcon } from '../odoo-icon/odoo-icon';
import { AvisosService } from './avisos';

const ICONO = { exito: 'confirmar', aviso: 'advertencia', error: 'aviso' } as const;

/** Pinta los avisos flotantes de `AvisosService`, arriba a la derecha. Va una vez en el layout. */
@Component({
  selector: 'pc-odoo-avisos',
  imports: [OdooIcon],
  template: `
    <div class="o_avisos" role="status" aria-live="polite">
      @for (a of avisos.avisos(); track a.id) {
        <div class="o_aviso" [class]="'o_aviso o_aviso_' + a.tipo" [attr.data-aviso]="a.tipo">
          <pc-odoo-icon [nombre]="icono[a.tipo]" />
          <span class="flex-grow-1">{{ a.texto }}</span>
          <button type="button" class="btn o_btn_icon" style="width:24px;height:24px" aria-label="Cerrar aviso" (click)="avisos.cerrar(a.id)">
            <pc-odoo-icon nombre="cerrar" />
          </button>
        </div>
      }
    </div>
  `,
})
export class OdooAvisos {
  protected readonly avisos = inject(AvisosService);
  protected readonly icono = ICONO;
}
