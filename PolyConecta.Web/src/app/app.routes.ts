import { Routes } from '@angular/router';
import { CALIDAD_ROUTES } from './features/calidad/calidad.routes';
import { INVENTARIO_ROUTES } from './features/inventario/inventario.routes';
import { LOGISTICA_ROUTES } from './features/logistica/logistica.routes';
import { PLATAFORMA_ROUTES } from './features/plataforma/plataforma.routes';
import { PRODUCCION_ROUTES } from './features/produccion/produccion.routes';
import { VENTAS_ROUTES } from './features/ventas/ventas.routes';
import { PaginaNoEncontrada } from './shared/pagina-no-encontrada/pagina-no-encontrada';

/** Cada módulo es dueño de su archivo de rutas (CT-09). */
export const routes: Routes = [
  ...PLATAFORMA_ROUTES,
  ...VENTAS_ROUTES,
  ...INVENTARIO_ROUTES,
  ...PRODUCCION_ROUTES,
  ...CALIDAD_ROUTES,
  ...LOGISTICA_ROUTES,
  { path: '**', component: PaginaNoEncontrada },
];
