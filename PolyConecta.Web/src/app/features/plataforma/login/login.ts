import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SesionAcciones } from '../../../core/sesion/sesion-acciones';
import { SesionState } from '../../../core/sesion/sesion-state';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';

/**
 * Inicio de sesión (/login, D-166): pantalla completa, sin barra superior, en dos secciones. A la
 * izquierda el logo de Polyempaques y la ilustración de la planta; a la derecha el formulario, con las etiquetas sobre
 * los campos, la contraseña con botón para mostrarla y el error como alerta arriba de los campos. En
 * pantallas angostas solo queda el formulario, con la marca arriba.
 */
@Component({
  selector: 'pc-login',
  imports: [OdooIcon],
  template: `
    <div class="o_login">
      <aside class="o_login_arte">
        <div class="o_login_marca">
          <img class="o_login_logo" src="img/polyempaques-logo-color.svg" alt="Polyempaques" />
        </div>
        <img class="o_login_ilustracion" src="img/manufactura.svg" alt="Línea de extrusión de película con su operadora y el tablero de la planta" />
        <p class="o_login_lema">Del pedido a la planta, con CONTPAQi siempre al día.</p>
      </aside>

      <section class="o_login_panel">
        <form class="o_login_form" (submit)="$event.preventDefault(); iniciarSesion()">
          <div class="o_login_marca o_login_marca_movil">
            <img class="o_login_logo" src="img/polyempaques-logo-color.svg" alt="Polyempaques" />
          </div>
          <h1>Bienvenido a PolyConecta</h1>
          <p class="o_login_subtitulo">Inicia sesión para continuar con la operación de la planta.</p>

          @if (error()) {
            <div class="alert alert-danger py-2 px-3 small mb-4 d-flex align-items-center gap-2" role="alert" data-login-error>
              <pc-odoo-icon nombre="hard-stop" />{{ error() }}
            </div>
          }

          <label class="o_login_label" for="login-usuario">Usuario</label>
          <input id="login-usuario" class="form-control o_login_input" autocomplete="username" data-login-usuario autofocus
                 [value]="usuario()" (input)="usuario.set($any($event.target).value)" [disabled]="cargando()" />

          <label class="o_login_label" for="login-contrasena">Contraseña</label>
          <div class="o_login_contrasena">
            <input id="login-contrasena" [type]="verContrasena() ? 'text' : 'password'" class="form-control o_login_input"
                   autocomplete="current-password" data-login-contrasena
                   [value]="contrasena()" (input)="contrasena.set($any($event.target).value)" [disabled]="cargando()" />
            <button type="button" class="o_login_ver" data-login-ver [attr.aria-label]="verContrasena() ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                    [attr.aria-pressed]="verContrasena()" (click)="verContrasena.set(!verContrasena())">
              <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                <path d="M2.06 12.35a1 1 0 0 1 0-.7 10.75 10.75 0 0 1 19.88 0 1 1 0 0 1 0 .7 10.75 10.75 0 0 1-19.88 0" />
                <circle cx="12" cy="12" r="3" />
                @if (verContrasena()) { <path d="m2 2 20 20" /> }
              </svg>
            </button>
          </div>

          <button type="submit" class="btn btn-primary o_login_entrar" data-login-submit
                  [disabled]="cargando() || !usuario().trim() || !contrasena()">
            @if (cargando()) { <pc-odoo-icon nombre="cargando" /> }Iniciar sesión
          </button>
          <p class="o_login_ayuda">¿Olvidaste tu contraseña? Pide a un administrador que la restablezca.</p>
        </form>
      </section>
    </div>
  `,
  styles: `
    :host { display: block; }
    .o_login { display: grid; grid-template-columns: 1fr 1fr; min-height: 100vh; background: var(--control-bg); }
    .o_login_arte {
      display: flex; flex-direction: column; padding: 2.5rem 3rem;
      background: linear-gradient(160deg, #F7F8FD 0%, #EEF0FA 100%); border-right: 1px solid var(--border-color);
    }
    .o_login_marca { display: flex; align-items: center; }
    /* Logo de Polyempaques con las letras en el azul de la marca (el original es blanco, para fondos oscuros). */
    .o_login_logo { height: 40px; width: auto; }
    .o_login_ilustracion { width: 100%; max-width: 600px; max-height: 64vh; margin: auto auto 1.75rem; object-fit: contain; }
    .o_login_lema { margin: 0 auto auto; max-width: 420px; text-align: center; color: var(--text-muted); font-size: 0.95rem; }
    .o_login_panel { display: flex; align-items: center; justify-content: center; padding: 2.5rem 1.5rem; }
    .o_login_form { width: 100%; max-width: 400px; }
    .o_login_marca_movil { display: none; margin-bottom: 2.5rem; }
    h1 { font-size: 1.85rem; font-weight: 700; color: var(--text-main); margin-bottom: 0.5rem; }
    .o_login_subtitulo { color: var(--text-muted); margin-bottom: 2.25rem; }
    .o_login_label { display: block; font-size: 0.875rem; font-weight: 500; color: var(--text-main); margin-bottom: 0.4rem; }
    .o_login_input { height: 46px; border-radius: 8px; border-color: var(--btn-secondary-border); padding: 0 0.9rem; margin-bottom: 1.25rem; font-size: 0.95rem; }
    .o_login_input:focus { border-color: var(--brand-primary); box-shadow: 0 0 0 3px rgba(46, 56, 137, 0.15); }
    .o_login_contrasena { position: relative; }
    .o_login_contrasena .o_login_input { padding-right: 3rem; }
    .o_login_ver {
      position: absolute; top: 0; right: 0; height: 46px; width: 46px; display: grid; place-items: center;
      border: 0; background: none; color: var(--text-muted); border-radius: 8px;
    }
    .o_login_ver:hover { color: var(--brand-primary); }
    .o_login_entrar { width: 100%; height: 46px; border-radius: 8px; font-weight: 600; font-size: 1rem; margin-top: 0.75rem; display: inline-flex; align-items: center; justify-content: center; gap: 0.5rem; }
    .o_login_ayuda { margin: 1.25rem 0 0; font-size: 0.85rem; color: var(--text-muted); text-align: center; }
    @media (max-width: 991.98px) {
      .o_login { grid-template-columns: 1fr; }
      .o_login_arte { display: none; }
      .o_login_marca_movil { display: flex; }
    }
  `,
})
export class Login implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly sesion = inject(SesionState);
  private readonly acciones = inject(SesionAcciones);

  protected readonly usuario = signal('');
  protected readonly contrasena = signal('');
  protected readonly verContrasena = signal(false);
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
