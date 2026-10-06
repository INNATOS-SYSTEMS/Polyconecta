import { Routes } from '@angular/router';
import { PaginaPendiente } from '../../shared/pagina-pendiente/pagina-pendiente';

export const PLATAFORMA_ROUTES: Routes = [
  { path: '', pathMatch: 'full', component: PaginaPendiente, data: { titulo: 'Inicio' } },
];
