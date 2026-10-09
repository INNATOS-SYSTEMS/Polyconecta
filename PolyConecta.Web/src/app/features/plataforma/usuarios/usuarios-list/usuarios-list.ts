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

export interface FilaUsuario {
  id: number;
  usuario: string;
  nombre: string;
  email: string;
  activo: boolean;
  _filtros?: string[];
  [key: string]: unknown;
}


@Component({
  selector: 'pc-usuarios-list',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSearchPanel, OdooList],
  templateUrl: './usuarios-list.html',
  styles: ':host { display: contents; }',
})
export class UsuariosList {
  private readonly router = inject(Router);

  protected readonly vista = signal<SearchView<FilaUsuario>>(vistaVacia());
  protected readonly favoritos = new FavoritosHttp();
  protected readonly idUsuario = (f: FilaUsuario) => String(f.id);

  protected readonly columnas: ColumnaLista<FilaUsuario>[] = [
    { campo: 'usuario', titulo: 'Usuario', clase: 'fw-semibold text-primary' },
    { campo: 'nombre', titulo: 'Nombre' },
    { campo: 'email', titulo: 'Correo' },
    { campo: 'activo', titulo: 'Estado', tipo: 'estado', texto: f => (f.activo ? 'Activo' : 'Archivado') },
  ];

  protected readonly origen = new OrigenHttp<FilaUsuario>({
    modulo: 'plataforma',
    lista: 'usuarios',
    id: f => String(f.id),
  });

  constructor() {
    void this.origen.vista().then(v => this.vista.set(v));
  }

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>(['Activos']);
  protected readonly agrupaciones = signal<string[]>([]);

  protected abrir(f: FilaUsuario): void {
    void this.router.navigateByUrl(`/plataforma/usuarios/${f.id}`);
  }
}
