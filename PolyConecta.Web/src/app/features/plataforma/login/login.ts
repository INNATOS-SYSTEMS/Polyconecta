import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { SesionState } from '../../../core/sesion/sesion-state';

/**
 * Pantalla de inicio de sesión de PolyConecta (/login).
 * Permite autenticarse mediante usuario y contraseña, redirigiendo a la ruta indicada en ?volver=.
 */
@Component({
  selector: 'pc-login',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="o_login_wrap d-flex align-items-center justify-content-center min-vh-100 bg-light p-3">
      <div class="card shadow-sm border-0 o_login_card w-100" style="max-width: 400px; border-radius: 10px;">
        <div class="card-body p-4">
          <div class="text-center mb-4">
            <h1 class="h4 fw-bold text-primary mb-1">PolyConecta</h1>
            <p class="text-muted small">Inicie sesión para acceder a la plataforma</p>
          </div>

          @if (error()) {
            <div class="alert alert-danger py-2 px-3 small mb-3" data-login-error role="alert">
              {{ error() }}
            </div>
          }

          <form (ngSubmit)="enviar()">
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
                placeholder="Nombre de usuario" />
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
                placeholder="••••••••" />
            </div>

            <button
              type="submit"
              class="btn btn-primary w-100 py-2 fw-medium"
              [disabled]="cargando()"
              data-login-submit>
              @if (cargando()) {
                Iniciando sesión...
              } @else {
                Iniciar sesión
              }
            </button>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: `
    .o_login_wrap {
      background-color: #f8fafc;
    }
    .o_login_card {
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
    }
  `,
})
export class Login implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly sesion = inject(SesionState);

  protected usuario = '';
  protected contrasena = '';
  protected readonly cargando = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    if (this.sesion.conSesion()) {
      this.redireccionar();
    }
  }

  protected enviar(): void {
    if (!this.usuario.trim() || !this.contrasena) {
      this.error.set('Ingrese usuario y contraseña.');
      return;
    }

    this.cargando.set(true);
    this.error.set(null);

    this.sesion.iniciarSesion(this.usuario.trim(), this.contrasena).subscribe({
      next: () => {
        this.redireccionar();
      },
      error: () => {
        this.cargando.set(false);
        this.error.set('Usuario o contraseña incorrectos.');
      },
    });
  }

  private redireccionar(): void {
    const volver = this.route.snapshot.queryParamMap.get('volver') || '/';
    this.router.navigateByUrl(volver);
  }
}
