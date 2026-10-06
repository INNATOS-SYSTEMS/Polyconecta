import { Routes } from '@angular/router';

/** `/ventas/inventario` es un alias de la misma pantalla, como en InventarioActualList.razor. */
export const INVENTARIO_ROUTES: Routes = [
  { path: 'inventario', loadComponent: () => import('./inventario-actual/inventario-actual').then(m => m.InventarioActual) },
  { path: 'ventas/inventario', loadComponent: () => import('./inventario-actual/inventario-actual').then(m => m.InventarioActual) },
];
