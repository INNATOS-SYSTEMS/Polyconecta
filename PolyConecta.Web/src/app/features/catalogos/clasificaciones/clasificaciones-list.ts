import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { OrigenEnMemoria } from '../../../core/lista/origen-en-memoria';
import { SearchView } from '../../../core/search/search-view';
import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooList } from '../../../shared/odoo-list/odoo-list';
import { OdooSeleccion } from '../../../shared/odoo-list/odoo-seleccion';
import { ColumnaLista } from '../../../shared/odoo-list/columnas';
import { OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';
import { CatalogosService, ClasificacionDto } from '../catalogos.service';

const VISTA: SearchView<ClasificacionDto> = {
  campos: [{ etiqueta: 'Código', valor: c => c.codigo }, { etiqueta: 'Nombre', valor: c => c.nombre }],
  filtros: [
    { nombre: 'De CONTPAQi', campo: 'Origen', condicion: c => !!c.valorErp },
    { nombre: 'Propias', campo: 'Origen', condicion: c => !c.valorErp },
  ],
  agrupaciones: [],
};

/**
 * Clasificaciones propias de los productos (FR-017, D-86, P-13 de la propuesta aprobada): la lista de los
 * contratos visuales sobre el catálogo completo, que es corto.
 */
@Component({
  selector: 'pc-clasificaciones-list',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSearchPanel, OdooList, OdooSeleccion],
  template: `
    <div class="o_control_panel">
      <div class="d-flex align-items-center gap-3">
        <pc-boton-nuevo [ruta]="'/plataforma/clasificaciones'" />
        <pc-odoo-breadcrumb [items]="[{ label: 'Clasificaciones' }]" />
      </div>
      <pc-odoo-search-panel [view]="vista" [(texto)]="busqueda" [(filtrosActivos)]="filtros" [conFavoritos]="false">
        <pc-odoo-seleccion [lista]="tabla" />
      </pc-odoo-search-panel>
      <div class="d-flex align-items-center gap-2"></div>
    </div>
    <div class="p-4">
      <pc-odoo-list #tabla lista="plataforma.clasificaciones" [origen]="origen()" [columnas]="columnas" [idDe]="idDe"
                    mensajeVacio="No hay clasificaciones" [(busqueda)]="busqueda" [(nombrados)]="filtros" (abrir)="abrir($event)" />
    </div>
  `,
  styles: ':host { display: contents; }',
})
export class ClasificacionesList {
  private readonly router = inject(Router);
  private readonly catalogos = inject(CatalogosService);
  private readonly datos = signal<ClasificacionDto[]>([]);

  protected readonly vista = VISTA;
  protected readonly busqueda = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly idDe = (c: ClasificacionDto) => String(c.id);
  protected readonly columnas: ColumnaLista<ClasificacionDto>[] = [
    { campo: 'codigo', titulo: 'Código', clase: 'fw-semibold text-primary' },
    { campo: 'nombre', titulo: 'Nombre' },
    { campo: 'valorErp', titulo: 'Valor en CONTPAQi', texto: c => c.valorErp ?? '—' },
  ];
  /** Un origen nuevo cuando llega el catálogo: la lista vuelve a consultar. */
  protected readonly origen = computed(() => { const l = this.datos(); return new OrigenEnMemoria<ClasificacionDto>({ datos: () => l, id: c => String(c.id), vista: VISTA }); });

  constructor() {
    void this.catalogos.listarClasificaciones().then(l => this.datos.set(l)).catch(() => undefined);
  }

  protected abrir(c: ClasificacionDto): void {
    void this.router.navigateByUrl(`/plataforma/clasificaciones/${c.id}`);
  }
}
