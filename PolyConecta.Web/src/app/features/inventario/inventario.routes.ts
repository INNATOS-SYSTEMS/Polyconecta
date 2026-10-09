import { Routes } from '@angular/router';
import { conSesion } from '../../core/sesion/con-sesion';

/** Módulo Inventario, bajo `/inventario` (D-155). Ventas tiene un alias de Inventario Actual en `/ventas/inventario`. */
export const INVENTARIO_ROUTES: Routes = conSesion([
  { path: '', pathMatch: 'full', loadComponent: () => import('./inventario-actual/inventario-actual').then(m => m.InventarioActual) },
  { path: 'productos', loadComponent: () => import('../catalogos/productos/productos-list/productos-list').then(m => m.ProductosList) },
  { path: 'productos/:id', loadComponent: () => import('../catalogos/productos/producto-form/producto-form').then(m => m.ProductoForm) },
]);
