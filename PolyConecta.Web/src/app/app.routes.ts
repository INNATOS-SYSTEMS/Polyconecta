import { Routes } from '@angular/router';
import { PaginaNoEncontrada } from './shared/pagina-no-encontrada/pagina-no-encontrada';

/**
 * Cada módulo de CT-09 es dueño de su archivo de rutas y se carga de forma perezosa bajo su prefijo
 * (D-155). El Inicio y la galería no son módulos y se quedan en `/` y `/catalogo`.
 */
export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/plataforma/login/login').then(m => m.Login) },
  { path: '', pathMatch: 'full', loadChildren: () => import('./features/plataforma/inicio.routes').then(m => m.INICIO_ROUTES) },
  { path: 'catalogo', loadChildren: () => import('./features/catalogo/catalogo.routes').then(m => m.CATALOGO_ROUTES) },
  { path: 'ventas', loadChildren: () => import('./features/ventas/ventas.routes').then(m => m.VENTAS_ROUTES) },
  { path: 'inventario', loadChildren: () => import('./features/inventario/inventario.routes').then(m => m.INVENTARIO_ROUTES) },
  { path: 'produccion', loadChildren: () => import('./features/produccion/produccion.routes').then(m => m.PRODUCCION_ROUTES) },
  { path: 'calidad', loadChildren: () => import('./features/calidad/calidad.routes').then(m => m.CALIDAD_ROUTES) },
  { path: 'logistica', loadChildren: () => import('./features/logistica/logistica.routes').then(m => m.LOGISTICA_ROUTES) },
  { path: 'plataforma', loadChildren: () => import('./features/plataforma/plataforma.routes').then(m => m.PLATAFORMA_ROUTES) },
  { path: '**', component: PaginaNoEncontrada },
];
