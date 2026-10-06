import { Routes } from '@angular/router';
import { PaginaPendiente } from '../../shared/pagina-pendiente/pagina-pendiente';

export const PRODUCCION_ROUTES: Routes = [
  { path: 'fabricacion', component: PaginaPendiente, data: { titulo: 'Órdenes de fabricación' } },
  { path: 'fabricacion/:folioOf', component: PaginaPendiente, data: { titulo: 'Orden de fabricación' } },
  { path: 'captura-masiva', component: PaginaPendiente, data: { titulo: 'Producción' } },
  { path: 'incidencias', component: PaginaPendiente, data: { titulo: 'Incidencias' } },
];
