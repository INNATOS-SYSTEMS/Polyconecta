import { Routes } from '@angular/router';
import { PaginaPendiente } from '../../shared/pagina-pendiente/pagina-pendiente';
import { folioMatcher } from '../../shared/routing/folio-matcher';

/** Los folios de estas operaciones llevan "/" (`SC/OUT/31688`): se resuelven con folioMatcher. */
export const LOGISTICA_ROUTES: Routes = [
  { path: 'recolecciones', component: PaginaPendiente, data: { titulo: 'Recolecciones' } },
  { matcher: folioMatcher('recolecciones'), component: PaginaPendiente, data: { titulo: 'Recolección' } },
  { path: 'traslados', component: PaginaPendiente, data: { titulo: 'Traslados' } },
  { matcher: folioMatcher('traslados'), component: PaginaPendiente, data: { titulo: 'Traslado' } },
  { path: 'recepcion', component: PaginaPendiente, data: { titulo: 'Recepciones' } },
  { matcher: folioMatcher('recepcion'), component: PaginaPendiente, data: { titulo: 'Recepción' } },
  { path: 'entregas', component: PaginaPendiente, data: { titulo: 'Entregas' } },
  { matcher: folioMatcher('entregas'), component: PaginaPendiente, data: { titulo: 'Entrega' } },
];
