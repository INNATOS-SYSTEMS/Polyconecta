import { Routes } from '@angular/router';

/** Galería viva de componentes (spec 011, E4). */
export const CATALOGO_ROUTES: Routes = [
  { path: 'catalogo', loadComponent: () => import('./catalogo').then(m => m.Catalogo), title: 'Catálogo de componentes' },
];
