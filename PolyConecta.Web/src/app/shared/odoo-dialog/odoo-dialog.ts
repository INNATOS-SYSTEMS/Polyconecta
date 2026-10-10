import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, input, output } from '@angular/core';
import { CdkTrapFocus } from '@angular/cdk/a11y';
import { DIALOG_DATA, Dialog, DialogConfig, DialogRef } from '@angular/cdk/dialog';
import { ComponentType } from '@angular/cdk/portal';
import { OdooIcon } from '../odoo-icon/odoo-icon';

/**
 * Marco de los diálogos (spec 011): título, cuerpo y botones primario y secundario, sobre el `Dialog`
 * del CDK, que atrapa el foco y cierra con Esc o clic fuera. Lo usan las confirmaciones, el hard-stop
 * y los diálogos de las transiciones del kanban. Si una pantalla lo pinta en su plantilla (con un `@if`)
 * en lugar de abrirlo con el CDK, el marco pone su propia capa con el mismo comportamiento: encima de la
 * página, centrado, con el fondo oscurecido, el foco atrapado, y Esc o clic fuera cancelan.
 */
@Component({
  selector: 'pc-odoo-dialog',
  imports: [NgTemplateOutlet, CdkTrapFocus, OdooIcon],
  template: `
    @if (conCapa()) {
      <div class="o_dialog_overlay o_dialog_backdrop" (click)="cancelar.emit()" (keydown.escape)="cancelar.emit()" data-dialogo-capa>
        <div cdkTrapFocus [cdkTrapFocusAutoCapture]="true" (click)="$event.stopPropagation()">
          <ng-container *ngTemplateOutlet="marco" />
        </div>
      </div>
    } @else {
      <ng-container *ngTemplateOutlet="marco" />
    }
    <ng-template #marco>
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
    </ng-template>
  `,
})
export class OdooDialog {
  private readonly abiertoPorCdk = !!inject(DialogRef, { optional: true });

  readonly titulo = input.required<string>();
  readonly textoPrimario = input<string | null>('Confirmar');
  readonly textoSecundario = input('Cancelar');
  readonly primarioDeshabilitado = input(false);
  /** Capa propia. Por omisión, solo si no lo abrió el `Dialog` del CDK; `false` si quien lo usa ya pone la suya. */
  readonly capa = input<boolean | null>(null);
  readonly confirmar = output<void>();
  readonly cancelar = output<void>();

  protected readonly conCapa = computed(() => this.capa() ?? !this.abiertoPorCdk);
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


/**
 * Hard-stop (contratos visuales §1.7): explica por qué no procede y qué hacer, sin ofrecer continuar.
 * Un solo botón, "Entendido".
 */
@Component({
  selector: 'pc-odoo-hard-stop',
  imports: [OdooDialog, OdooIcon],
  template: `
    <pc-odoo-dialog [titulo]="datos.titulo" [textoPrimario]="null" textoSecundario="Entendido" (cancelar)="ref.close()">
      <p class="mb-0 d-flex gap-2 align-items-start" data-hard-stop><pc-odoo-icon nombre="hard-stop" class="text-danger" />{{ datos.mensaje }}</p>
    </pc-odoo-dialog>
  `,
})
export class OdooHardStop {
  protected readonly ref = inject<DialogRef<void>>(DialogRef);
  protected readonly datos = inject<{ titulo: string; mensaje: string }>(DIALOG_DATA);
}
