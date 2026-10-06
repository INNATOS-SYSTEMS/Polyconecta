import { Routes } from '@angular/router';
import { PedidoNuevo } from './pedido-nuevo/pedido-nuevo';
import { PedidoForm } from './pedido-form/pedido-form';
import { PedidosList } from './pedidos-list/pedidos-list';

export const VENTAS_ROUTES: Routes = [
  { path: 'pedidos', component: PedidosList },
  { path: 'pedidos/nuevo', component: PedidoNuevo },
  { path: 'pedidos/:folio', component: PedidoForm },
];
