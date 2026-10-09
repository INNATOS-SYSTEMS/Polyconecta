import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SesionAcciones } from '../../../core/sesion/sesion-acciones';
import { SesionState } from '../../../core/sesion/sesion-state';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';

/**
 * Inicio de sesión (/login, P-01 de la propuesta aprobada, D-163): sin componente propio, se arma con la
 * hoja y los campos del formulario de los contratos visuales. El error va como alerta arriba de los campos.
 */
@Component({
  selector: 'pc-login',
  imports: [OdooIcon],
  template: `
    <div class="d-flex align-items-center justify-content-center bg-light vh-100 p-3">
      <div class="o_form_sheet w-100" style="max-width: 400px">
        <form class="o_sheet_body" (submit)="$event.preventDefault(); iniciarSesion()">
          <h3 class="fw-bold mb-1">PolyConecta</h3>
          <div class="text-muted small mb-4">Inicia sesión con tu usuario</div>
          @if (error()) {
            <div class="alert alert-danger py-2 px-3 small mb-3" role="alert" data-login-error><pc-odoo-icon nombre="hard-stop" />{{ error() }}</div>
          }
          <div class="o_form_label_row">
            <label class="o_form_label" for="login-usuario">Usuario</label>
            <input id="login-usuario" class="form-control form-control-sm o_inline_input" autocomplete="username" data-login-usuario
                   [value]="usuario()" (input)="usuario.set($any($event.target).value)" [disabled]="cargando()" />
          </div>
          <div class="o_form_label_row">
            <label class="o_form_label" for="login-contrasena">Contraseña</label>
            <input id="login-contrasena" type="password" class="form-control form-control-sm o_inline_input" autocomplete="current-password" data-login-contrasena
                   [value]="contrasena()" (input)="contrasena.set($any($event.target).value)" [disabled]="cargando()" />
          </div>
          <button type="submit" class="btn btn-primary btn-sm fw-bold px-3 w-100 mt-4" data-login-submit
                  [disabled]="cargando() || !usuario().trim() || !contrasena()">
            @if (cargando()) { <pc-odoo-icon nombre="cargando" /> }Iniciar sesión
          </button>
        </form>
      </div>
    </div>
  `,
})
export class Login implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly sesion = inject(SesionState);
  private readonly acciones = inject(SesionAcciones);

  protected readonly usuario = signal('');
  protected readonly contrasena = signal('');
  protected readonly cargando = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    if (this.sesion.conSesion()) this.redireccionar();
  }

  protected iniciarSesion(): void {
    if (this.cargando() || !this.usuario().trim() || !this.contrasena()) return;
    this.cargando.set(true);
    this.error.set(null);
    this.acciones.iniciarSesion(this.usuario().trim(), this.contrasena()).subscribe({
      next: () => this.redireccionar(),
      error: () => {
        this.cargando.set(false);
        this.error.set('Usuario o contraseña incorrectos.');
      },
    });
  }

  private redireccionar(): void {
    void this.router.navigateByUrl(this.route.snapshot.queryParamMap.get('volver') || '/');
  }
}
