import { Component, ElementRef, HostListener, inject, signal } from '@angular/core';
import { SesionAcciones } from '../../core/sesion/sesion-acciones';
import { SesionState } from '../../core/state/sesion-state';

/**
 * Parte derecha de la barra superior (contratos visuales §2.2): mensajes, el nombre del usuario (texto)
 * y el avatar cuadrado de su inicial, que es lo único interactivo y abre Preferencias y Cerrar sesión. Hasta F1 no hay inicio de sesión, así
 * que las dos opciones se ven deshabilitadas. La barra la carga con `@defer`, fuera de la carga inicial.
 */
@Component({
  selector: 'pc-odoo-systray',
  template: `
  <div class="d-flex align-items-center gap-2">
    <!-- Lucide messages-square en línea. -->
    <button type="button" class="o_systray_item" title="Mensajes y notificaciones" aria-label="Mensajes y notificaciones">
      <svg xmlns="http://www.w3.org/2000/svg" width="19" height="19" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M16 10a2 2 0 0 1-2 2H6.828a2 2 0 0 0-1.414.586l-2.202 2.202A.71.71 0 0 1 2 14.286V4a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z" /><path d="M20 9a2 2 0 0 1 2 2v10.286a.71.71 0 0 1-1.212.502l-2.202-2.202A2 2 0 0 0 17.172 19H10a2 2 0 0 1-2-2v-1" /></svg>
    </button>
    <!-- Solo el avatar es interactivo; el nombre es texto (contratos visuales §2.2). -->
    <span class="o_user_name">{{ sesion.usuario().nombre }}</span>
    <!-- Ancla propia: el contenedor de los menús del módulo (o_header_dropdown) vuelve transparente a sus botones. -->
    <div class="o_user_ancla">
      <!-- La letra va en ::before (data-inicial): no forma parte del texto de la barra. -->
      <button type="button" class="o_avatar" [class.active]="abierto()" [attr.data-inicial]="sesion.inicial()" aria-haspopup="menu" [attr.aria-expanded]="abierto()"
              [attr.aria-label]="'Usuario: ' + sesion.usuario().nombre" (click)="abierto.set(!abierto())" data-usuario></button>
      @if (abierto()) {
        <div class="o_header_dropdown_menu o_user_dropdown" role="menu">
          <button type="button" role="menuitem" [disabled]="!sesion.conSesion()" [title]="sesion.conSesion() ? 'Preferencias del usuario' : 'Disponible con el inicio de sesión (F1)'" data-usuario-opcion="preferencias">Preferencias</button>
          <div class="o_user_dropdown_sep"></div>
          <button type="button" role="menuitem" [disabled]="!sesion.conSesion()" [title]="sesion.conSesion() ? 'Cerrar la sesión actual' : 'Disponible con el inicio de sesión (F1)'" (click)="cerrarSesion()" data-usuario-opcion="salir">Cerrar sesión</button>
        </div>
      }
    </div>
  </div>
  `,
  // Estilos aquí y no en app.css: el componente carga diferido y no pesa en la carga inicial.
  styles: `
    :host { display: contents; }
    .o_systray_item { background: transparent; border: 0; color: white; opacity: 0.85; width: 34px; height: 34px; border-radius: 6px; display: inline-flex; align-items: center; justify-content: center; }
    .o_systray_item:hover { background: rgba(255, 255, 255, 0.12); opacity: 1; }
    .o_user_name { font-size: 0.82rem; font-weight: 400; opacity: 0.9; letter-spacing: 0.01em; -webkit-font-smoothing: antialiased; margin-left: 0.25rem; }
    .o_avatar { width: 28px; height: 28px; border-radius: 6px; border: 0; padding: 0; background: #E4E7F6; color: var(--brand-primary); display: inline-flex; align-items: center; justify-content: center; font-size: 0.85rem; font-weight: 600; cursor: pointer; transition: box-shadow 0.12s ease; }
    /* Hover y abierto: un anillo, sin cambiar el tamaño del cuadro. */
    .o_avatar:hover, .o_avatar.active { box-shadow: 0 0 0 2px rgba(255, 255, 255, 0.35); }
    .o_avatar:focus-visible { outline: none; box-shadow: 0 0 0 2px rgba(255, 255, 255, 0.8); }
    .o_avatar::before { content: attr(data-inicial); }
    .o_user_ancla { position: relative; display: inline-flex; align-items: center; }
    .o_user_dropdown { right: 0; left: auto; min-width: 190px; padding: 0.3rem 0; }
    .o_user_dropdown button { display: block; width: 100%; text-align: left; background: transparent; border: 0; padding: 0.45rem 1rem; font-size: 0.85rem; color: var(--text-main); }
    .o_user_dropdown button:hover:not(:disabled) { background: #F2F6F9; }
    .o_user_dropdown button:disabled { color: var(--text-muted); cursor: default; }
    .o_user_dropdown_sep { height: 1px; background: var(--border-color); margin: 0.3rem 0; }
  `,
})
export class OdooSystray {
  private readonly host = inject(ElementRef<HTMLElement>);
  protected readonly sesion = inject(SesionState);
  private readonly acciones = inject(SesionAcciones);

  constructor() {
    if (!this.sesion.conSesion()) this.acciones.cargarSesion().subscribe();
  }
  protected readonly abierto = signal(false);

  /** Un clic fuera o Esc cierran el menú del usuario. */
  @HostListener('document:click', ['$event'])
  protected clicFuera(e: MouseEvent): void {
    if (this.abierto() && !this.host.nativeElement.contains(e.target as Node)) this.abierto.set(false);
  }

  protected cerrarSesion(): void {
    if (this.sesion.conSesion()) {
      this.abierto.set(false);
      this.acciones.cerrarSesion().subscribe();
    }
  }

  @HostListener('document:keydown.escape')
  protected escape(): void {
    this.abierto.set(false);
  }
}
