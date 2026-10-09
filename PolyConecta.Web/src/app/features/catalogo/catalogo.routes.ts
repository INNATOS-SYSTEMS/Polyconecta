import { Routes } from '@angular/router';
import { conSesion } from '../../core/sesion/con-sesion';

/** Galería viva de componentes (spec 011, E4). */
export const CATALOGO_ROUTES: Routes = conSesion([
  { path: '', loadComponent: () => import('./catalogo').then(m => m.Catalogo), title: 'Catálogo de componentes' },
]);
