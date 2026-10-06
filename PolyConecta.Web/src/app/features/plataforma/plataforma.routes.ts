import { Routes } from '@angular/router';
import { Dashboard } from './dashboard/dashboard';

export const PLATAFORMA_ROUTES: Routes = [
  { path: '', pathMatch: 'full', component: Dashboard },
];
