import { Routes } from '@angular/router';

/** Módulo Ventas, bajo `/ventas` (D-155). `/ventas/inventario` es la misma pantalla de Inventario Actual. */
export const VENTAS_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'pedidos' },
  { path: 'pedidos', loadComponent: () => import('./pedidos-list/pedidos-list').then(m => m.PedidosList) },
  { path: 'pedidos/nuevo', loadComponent: () => import('./pedido-nuevo/pedido-nuevo').then(m => m.PedidoNuevo) },
  { path: 'pedidos/:folio', loadComponent: () => import('./pedido-form/pedido-form').then(m => m.PedidoForm) },
  { path: 'inventario', loadComponent: () => import('../inventario/inventario-actual/inventario-actual').then(m => m.InventarioActual) },
];
