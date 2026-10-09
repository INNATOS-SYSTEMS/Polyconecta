import { Injectable, inject } from '@angular/core';
import { Dialog } from '@angular/cdk/dialog';
import { firstValueFrom } from 'rxjs';
import { DialogoLogin } from './dialogo-login';

/**
 * Servicio para coordinar la apertura del diálogo de inicio de sesión ante un 401.
 * Si ocurren múltiples peticiones 401 concurrentes, comparte una única instancia del diálogo.
 * Usa una promesa y no `shareReplay`: un operador que use una ruta perezosa entra a la carga inicial.
 */
@Injectable({ providedIn: 'root' })
export class DialogoLoginService {
  private readonly dialog = inject(Dialog);
  private activo: Promise<boolean> | null = null;

  abrir(): Promise<boolean> {
    if (this.activo) {
      return this.activo;
    }

    const ref = this.dialog.open<boolean>(DialogoLogin, {
      backdropClass: 'o_dialog_backdrop',
      disableClose: true,
    });

    this.activo = firstValueFrom(ref.closed, { defaultValue: undefined })
      .then(res => res === true)
      .finally(() => {
        this.activo = null;
      });

    return this.activo;
  }
}
