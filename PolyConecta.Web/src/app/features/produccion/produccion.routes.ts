import { Routes } from '@angular/router';

/** Módulo Producción, bajo `/produccion` (D-155). */
export const PRODUCCION_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'fabricacion' },
  { path: 'fabricacion', loadComponent: () => import('./fabricacion-list/fabricacion-list').then(m => m.FabricacionList) },
  { path: 'fabricacion/nuevo', loadComponent: () => import('./fabricacion-nueva/fabricacion-nueva').then(m => m.FabricacionNueva) },
  { path: 'fabricacion/:folioOf', loadComponent: () => import('./fabricacion-form/fabricacion-form').then(m => m.FabricacionForm) },
  { path: 'captura-masiva', loadComponent: () => import('./captura-masiva/captura-masiva').then(m => m.CapturaMasiva) },
  { path: 'incidencias', loadComponent: () => import('./incidencias/incidencias').then(m => m.Incidencias) },
];
