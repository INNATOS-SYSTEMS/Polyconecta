import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { OdooBreadcrumb } from '../../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooList } from '../../../../shared/odoo-list/odoo-list';
import { ColumnaLista } from '../../../../shared/odoo-list/columnas';
import { OdooSearchPanel } from '../../../../shared/odoo-search-panel/odoo-search-panel';
import { SearchView } from '../../../../core/search/search-view';
import { OrigenHttp } from '../../../../core/lista/origen-http';

export interface FilaCliente {
  id: number;
  codigo: string;
  razonSocial: string;
  rfc?: string;
  moneda?: string;
  activo: boolean;
  _filtros?: string[];
  [key: string]: unknown;
}

const VISTA_CLIENTES: SearchView<FilaCliente> = {
  campos: [
    { etiqueta: 'Código', valor: f => f.codigo },
    { etiqueta: 'Razón social', valor: f => f.razonSocial },
    { etiqueta: 'RFC', valor: f => f.rfc },
  ],
  filtros: [
    { nombre: 'Activos', campo: 'Estado', condicion: f => f.activo },
    { nombre: 'Archivados', campo: 'Estado', condicion: f => !f.activo },
  ],
  agrupaciones: [
    { etiqueta: 'Moneda', clave: f => f.moneda ?? 'Sin moneda' },
    { etiqueta: 'Estado', clave: f => (f.activo ? 'Activo' : 'Archivado') },
  ],
};

@Component({
  selector: 'pc-clientes-list',
  imports: [OdooBreadcrumb, OdooSearchPanel, OdooList],
  templateUrl: './clientes-list.html',
  styles: ':host { display: contents; }',
})
export class ClientesList {
  private readonly router = inject(Router);

  protected readonly vista = VISTA_CLIENTES;
  protected readonly idCliente = (f: FilaCliente) => String(f.id);

  protected readonly columnas: ColumnaLista<FilaCliente>[] = [
    { campo: 'codigo', titulo: 'Código', clase: 'fw-semibold text-primary' },
    { campo: 'razonSocial', titulo: 'Razón social' },
    { campo: 'rfc', titulo: 'RFC' },
    { campo: 'moneda', titulo: 'Moneda' },
    { campo: 'activo', titulo: 'Estado', tipo: 'estado', texto: f => (f.activo ? 'Activo' : 'Archivado') },
  ];

  protected readonly origen = new OrigenHttp<FilaCliente>({
    modulo: 'ventas',
    lista: 'clientes',
    id: f => String(f.id),
    buscables: ['codigo', 'razonSocial', 'rfc'],
    vista: VISTA_CLIENTES,
  });

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>(['Activos']);
  protected readonly agrupaciones = signal<string[]>([]);

  protected abrir(f: FilaCliente): void {
    void this.router.navigateByUrl(`/ventas/clientes/${f.id}`);
  }
}
