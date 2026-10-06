import { Routes } from '@angular/router';
import { folioMatcher } from '../../shared/routing/folio-matcher';
import { LogisticaForm } from './documento/logistica-form';
import { LogisticaList } from './documento/logistica-list';
import { RecoleccionForm } from './recolecciones/recoleccion-form';
import { RecoleccionesList } from './recolecciones/recolecciones-list';

/** Los folios de estas operaciones llevan "/" (SC/OUT/31688): se resuelven con folioMatcher. */
export const LOGISTICA_ROUTES: Routes = [
  { path: 'recolecciones', component: RecoleccionesList },
  { matcher: folioMatcher('recolecciones'), component: RecoleccionForm },
  { path: 'traslados', component: LogisticaList, data: { tipo: 'traslado' } },
  { matcher: folioMatcher('traslados'), component: LogisticaForm, data: { tipo: 'traslado' } },
  { path: 'recepcion', component: LogisticaList, data: { tipo: 'recepcion' } },
  { matcher: folioMatcher('recepcion'), component: LogisticaForm, data: { tipo: 'recepcion' } },
  { path: 'entregas', component: LogisticaList, data: { tipo: 'entrega' } },
  { matcher: folioMatcher('entregas'), component: LogisticaForm, data: { tipo: 'entrega' } },
];
