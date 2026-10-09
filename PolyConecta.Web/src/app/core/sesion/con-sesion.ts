import { Routes } from '@angular/router';
import { proveerClienteApi } from './proveedor-api';
import { sesionGuard } from './sesion.guard';

/**
 * Las rutas de un módulo, con sesión (US2 escenario 2): sin ella, `/login?volver=`. Provee además el manejo
 * del `401` del cliente de la API. Va en cada archivo de rutas perezoso, no en `app.routes`, para que la
 * guardia y el cliente no entren a la carga inicial (D-143). Solo `/login` queda sin sesión.
 */
export const conSesion = (rutas: Routes): Routes => [
  { path: '', canActivate: [sesionGuard], providers: [proveerClienteApi()], children: rutas },
];
