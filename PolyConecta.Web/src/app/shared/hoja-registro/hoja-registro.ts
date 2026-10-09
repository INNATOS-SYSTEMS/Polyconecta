import { Component, computed, inject, input, output } from '@angular/core';
import { Router } from '@angular/router';
import { BotonNuevo } from '../boton-nuevo/boton-nuevo';
import { AccionMenu, OdooActionMenu } from '../odoo-action-menu/odoo-action-menu';
import { OdooBreadcrumb } from '../odoo-breadcrumb/odoo-breadcrumb';
import { OdooChatterDrawer } from '../odoo-chatter-drawer/odoo-chatter-drawer';
import { OdooIcon } from '../odoo-icon/odoo-icon';
import { OdooSmartButtons, SmartButtonModel } from '../odoo-smart-buttons/odoo-smart-buttons';

/**
 * Formulario de un registro sin estados (usuario, grupo, producto, cliente, clasificación): el formulario
 * de documento de los contratos visuales §1.3 sin etapas, y su "Nuevo" (§1.4). Panel con "Nuevo", migas y
 * engranaje; barra con "Guardar" y "Descartar" solo con cambios (D-164) o al dar de alta; hoja con el tipo y
 * el nombre; y la bitácora guardada del registro, inactiva hasta guardar (D-157).
 */
@Component({
  selector: 'pc-hoja-registro',
  imports: [BotonNuevo, OdooBreadcrumb, OdooActionMenu, OdooSmartButtons, OdooChatterDrawer, OdooIcon],
  template: `
    <div class="o_control_panel">
      <div class="d-flex align-items-center gap-3">
        @if (conNuevo() && !nuevo()) {
          <pc-boton-nuevo [ruta]="ruta() + '/nuevo'" />
        }
        <pc-odoo-breadcrumb [items]="[{ label: lista(), url: ruta() }, { label: nuevo() ? 'Nuevo' : nombre() }]" />
        @if (!nuevo() && acciones().length > 0) {
          <pc-odoo-action-menu [acciones]="acciones()" />
        }
      </div>
      <div class="o_button_box">
        @if (!nuevo() && botones().length > 0) {
          <pc-odoo-smart-buttons [buttons]="botones()" (smartNavigate)="navegar($event)" />
        }
      </div>
    </div>
    <div class="p-4">
      <div class="o_statusbar">
        <div class="d-flex align-items-center gap-2">
          @if (nuevo() || sucio()) {
            <button class="btn btn-primary btn-sm fw-bold px-3" [disabled]="guardando()" (click)="guardar.emit()" data-guardar>Guardar</button>
            <button class="btn btn-outline-secondary btn-sm px-3" [disabled]="guardando()" (click)="descartar.emit()" data-descartar>Descartar</button>
          } @else {
            <ng-content select="[barra]" />
          }
        </div>
      </div>
      <div class="d-flex gap-3 align-items-start">
        <div class="o_form_sheet flex-grow-1">
          <div class="o_sheet_body">
            <h3 class="fw-bold mb-1">{{ titulo() }}</h3>
            <h4 class="fw-bold mb-3" [class.text-primary]="!nuevo()" [class.text-muted]="nuevo()" data-nombre-registro>{{ nuevo() ? 'Nuevo' : nombre() }}</h4>
            @if (error()) {
              <div class="alert alert-danger py-2 px-3 small mb-3" role="alert" data-error-hoja><pc-odoo-icon nombre="hard-stop" />{{ error() }}</div>
            }
            <ng-content />
          </div>
        </div>
        <pc-odoo-chatter-drawer [documento]="documentoChatter()" [inactivo]="nuevo()" />
      </div>
    </div>
  `,
  styles: ':host { display: block; }',
  // Con cambios sin guardar, un 401 abre el diálogo de sesión en lugar de ir a /login (spec 003, caso límite).
  host: { '[attr.data-captura-pendiente]': 'nuevo() || sucio()' },
})
export class HojaRegistro {
  private readonly router = inject(Router);

  readonly lista = input.required<string>();
  readonly ruta = input.required<string>();
  /** El tipo de registro: "Usuario", "Grupo"… */
  readonly titulo = input.required<string>();
  readonly nombre = input('');
  readonly nuevo = input(false);
  readonly sucio = input(false);
  readonly guardando = input(false);
  readonly error = input<string | null>(null);
  /** Los catálogos de CONTPAQi no se dan de alta en PolyConecta: sin "Nuevo". */
  readonly conNuevo = input(true);
  readonly acciones = input<AccionMenu[]>([]);
  readonly botones = input<SmartButtonModel[]>([]);
  /** Tipo de la bitácora en la API (`plataforma.usuario`…) y el id del registro. */
  readonly tipo = input.required<string>();
  readonly id = input<number | null>(null);

  readonly guardar = output<void>();
  readonly descartar = output<void>();

  protected readonly documentoChatter = computed(() => {
    const id = this.id();
    return id ? { tipo: this.tipo(), id } : undefined;
  });

  protected navegar(ruta: string): void {
    void this.router.navigateByUrl(ruta);
  }
}
