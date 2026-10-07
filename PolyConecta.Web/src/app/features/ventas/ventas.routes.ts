import { Routes } from '@angular/router';

export const VENTAS_ROUTES: Routes = [
  { path: 'pedidos', loadComponent: () => import('./pedidos-list/pedidos-list').then(m => m.PedidosList) },
  { path: 'pedidos/nuevo', loadComponent: () => import('./pedido-nuevo/pedido-nuevo').then(m => m.PedidoNuevo) },
  { path: 'pedidos/:folio', loadComponent: () => import('./pedido-form/pedido-form').then(m => m.PedidoForm) },
];
