import { Routes } from '@angular/router';
import { CalidadForm } from './calidad-form/calidad-form';
import { CalidadList } from './calidad-list/calidad-list';

export const CALIDAD_ROUTES: Routes = [
  { path: 'calidad', component: CalidadList },
  { path: 'calidad/:folioOf', component: CalidadForm },
];
