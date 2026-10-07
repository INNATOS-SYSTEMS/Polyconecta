import { Routes } from '@angular/router';
import { PaginaPendiente } from '../../shared/pagina-pendiente/pagina-pendiente';

export const VENTAS_ROUTES: Routes = [
  { path: 'pedidos', component: PaginaPendiente, data: { titulo: 'Pedidos' } },
  { path: 'pedidos/:folio', component: PaginaPendiente, data: { titulo: 'Pedido' } },
];
