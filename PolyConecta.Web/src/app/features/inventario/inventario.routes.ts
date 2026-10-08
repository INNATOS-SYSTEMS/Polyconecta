import { Routes } from '@angular/router';

/** Módulo Inventario, bajo `/inventario` (D-155). Ventas tiene un alias de Inventario Actual en `/ventas/inventario`. */
export const INVENTARIO_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./inventario-actual/inventario-actual').then(m => m.InventarioActual) },
];
