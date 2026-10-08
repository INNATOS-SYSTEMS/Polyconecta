import { Component, computed, input, output } from '@angular/core';
import { OdooIcon } from '../odoo-icon/odoo-icon';

export interface SmartButtonModel {
  label: string;
  countBadge: number;
  /** Nombre del catálogo de íconos (`entrega`). */
  iconClass: string;
  targetRoute: string;
  /** Documento libre sin ese origen (FR-014): se ve atenuado y no navega. */
  deshabilitado?: boolean;
  /** Tipo del documento al que lleva: fija el nombre en singular o plural y el lugar del botón (D-141). */
  tipo?: TipoBotonInteligente;
}

/**
 * Botones inteligentes (D-141): un nombre por tipo de documento, en singular con 1 y en plural con
 * cualquier otro conteo, y un orden fijo por grupo: primero los movimientos de inventario, después el
 * documento origen o relacionado, al final los documentos de control.
 */
export const BOTONES_INTELIGENTES = {
  recoleccion: { singular: 'Recolección', plural: 'Recolecciones', icono: 'recoleccion', grupo: 0 },
  traslado: { singular: 'Traslado', plural: 'Traslados', icono: 'traslado', grupo: 0 },
  recepcion: { singular: 'Recepción', plural: 'Recepciones', icono: 'recepcion', grupo: 0 },
  entrega: { singular: 'Entrega', plural: 'Entregas', icono: 'entrega', grupo: 0 },
  pedido: { singular: 'Pedido', plural: 'Pedidos', icono: 'pedido', grupo: 1 },
  orden: { singular: 'Orden de fabricación', plural: 'Órdenes de fabricación', icono: 'fabricacion', grupo: 1 },
  control: { singular: 'Control de calidad', plural: 'Controles de calidad', icono: 'calidad', grupo: 2 },
} as const;

export type TipoBotonInteligente = keyof typeof BOTONES_INTELIGENTES;

/** Arma el botón de un tipo con su nombre según el conteo. */
export function botonInteligente(tipo: TipoBotonInteligente, conteo: number, ruta: string, deshabilitado = false): SmartButtonModel {
  const b = BOTONES_INTELIGENTES[tipo];
  return { tipo, label: conteo === 1 ? b.singular : b.plural, countBadge: conteo, iconClass: b.icono, targetRoute: ruta, deshabilitado };
}

/** Réplica de Components/Forms/OdooSmartButtons.razor, con el orden y los nombres de D-141. */
@Component({
  selector: 'pc-odoo-smart-buttons',
  imports: [OdooIcon],
  template: `
    <div class="d-flex gap-1 flex-wrap">
      @for (btn of ordenados(); track btn.label) {
        <div class="o_smart_button" [class.opacity-50]="btn.deshabilitado" [attr.title]="btn.deshabilitado ? 'Documento libre: sin documento de origen' : null" (click)="btn.deshabilitado || smartNavigate.emit(btn.targetRoute)">
          <div class="d-flex align-items-center gap-1">
            <pc-odoo-icon [nombre]="btn.iconClass" contexto="inteligente" />
            <span class="stat-label">{{ btn.label }}</span>
          </div>
          <span class="stat-count">{{ btn.countBadge }}</span>
        </div>
      }
    </div>
  `,
  styles: ':host { display: contents; }',
})
export class OdooSmartButtons {
  readonly buttons = input<readonly SmartButtonModel[]>([]);
  readonly smartNavigate = output<string>();
  /** Inventario, origen y control (D-141); dentro de cada grupo, el orden en que llegan. */
  protected readonly ordenados = computed(() =>
    this.buttons().map((b, i) => ({ b, i })).sort((x, y) => grupo(x.b) - grupo(y.b) || x.i - y.i).map(x => x.b),
  );
}

const grupo = (b: SmartButtonModel): number => (b.tipo ? BOTONES_INTELIGENTES[b.tipo].grupo : 1);
