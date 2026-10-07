import { Component, inject, input, output } from '@angular/core';
import { DIALOG_DATA, Dialog, DialogConfig, DialogRef } from '@angular/cdk/dialog';
import { ComponentType } from '@angular/cdk/portal';
import { OdooIcon } from '../odoo-icon/odoo-icon';

/**
 * Marco de los diálogos (spec 011): título, cuerpo y botones primario y secundario, sobre el `Dialog`
 * del CDK, que atrapa el foco y cierra con Esc o clic fuera. Lo usan las confirmaciones, el hard-stop
 * y los diálogos de las transiciones del kanban.
 */
@Component({
  selector: 'pc-odoo-dialog',
  imports: [OdooIcon],
  template: `
    <div class="o_dialog" role="dialog" aria-modal="true" [attr.aria-label]="titulo()">
      <div class="o_dialog_header">
        <span>{{ titulo() }}</span>
        <button type="button" class="btn o_btn_icon" aria-label="Cerrar" (click)="cancelar.emit()"><pc-odoo-icon nombre="cerrar" /></button>
      </div>
      <div class="o_dialog_body"><ng-content /></div>
      <div class="o_dialog_footer">
        @if (textoPrimario()) {
          <button type="button" class="btn btn-primary" [disabled]="primarioDeshabilitado()" (click)="confirmar.emit()" data-dialogo="confirmar">{{ textoPrimario() }}</button>
        }
        <button type="button" class="btn btn-outline-secondary" (click)="cancelar.emit()" data-dialogo="cancelar">{{ textoSecundario() }}</button>
      </div>
    </div>
  `,
})
export class OdooDialog {
  readonly titulo = input.required<string>();
  readonly textoPrimario = input<string | null>('Confirmar');
  readonly textoSecundario = input('Cancelar');
  readonly primarioDeshabilitado = input(false);
  readonly confirmar = output<void>();
  readonly cancelar = output<void>();
}

/** Abre un componente de diálogo con la configuración de PolyConecta (fondo y foco del CDK). */
export function abrirDialogo<R, C = unknown>(dialog: Dialog, componente: ComponentType<C>, datos?: unknown, config: DialogConfig<unknown, DialogRef<R, C>> = {}): DialogRef<R, C> {
  return dialog.open<R, unknown, C>(componente, { backdropClass: 'o_dialog_backdrop', data: datos, ...config } as DialogConfig<unknown, DialogRef<R, C>>);
}

/** Diálogo de confirmación genérico: devuelve `true` si se confirma. */
@Component({
  selector: 'pc-odoo-confirmacion',
  imports: [OdooDialog],
  template: `
    <pc-odoo-dialog [titulo]="datos.titulo" [textoPrimario]="datos.confirmar ?? 'Confirmar'" (confirmar)="ref.close(true)" (cancelar)="ref.close(false)">
      <p class="mb-0">{{ datos.mensaje }}</p>
    </pc-odoo-dialog>
  `,
})
export class OdooConfirmacion {
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  protected readonly datos = inject<{ titulo: string; mensaje: string; confirmar?: string }>(DIALOG_DATA);
}

