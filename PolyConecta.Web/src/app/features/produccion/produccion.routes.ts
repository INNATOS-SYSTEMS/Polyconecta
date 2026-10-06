import { Routes } from '@angular/router';
import { CapturaMasiva } from './captura-masiva/captura-masiva';
import { FabricacionNueva } from './fabricacion-nueva/fabricacion-nueva';
import { FabricacionForm } from './fabricacion-form/fabricacion-form';
import { FabricacionList } from './fabricacion-list/fabricacion-list';
import { Incidencias } from './incidencias/incidencias';

export const PRODUCCION_ROUTES: Routes = [
  { path: 'fabricacion', component: FabricacionList },
  { path: 'fabricacion/nuevo', component: FabricacionNueva },
  { path: 'fabricacion/:folioOf', component: FabricacionForm },
  { path: 'captura-masiva', component: CapturaMasiva },
  { path: 'incidencias', component: Incidencias },
];
