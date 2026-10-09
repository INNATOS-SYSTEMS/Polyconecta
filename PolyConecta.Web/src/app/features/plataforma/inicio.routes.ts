import { Routes } from '@angular/router';
import { conSesion } from '../../core/sesion/con-sesion';

/** El Inicio también exige sesión (US2 escenario 2); va en su propio archivo perezoso por la carga inicial. */
export const INICIO_ROUTES: Routes = conSesion([
  { path: '', loadComponent: () => import('./dashboard/dashboard').then(m => m.Dashboard) },
]);
