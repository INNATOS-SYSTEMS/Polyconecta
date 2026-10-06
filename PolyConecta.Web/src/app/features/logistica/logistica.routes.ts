import { Routes } from '@angular/router';
import { folioMatcher } from '../../shared/routing/folio-matcher';
import { LogisticaForm } from './documento/logistica-form';
import { LogisticaList } from './documento/logistica-list';
import { LogisticaNuevo } from './documento/logistica-nuevo';
import { RecoleccionNueva } from './recoleccion-nueva/recoleccion-nueva';
import { RecoleccionForm } from './recolecciones/recoleccion-form';
import { RecoleccionesList } from './recolecciones/recolecciones-list';

/** Los folios de estas operaciones llevan "/" (SC/OUT/31688): se resuelven con folioMatcher. */
export const LOGISTICA_ROUTES: Routes = [
  { path: 'recolecciones', component: RecoleccionesList },
  { path: 'recolecciones/nuevo', component: RecoleccionNueva },
  { matcher: folioMatcher('recolecciones'), component: RecoleccionForm },
  { path: 'traslados', component: LogisticaList, data: { tipo: 'traslado' } },
  { path: 'traslados/nuevo', component: LogisticaNuevo, data: { tipo: 'traslado' } },
  { matcher: folioMatcher('traslados'), component: LogisticaForm, data: { tipo: 'traslado' } },
  { path: 'recepcion', component: LogisticaList, data: { tipo: 'recepcion' } },
  { path: 'recepcion/nuevo', component: LogisticaNuevo, data: { tipo: 'recepcion' } },
  { matcher: folioMatcher('recepcion'), component: LogisticaForm, data: { tipo: 'recepcion' } },
  { path: 'entregas', component: LogisticaList, data: { tipo: 'entrega' } },
  { path: 'entregas/nuevo', component: LogisticaNuevo, data: { tipo: 'entrega' } },
  { matcher: folioMatcher('entregas'), component: LogisticaForm, data: { tipo: 'entrega' } },
];
