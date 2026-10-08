import { EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { sesionInterceptor } from './sesion.interceptor';

/**
 * Proveedor para habilitar HttpClient configurado con el interceptor de sesión
 * en rutas perezosas (lazy chunks), preservando el presupuesto de carga inicial <= 89 kB.
 */
export function proveerServiciosHttp(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideHttpClient(withInterceptors([sesionInterceptor])),
  ]);
}
