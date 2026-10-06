import { Routes } from '@angular/router';
import { PedidoForm } from './pedido-form/pedido-form';
import { PedidosList } from './pedidos-list/pedidos-list';

export const VENTAS_ROUTES: Routes = [
  { path: 'pedidos', component: PedidosList },
  { path: 'pedidos/:folio', component: PedidoForm },
];
