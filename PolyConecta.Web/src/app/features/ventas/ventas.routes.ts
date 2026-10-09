import { Routes } from '@angular/router';
import { conSesion } from '../../core/sesion/con-sesion';

/** Módulo Ventas, bajo `/ventas` (D-155). `/ventas/inventario` es la misma pantalla de Inventario Actual. */
export const VENTAS_ROUTES: Routes = conSesion([
  { path: '', pathMatch: 'full', redirectTo: 'pedidos' },
  { path: 'pedidos', loadComponent: () => import('./pedidos-list/pedidos-list').then(m => m.PedidosList) },
  { path: 'pedidos/nuevo', loadComponent: () => import('./pedido-nuevo/pedido-nuevo').then(m => m.PedidoNuevo) },
  { path: 'pedidos/:id', loadComponent: () => import('./pedido-form/pedido-form').then(m => m.PedidoForm) },
  { path: 'clientes', loadComponent: () => import('../catalogos/clientes/clientes-list/clientes-list').then(m => m.ClientesList) },
  { path: 'clientes/:id', loadComponent: () => import('../catalogos/clientes/cliente-form/cliente-form').then(m => m.ClienteForm) },
  { path: 'inventario', loadComponent: () => import('../inventario/inventario-actual/inventario-actual').then(m => m.InventarioActual) },
]);
