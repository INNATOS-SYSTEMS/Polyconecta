import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SesionState } from '../../../core/sesion/sesion-state';
import { OdooLoginForm } from '../../../shared/odoo-login-form/odoo-login-form';

/**
 * Pantalla de inicio de sesión de PolyConecta (/login) compuesta con pc-odoo-login-form.
 */
@Component({
  selector: 'pc-login',
  standalone: true,
  imports: [OdooLoginForm],
  template: `
    <div class="o_login_wrap d-flex align-items-center justify-content-center min-vh-100 bg-light p-3">
      <pc-odoo-login-form
        [cargando]="cargando()"
        [error]="error()"
        (enviar)="iniciarSesion($event)" />
    </div>
  `,
  styles: `
    .o_login_wrap {
      background-color: #f8fafc;
    }
  `,
})
export class Login implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly sesion = inject(SesionState);

  protected readonly cargando = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    if (this.sesion.conSesion()) {
      this.redireccionar();
    }
  }

  protected iniciarSesion(credenciales: { usuario: string; contrasena: string }): void {
    this.cargando.set(true);
    this.error.set(null);

    this.sesion.iniciarSesion(credenciales.usuario, credenciales.contrasena).subscribe({
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
