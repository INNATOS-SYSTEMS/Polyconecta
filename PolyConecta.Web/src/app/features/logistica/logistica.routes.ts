import { Routes } from '@angular/router';
import { folioMatcher } from '../../shared/routing/folio-matcher';

/** Los folios de estas operaciones llevan "/" (SC/OUT/31688): se resuelven con folioMatcher. */
export const LOGISTICA_ROUTES: Routes = [
  { path: 'recolecciones', loadComponent: () => import('./recolecciones/recolecciones-list').then(m => m.RecoleccionesList) },
  { path: 'recolecciones/nuevo', loadComponent: () => import('./recoleccion-nueva/recoleccion-nueva').then(m => m.RecoleccionNueva) },
  { matcher: folioMatcher('recolecciones'), loadComponent: () => import('./recolecciones/recoleccion-form').then(m => m.RecoleccionForm) },
  { path: 'traslados', loadComponent: () => import('./documento/logistica-list').then(m => m.LogisticaList), data: { tipo: 'traslado' } },
  { path: 'traslados/nuevo', loadComponent: () => import('./documento/logistica-nuevo').then(m => m.LogisticaNuevo), data: { tipo: 'traslado' } },
  { matcher: folioMatcher('traslados'), loadComponent: () => import('./documento/logistica-form').then(m => m.LogisticaForm), data: { tipo: 'traslado' } },
  { path: 'recepcion', loadComponent: () => import('./documento/logistica-list').then(m => m.LogisticaList), data: { tipo: 'recepcion' } },
  { path: 'recepcion/nuevo', loadComponent: () => import('./documento/logistica-nuevo').then(m => m.LogisticaNuevo), data: { tipo: 'recepcion' } },
  { matcher: folioMatcher('recepcion'), loadComponent: () => import('./documento/logistica-form').then(m => m.LogisticaForm), data: { tipo: 'recepcion' } },
  { path: 'entregas', loadComponent: () => import('./documento/logistica-list').then(m => m.LogisticaList), data: { tipo: 'entrega' } },
  { path: 'entregas/nuevo', loadComponent: () => import('./documento/logistica-nuevo').then(m => m.LogisticaNuevo), data: { tipo: 'entrega' } },
  { matcher: folioMatcher('entregas'), loadComponent: () => import('./documento/logistica-form').then(m => m.LogisticaForm), data: { tipo: 'entrega' } },
];
