import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DialogRef } from '@angular/cdk/dialog';
import { OdooDialog } from '../../../shared/odoo-dialog/odoo-dialog';
import { SesionState } from '../sesion-state';

/**
 * Diálogo que se abre cuando la sesión expira (401) y hay captura o cambios sin guardar.
 * Permite al usuario reanudar su sesión sin perder la información capturada (caso límite spec 003).
 */
@Component({
  selector: 'pc-dialogo-login',
  standalone: true,
  imports: [FormsModule, OdooDialog],
  template: `
    <pc-odoo-dialog
      titulo="Sesión expirada"
      textoPrimario="Reanudar sesión"
      textoSecundario="Cancelar"
      [primarioDeshabilitado]="cargando()"
      (confirmar)="enviar()"
      (cancelar)="cancelar()">
      <div class="mb-3 text-muted">
        Su sesión ha expirado pero hay cambios sin guardar. Inicie sesión para continuar sin perder su captura.
      </div>

      @if (error()) {
        <div class="alert alert-danger py-1 px-2 mb-2" data-login-error role="alert">
          {{ error() }}
        </div>
      }

      <form (ngSubmit)="enviar()">
        <div class="mb-3">
          <label for="dialogo-usuario" class="form-label mb-1">Usuario</label>
          <input
            id="dialogo-usuario"
            type="text"
            class="form-control"
            [(ngModel)]="usuario"
            name="usuario"
            data-login-usuario
            required
            autocomplete="username" />
        </div>
        <div class="mb-2">
          <label for="dialogo-contrasena" class="form-label mb-1">Contraseña</label>
          <input
            id="dialogo-contrasena"
            type="password"
            class="form-control"
            [(ngModel)]="contrasena"
            name="contrasena"
            data-login-contrasena
            required
            autocomplete="current-password" />
        </div>
        <button type="submit" class="d-none"></button>
      </form>
    </pc-odoo-dialog>
  `,
})
export class DialogoLogin {
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  private readonly sesion = inject(SesionState);

  protected usuario = '';
  protected contrasena = '';
  protected readonly cargando = signal(false);
  protected readonly error = signal<string | null>(null);

  protected enviar(): void {
    if (!this.usuario.trim() || !this.contrasena) {
      this.error.set('Ingrese usuario y contraseña.');
      return;
    }
    this.cargando.set(true);
    this.error.set(null);
    this.sesion.iniciarSesion(this.usuario.trim(), this.contrasena).subscribe({
      next: () => {
        this.ref.close(true);
      },
      error: () => {
        this.cargando.set(false);
        this.error.set('Usuario o contraseña incorrectos.');
      },
    });
  }

  protected cancelar(): void {
    this.ref.close(false);
  }
}
