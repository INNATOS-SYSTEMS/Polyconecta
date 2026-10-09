import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { BotonNuevo } from '../../../../shared/boton-nuevo/boton-nuevo';
import { OdooBreadcrumb } from '../../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooList } from '../../../../shared/odoo-list/odoo-list';
import { ColumnaLista } from '../../../../shared/odoo-list/columnas';
import { OdooSearchPanel } from '../../../../shared/odoo-search-panel/odoo-search-panel';
import { SearchView } from '../../../../core/search/search-view';
import { OrigenHttp, vistaVacia } from '../../../../core/lista/origen-http';
import { FavoritosHttp } from '../../../../core/lista/favoritos-http';

export interface FilaGrupo {
  id: number;
  codigo: string;
  nombre: string;
  descripcion: string;
  activo: boolean;
  miembros?: number;
  _filtros?: string[];
  [key: string]: unknown;
}


@Component({
  selector: 'pc-grupos-list',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSearchPanel, OdooList],
  templateUrl: './grupos-list.html',
  styles: ':host { display: contents; }',
})
export class GruposList {
  private readonly router = inject(Router);

  protected readonly vista = signal<SearchView<FilaGrupo>>(vistaVacia());
  protected readonly favoritos = new FavoritosHttp();
  protected readonly idGrupo = (f: FilaGrupo) => String(f.id);

  protected readonly columnas: ColumnaLista<FilaGrupo>[] = [
    { campo: 'codigo', titulo: 'Código', clase: 'fw-semibold text-primary' },
    { campo: 'nombre', titulo: 'Nombre' },
    { campo: 'descripcion', titulo: 'Descripción' },
    { campo: 'activo', titulo: 'Estado', tipo: 'estado', texto: f => (f.activo ? 'Activo' : 'Archivado') },
  ];

  protected readonly origen = new OrigenHttp<FilaGrupo>({
    modulo: 'plataforma',
    lista: 'grupos',
    id: f => String(f.id),
  });

  constructor() {
    void this.origen.vista().then(v => this.vista.set(v));
  }

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>(['Activos']);
  protected readonly agrupaciones = signal<string[]>([]);

  protected abrir(f: FilaGrupo): void {
    void this.router.navigateByUrl(`/plataforma/grupos/${f.id}`);
  }
}
