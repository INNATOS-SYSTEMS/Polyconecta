import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

/**
 * Contrato visual del formulario de inicio de sesión de PolyConecta (D-148, CT-24).
 * Presenta tarjeta estilizada con logo/título, entradas para usuario y contraseña,
 * estado de carga, mensaje de error y acción primaria accesible por teclado.
 */
@Component({
  selector: 'pc-odoo-login-form',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="o_login_form_card card shadow-sm border-0 w-100" style="max-width: 400px; border-radius: 10px;">
      <div class="card-body p-4">
        <div class="text-center mb-4">
          <h1 class="h4 fw-bold text-primary mb-1">{{ titulo() }}</h1>
          @if (subtitulo()) {
            <p class="text-muted small mb-0">{{ subtitulo() }}</p>
          }
        </div>

        @if (error()) {
          <div class="alert alert-danger py-2 px-3 small mb-3" data-login-error role="alert">
            {{ error() }}
          </div>
        }

        <form (ngSubmit)="alEnviar()">
          <div class="mb-3">
            <label for="login-usuario" class="form-label small fw-medium text-secondary">Usuario</label>
            <input
              id="login-usuario"
              type="text"
              class="form-control"
              [(ngModel)]="usuario"
              name="usuario"
              data-login-usuario
              required
              autocomplete="username"
              placeholder="Nombre de usuario"
              [disabled]="cargando()"
              [attr.disabled]="cargando() ? '' : null" />
          </div>

          <div class="mb-4">
            <label for="login-contrasena" class="form-label small fw-medium text-secondary">Contraseña</label>
            <input
              id="login-contrasena"
              type="password"
              class="form-control"
              [(ngModel)]="contrasena"
              name="contrasena"
              data-login-contrasena
              required
              autocomplete="current-password"
              placeholder="••••••••"
              [disabled]="cargando()"
              [attr.disabled]="cargando() ? '' : null" />
          </div>

          <button
            type="submit"
            class="btn btn-primary w-100 py-2 fw-medium"
            [disabled]="cargando() || !usuario.trim() || !contrasena"
            data-login-submit>
            @if (cargando()) {
              <span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
              Iniciando sesión...
            } @else {
              {{ textoBoton() }}
            }
          </button>
        </form>
      </div>
    </div>
  `,
  styles: `
    .o_login_form_card {
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
      background: white;
    }
  `,
})
export class OdooLoginForm {
  readonly titulo = input('PolyConecta');
  readonly subtitulo = input('Inicie sesión para acceder a la plataforma');
  readonly cargando = input(false);
  readonly error = input<string | null>(null);
  readonly textoBoton = input('Iniciar sesión');

  readonly enviar = output<{ usuario: string; contrasena: string }>();

  protected usuario = '';
  protected contrasena = '';

  protected alEnviar(): void {
    if (this.usuario.trim() && this.contrasena && !this.cargando()) {
      this.enviar.emit({
        usuario: this.usuario.trim(),
        contrasena: this.contrasena,
      });
    }
  }
}
