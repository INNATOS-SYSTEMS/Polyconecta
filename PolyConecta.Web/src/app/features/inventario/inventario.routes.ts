import { Routes } from '@angular/router';
import { PaginaPendiente } from '../../shared/pagina-pendiente/pagina-pendiente';

/** `/ventas/inventario` es un alias de la misma pantalla, como en InventarioActualList.razor. */
export const INVENTARIO_ROUTES: Routes = [
  { path: 'inventario', component: PaginaPendiente, data: { titulo: 'Inventario Actual' } },
  { path: 'ventas/inventario', component: PaginaPendiente, data: { titulo: 'Inventario Actual' } },
];
