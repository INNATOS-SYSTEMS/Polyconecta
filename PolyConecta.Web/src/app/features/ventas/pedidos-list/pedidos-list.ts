import { Component, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
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
import { ETAPAS_PEDIDO, FilaPedido, PedidosAcciones, VISTA_PEDIDOS } from '../pedidos-acciones';

/** Lista y kanban de pedidos de venta conectados a la API HTTP (contracts/api-listas.md, D-154). */
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
  protected readonly vista = VISTA_PEDIDOS;
  protected readonly lista = viewChild(OdooList<FilaPedido>);

  protected readonly origen = this.acciones.origen();
  protected readonly transiciones = this.acciones.transiciones();
  protected readonly fechaCampo = fechaCampo;
  protected readonly etapas = ETAPAS_PEDIDO.map(e => ({ valor: e, titulo: e }));
  protected readonly idPedido = (f: FilaPedido) => String(f.id);
  protected readonly etapaPedido = (f: FilaPedido) => f.estado;
  protected readonly columnas: ColumnaLista<FilaPedido>[] = [
    { campo: 'folio', titulo: 'Folio', clase: 'fw-semibold text-primary' },
    { campo: 'cliente', titulo: 'Cliente' },
    { campo: 'fechaPromesa', titulo: 'Entrega estimada', tipo: 'fecha' },
    { campo: 'estado', titulo: 'Estado', tipo: 'estado' },
  ];

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly agrupaciones = signal<string[]>([]);

  protected abrir(f: FilaPedido): void {
    void this.router.navigateByUrl(`/ventas/pedidos/${f.id}`);
  }
}
