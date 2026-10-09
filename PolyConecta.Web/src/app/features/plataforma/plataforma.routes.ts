import { Routes } from '@angular/router';
import { proveerServiciosHttp } from '../../core/sesion/proveedor-http';
import { sesionGuard } from '../../core/sesion/sesion.guard';

export const PLATAFORMA_ROUTES: Routes = [
  {
    path: '',
    canActivate: [sesionGuard],
    providers: [proveerServiciosHttp()],
    children: [
      {
        path: 'usuarios',
        loadComponent: () =>
          import('./usuarios/usuarios-list/usuarios-list').then(m => m.UsuariosList),
      },
      {
        path: 'usuarios/nuevo',
        loadComponent: () =>
          import('./usuarios/usuario-form/usuario-form').then(m => m.UsuarioForm),
      },
      {
        path: 'usuarios/:id',
        loadComponent: () =>
          import('./usuarios/usuario-form/usuario-form').then(m => m.UsuarioForm),
      },
      {
        path: 'grupos',
        loadComponent: () =>
          import('./grupos/grupos-list/grupos-list').then(m => m.GruposList),
      },
      {
        path: 'grupos/nuevo',
        loadComponent: () =>
          import('./grupos/grupo-form/grupo-form').then(m => m.GrupoForm),
      },
      {
        path: 'grupos/:id',
        loadComponent: () =>
          import('./grupos/grupo-form/grupo-form').then(m => m.GrupoForm),
      },
      {
        path: 'sincronizacion',
        loadComponent: () =>
          import('./sincronizacion/sincronizacion').then(m => m.Sincronizacion),
      },
    ],
  },
];
