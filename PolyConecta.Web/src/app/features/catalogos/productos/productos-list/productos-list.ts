import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { nombreProducto } from '../../../../core/format/producto';
import { OdooBreadcrumb } from '../../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooList } from '../../../../shared/odoo-list/odoo-list';
import { OdooSeleccion } from '../../../../shared/odoo-list/odoo-seleccion';
import { ColumnaLista } from '../../../../shared/odoo-list/columnas';
import { OdooSearchPanel } from '../../../../shared/odoo-search-panel/odoo-search-panel';
import { SearchView } from '../../../../core/search/search-view';
import { OrigenHttp, vistaVacia } from '../../../../core/lista/origen-http';
import { FavoritosHttp } from '../../../../core/lista/favoritos-http';

export interface FilaProducto {
  id: number;
  codigo: string;
  nombre: string;
  unidadBase?: string;
  clasificacion?: string;
  activo: boolean;
  _filtros?: string[];
  [key: string]: unknown;
}


@Component({
  selector: 'pc-productos-list',
  imports: [OdooBreadcrumb, OdooSearchPanel, OdooList, OdooSeleccion],
  templateUrl: './productos-list.html',
  styles: ':host { display: contents; }',
})
export class ProductosList {
  private readonly router = inject(Router);

  protected readonly vista = signal<SearchView<FilaProducto>>(vistaVacia());
  protected readonly favoritos = new FavoritosHttp();
  protected readonly idProducto = (f: FilaProducto) => String(f.id);

  protected readonly columnas: ColumnaLista<FilaProducto>[] = [
    // "Clave - Nombre" en una sola columna (D-141); ordena por la clave.
    { campo: 'codigo', titulo: 'Producto', texto: f => nombreProducto(f.codigo, f.nombre) },
    { campo: 'unidadBase', titulo: 'Unidad base' },
    { campo: 'clasificacion', titulo: 'Clasificación' },
    { campo: 'activo', titulo: 'Estado', tipo: 'estado', texto: f => (f.activo ? 'Activo' : 'Archivado') },
  ];

  protected readonly origen = new OrigenHttp<FilaProducto>({
    modulo: 'inventario',
    lista: 'productos',
    id: f => String(f.id),
  });

  constructor() {
    void this.origen.vista().then(v => this.vista.set(v));
  }

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>(['Activos']);
  protected readonly agrupaciones = signal<string[]>([]);

  protected abrir(f: FilaProducto): void {
    void this.router.navigateByUrl(`/inventario/productos/${f.id}`);
  }
}
