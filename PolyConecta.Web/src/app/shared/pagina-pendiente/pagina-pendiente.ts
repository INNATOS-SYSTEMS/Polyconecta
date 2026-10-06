import { Component, input } from '@angular/core';

/**
 * Página vacía de un módulo mientras se construyen sus pantallas: las réplicas de la spec 001 y
 * las de cada fase sobre la API la sustituyen ruta por ruta en el archivo de rutas de su módulo.
 */
@Component({
  selector: 'pc-pagina-pendiente',
  template: `
    <div class="p-4">
      <h4 class="fw-bold">{{ titulo() }}</h4>
      <p class="text-muted">Pantalla en construcción.</p>
    </div>
  `,
})
export class PaginaPendiente {
  /** Llega por el `data` de la ruta (withComponentInputBinding). */
  readonly titulo = input('');
}
