import { Component } from '@angular/core';

/** El mismo "no encontrado" de Routes.razor. */
@Component({
  selector: 'pc-pagina-no-encontrada',
  template: `
    <div class="p-4">
      <h4 class="fw-bold">Página no encontrada</h4>
      <p class="text-muted">La ruta solicitada no existe en PolyConecta.</p>
    </div>
  `,
})
export class PaginaNoEncontrada {}
