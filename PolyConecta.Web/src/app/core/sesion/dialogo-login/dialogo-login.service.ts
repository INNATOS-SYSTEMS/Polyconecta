import { Injectable, inject } from '@angular/core';
import { Dialog } from '@angular/cdk/dialog';
import { Observable, finalize, map, shareReplay } from 'rxjs';
import { DialogoLogin } from './dialogo-login';

/**
 * Servicio para coordinar la apertura del diálogo de inicio de sesión ante un 401.
 * Si ocurren múltiples peticiones 401 concurrentes, comparte una única instancia del diálogo.
 */
@Injectable({ providedIn: 'root' })
export class DialogoLoginService {
  private readonly dialog = inject(Dialog);
  private activo$: Observable<boolean> | null = null;

  abrir(): Observable<boolean> {
    if (this.activo$) {
      return this.activo$;
    }

    const ref = this.dialog.open<boolean>(DialogoLogin, {
      backdropClass: 'o_dialog_backdrop',
      disableClose: true,
    });

    this.activo$ = ref.closed.pipe(
      map(res => res === true),
      shareReplay({ bufferSize: 1, refCount: false }),
      finalize(() => {
        this.activo$ = null;
      })
    );

    return this.activo$;
  }
}
