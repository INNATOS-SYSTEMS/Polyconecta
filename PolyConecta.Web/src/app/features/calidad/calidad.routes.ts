import { Routes } from '@angular/router';
import { PaginaPendiente } from '../../shared/pagina-pendiente/pagina-pendiente';

export const CALIDAD_ROUTES: Routes = [
  { path: 'calidad', component: PaginaPendiente, data: { titulo: 'Calidad' } },
  { path: 'calidad/:folioOf', component: PaginaPendiente, data: { titulo: 'Control de calidad' } },
];
