import { Routes } from '@angular/router';

export const PLATAFORMA_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./dashboard/dashboard').then(m => m.Dashboard) },
];
