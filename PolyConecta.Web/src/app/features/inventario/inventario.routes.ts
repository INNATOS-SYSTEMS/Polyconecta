import { Routes } from '@angular/router';
import { InventarioActual } from './inventario-actual/inventario-actual';

/** `/ventas/inventario` es un alias de la misma pantalla, como en InventarioActualList.razor. */
export const INVENTARIO_ROUTES: Routes = [
  { path: 'inventario', component: InventarioActual },
  { path: 'ventas/inventario', component: InventarioActual },
];
