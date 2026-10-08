import { Component, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { PEDIDOS } from '../../../core/search/views';
import { UiViewState } from '../../../core/state/ui-view-state';
import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { fechaCampo } from '../../../core/format/numero';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { OdooKanban } from '../../../shared/odoo-kanban/odoo-kanban';
import { ColumnaLista } from '../../../shared/odoo-list/columnas';
import { OdooList } from '../../../shared/odoo-list/odoo-list';
import { OdooPager } from '../../../shared/odoo-pager/odoo-pager';
import { OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';
import { OdooViewSwitcher } from '../../../shared/odoo-view-switcher/odoo-view-switcher';
import { ETAPAS_PEDIDO, FilaPedido, PedidosAcciones } from '../pedidos-acciones';

/** Réplica de Pages/PedidosList.razor sobre los componentes de la spec 011: lista y kanban desde un origen de datos. */
@Component({
  selector: 'pc-pedidos-list',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSearchPanel, OdooViewSwitcher, OdooList, OdooKanban, OdooIcon, OdooPager],
  templateUrl: './pedidos-list.html',
  styles: ':host { display: contents; }',
})
export class PedidosList {
  private readonly router = inject(Router);
  private readonly acciones = inject(PedidosAcciones);
  protected readonly viewState = inject(UiViewState);
  protected readonly vista = PEDIDOS;
  protected readonly lista = viewChild(OdooList<FilaPedido>);

  protected readonly origen = this.acciones.origen();
  protected readonly transiciones = this.acciones.transiciones();
  protected readonly fechaCampo = fechaCampo;
  protected readonly etapas = ETAPAS_PEDIDO.map(e => ({ valor: e, titulo: e }));
  protected readonly idPedido = (f: FilaPedido) => f.id;
  protected readonly etapaPedido = (f: FilaPedido) => f.estado;
  protected readonly columnas: ColumnaLista<FilaPedido>[] = [
    { campo: 'folio', titulo: 'Folio', clase: 'fw-semibold text-primary' },
    { campo: 'cliente', titulo: 'Cliente' },
    { campo: 'producto', titulo: 'SKU Producto Terminado' },
    { campo: 'cantidad', titulo: 'Cantidad', clase: 'text-end', ordenable: false },
    { campo: 'estado', titulo: 'Estado', tipo: 'estado' },
  ];

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly agrupaciones = signal<string[]>([]);

  protected abrir(f: FilaPedido): void {
    void this.router.navigateByUrl(`/pedidos/${f.folio}`);
  }
}
