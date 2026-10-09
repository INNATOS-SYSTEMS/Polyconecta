import { Component, ViewEncapsulation } from '@angular/core';

/**
 * Maestro del formulario en dos columnas (contratos visuales §1.6). Cada renglón es un `o_form_label_row`
 * con su etiqueta y su valor o su campo. En captura el renglón mide lo mismo que en solo lectura: el campo
 * no agrega altura (D-159). Los estilos van aquí y no en app.css, fuera de la carga inicial.
 */
@Component({
  selector: 'pc-odoo-maestro',
  template: `
    <div class="row g-4 mb-4">
      <div class="col-md-6"><ng-content select="[izquierda]" /></div>
      <div class="col-md-6"><ng-content select="[derecha]" /></div>
    </div>
  `,
  encapsulation: ViewEncapsulation.None,
  styles: `
    pc-odoo-maestro { display: block; }
    pc-odoo-maestro .o_form_label_row > pc-odoo-many2one,
    pc-odoo-maestro .o_form_label_row > pc-odoo-date,
    pc-odoo-maestro .o_form_label_row > pc-odoo-number,
    pc-odoo-maestro .o_form_label_row > .o_form_campo { display: block; flex: 1 1 auto; min-width: 0; }
    pc-odoo-maestro .o_form_label_row .o_inline_input { padding-top: 0; padding-bottom: 0; min-height: 0; height: 22px; line-height: 21px; }
    pc-odoo-maestro .o_form_label_row .o_field { min-height: 0; height: 22px; }
    pc-odoo-maestro .o_form_label_row .o_field input,
    pc-odoo-maestro .o_form_label_row .o_field .o_field_valor { padding-top: 0; padding-bottom: 0; height: 21px; line-height: 21px; }
    pc-odoo-maestro .o_form_nota { display: block; font-size: 0.8rem; color: var(--text-muted); margin-top: 0.15rem; }
  `,
})
export class OdooMaestro {}
