import { Component, computed, inject, input } from '@angular/core';
import { Dialog } from '@angular/cdk/dialog';
import { CdkMenu, CdkMenuItem, CdkMenuTrigger } from '@angular/cdk/menu';
import { firstValueFrom } from 'rxjs';
import { AvisosService } from '../odoo-dialog/avisos';
import { abrirDialogo, OdooConfirmacion } from '../odoo-dialog/odoo-dialog';
import { OdooIcon } from '../odoo-icon/odoo-icon';
import { AccionMasiva } from './columnas';
import { OdooList } from './odoo-list';

/**
 * Selección de una lista en el panel de control (contratos visuales §1.1, D-167): a la derecha de la
 * búsqueda y los filtros, sin desplazar la tabla. El conteo con su botón para quitarla y el menú
 * "Acciones": "Exportar" siempre, luego las acciones contextuales de la lista y, al final, separadas y en
 * rojo, las destructivas (Eliminar), que piden confirmación. Se proyecta dentro de `pc-odoo-search-panel`.
 */
@Component({
  selector: 'pc-odoo-seleccion',
  imports: [CdkMenuTrigger, CdkMenu, CdkMenuItem, OdooIcon],
  template: `
    @if (n() > 0) {
      <div class="o_selection_box" data-lista="seleccion">
        <span>{{ n() }} {{ n() === 1 ? 'seleccionado' : 'seleccionados' }}</span>
        <button type="button" class="o_selection_clear" aria-label="Quitar selección" title="Quitar selección" data-lista="quitar-seleccion"
                (click)="tabla().limpiarSeleccion()"><pc-odoo-icon nombre="quitar" /></button>
      </div>
      <button type="button" class="btn btn-sm btn-outline-secondary o_selection_actions" [cdkMenuTriggerFor]="menu" data-lista="acciones">
        <pc-odoo-icon nombre="acciones" />Acciones
      </button>
      <ng-template #menu>
        <div class="o_dropdown_panel" cdkMenu>
          <button type="button" class="o_search_menu_item" cdkMenuItem data-lista="exportar" (cdkMenuItemTriggered)="exportar()">
            <pc-odoo-icon nombre="excel" />Exportar
          </button>
          @for (a of ordenadas(); track a.nombre) {
            @if (a.peligrosa && $index === normales().length) { <div class="o_dropdown_divider" role="separator"></div> }
            <button type="button" class="o_search_menu_item" [class.text-danger]="a.peligrosa" cdkMenuItem [cdkMenuItemDisabled]="!!razonDe(a)"
                    [attr.title]="razonDe(a)" [attr.data-accion-masiva]="a.nombre" (cdkMenuItemTriggered)="ejecutar(a)">
              @if (a.icono) { <pc-odoo-icon [nombre]="a.icono" /> }{{ a.nombre }}
            </button>
          }
        </div>
      </ng-template>
    }
  `,
  styles: ':host { display: contents; }',
})
export class OdooSeleccion<T> {
  private readonly dialog = inject(Dialog);
  private readonly avisos = inject(AvisosService);

  /** La tabla: una referencia de plantilla o, si vive dentro de un `@if` (lista o kanban), un `viewChild`. */
  readonly lista = input.required<OdooList<T> | undefined>();
  protected readonly tabla = computed(() => this.lista()!);

  protected readonly n = computed(() => this.lista()?.idsSeleccionados().length ?? 0);
  protected readonly normales = computed(() => this.lista()?.acciones().filter(a => !a.peligrosa) ?? []);
  /** Las destructivas al final, separadas de las demás. */
  protected readonly ordenadas = computed(() => [...this.normales(), ...(this.lista()?.acciones().filter(a => a.peligrosa) ?? [])]);

  protected razonDe(a: AccionMasiva): string | null {
    return a.razonDeshabilitada?.(this.tabla().filasSeleccionadas()) ?? null;
  }

  protected async exportar(): Promise<void> {
    await this.tabla().exportar();
  }

  protected async ejecutar(a: AccionMasiva): Promise<void> {
    const lista = this.tabla();
    const ids = lista.idsSeleccionados();
    if (a.peligrosa || a.confirmar) {
      const mensaje = a.confirmar?.(ids.length)
        ?? `¿${a.nombre} ${ids.length === 1 ? 'el registro seleccionado' : `los ${ids.length} registros seleccionados`}? No se puede deshacer.`;
      const ref = abrirDialogo<boolean>(this.dialog, OdooConfirmacion, { titulo: a.nombre, mensaje, confirmar: a.nombre });
      if (!(await firstValueFrom(ref.closed))) return;
    }
    try {
      await a.ejecutar(ids, lista.filasSeleccionadas());
    } catch (e) {
      this.avisos.error((e as Error).message || `No se pudo ${a.nombre.toLowerCase()}.`);
    } finally {
      lista.limpiarSeleccion();
      lista.recargar();
    }
  }
}
