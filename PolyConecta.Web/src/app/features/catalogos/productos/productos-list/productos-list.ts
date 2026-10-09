import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { OdooBreadcrumb } from '../../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooList } from '../../../../shared/odoo-list/odoo-list';
import { ColumnaLista } from '../../../../shared/odoo-list/columnas';
import { OdooSearchPanel } from '../../../../shared/odoo-search-panel/odoo-search-panel';
import { SearchView } from '../../../../core/search/search-view';
import { OrigenHttp } from '../../../../core/lista/origen-http';

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

const VISTA_PRODUCTOS: SearchView<FilaProducto> = {
  campos: [
    { etiqueta: 'Código', valor: f => f.codigo },
    { etiqueta: 'Nombre', valor: f => f.nombre },
    { etiqueta: 'Clasificación', valor: f => f.clasificacion },
  ],
  filtros: [
    { nombre: 'Activos', campo: 'Estado', condicion: f => f.activo },
    { nombre: 'Archivados', campo: 'Estado', condicion: f => !f.activo },
  ],
  agrupaciones: [
    { etiqueta: 'Clasificación', clave: f => f.clasificacion ?? 'Sin clasificar' },
    { etiqueta: 'Estado', clave: f => (f.activo ? 'Activo' : 'Archivado') },
  ],
};

@Component({
  selector: 'pc-productos-list',
  imports: [OdooBreadcrumb, OdooSearchPanel, OdooList],
  templateUrl: './productos-list.html',
  styles: ':host { display: contents; }',
})
export class ProductosList {
  private readonly router = inject(Router);

  protected readonly vista = VISTA_PRODUCTOS;
  protected readonly idProducto = (f: FilaProducto) => String(f.id);

  protected readonly columnas: ColumnaLista<FilaProducto>[] = [
    { campo: 'codigo', titulo: 'Código', clase: 'fw-semibold text-primary' },
    { campo: 'nombre', titulo: 'Nombre' },
    { campo: 'unidadBase', titulo: 'Unidad' },
    { campo: 'clasificacion', titulo: 'Clasificación' },
    { campo: 'activo', titulo: 'Estado', tipo: 'estado', texto: f => (f.activo ? 'Activo' : 'Archivado') },
  ];

  protected readonly origen = new OrigenHttp<FilaProducto>({
    modulo: 'inventario',
    lista: 'productos',
    id: f => String(f.id),
    buscables: ['codigo', 'nombre', 'clasificacion'],
    vista: VISTA_PRODUCTOS,
  });

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>(['Activos']);
  protected readonly agrupaciones = signal<string[]>([]);

  protected abrir(f: FilaProducto): void {
    void this.router.navigateByUrl(`/inventario/productos/${f.id}`);
  }
}
