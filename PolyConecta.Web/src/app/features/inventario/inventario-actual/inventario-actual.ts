import { Component, inject, signal } from '@angular/core';
import { n1, ordenCultural } from '../../../core/format/numero';
import { nombreProducto } from '../../../core/format/producto';
import { OrigenEnMemoria } from '../../../core/lista/origen-en-memoria';
import { StockQuant } from '../../../core/models/inventario';
import { adaptarVista } from '../../../core/search/search-view';
import { INVENTARIO_ACTUAL } from '../../../core/search/views';
import { InventoryState } from '../../../core/state/inventory-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { ColumnaLista } from '../../../shared/odoo-list/columnas';
import { OdooList } from '../../../shared/odoo-list/odoo-list';
import { OdooSeleccion } from '../../../shared/odoo-list/odoo-seleccion';
import { OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';
import { OdooViewSwitcher } from '../../../shared/odoo-view-switcher/odoo-view-switcher';

/** Renglón de existencias: la cantidad de un lote en una ubicación. */
interface FilaInventario {
  id: string;
  ubicacion: string;
  producto: string;
  lote: string;
  cantidad: number;
  unidad: string;
  quant: StockQuant;
}

/**
 * Réplica de Pages/InventarioActualList.razor (en /inventario y /ventas/inventario) sobre `pc-odoo-list`
 * (spec 011, P8): agrupa por omisión con la vista de búsqueda, los grupos abren plegados y el total de
 * un grupo solo aparece si todo el grupo comparte unidad. Lista sin kanban.
 */
@Component({
  selector: 'pc-inventario-actual',
  imports: [OdooBreadcrumb, OdooSearchPanel, OdooViewSwitcher, OdooList, OdooSeleccion],
  templateUrl: './inventario-actual.html',
  styles: ':host { display: contents; }',
})
export class InventarioActual {
  private readonly inv = inject(InventoryState);
  protected readonly vista = adaptarVista(INVENTARIO_ACTUAL, (f: FilaInventario) => f.quant);

  protected readonly origen = new OrigenEnMemoria<FilaInventario>({
    datos: () =>
      [...this.inv.existencias()]
        .sort((a, b) => ordenCultural(a.ubicacion, b.ubicacion) || ordenCultural(a.producto.clave, b.producto.clave) || ordenCultural(a.lote, b.lote))
        .map(q => ({
          id: `${q.ubicacion}|${q.producto.clave}|${q.lote}`, ubicacion: q.ubicacion, producto: nombreProducto(q.producto.clave, q.producto.nombre),
          lote: q.lote, cantidad: q.cantidad, unidad: q.producto.unidad, quant: q,
        })),
    id: f => f.id,
    vista: this.vista,
    sumables: ['cantidad'],
    unidad: f => f.unidad,
  });
  protected readonly idFila = (f: FilaInventario) => f.id;
  protected readonly columnas: ColumnaLista<FilaInventario>[] = [
    { campo: 'ubicacion', titulo: 'Ubicación' },
    { campo: 'producto', titulo: 'Producto' },
    { campo: 'lote', titulo: 'Lote', clase: 'font-monospace small' },
    { campo: 'cantidad', titulo: 'Cantidad', tipo: 'numero', texto: f => n1(f.cantidad), sumable: true },
    { campo: 'unidad', titulo: 'Unidad', clase: 'text-muted' },
  ];

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly agrupaciones = signal<string[]>([...(INVENTARIO_ACTUAL.agrupacionesPorDefecto ?? [])]);
}
