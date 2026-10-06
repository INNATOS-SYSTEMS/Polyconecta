import { Component, inject, input } from '@angular/core';
import { Router } from '@angular/router';

/**
 * "Nuevo" habilitado (D-59, Principio X): abre el formulario vacío del documento en `<ruta>/nuevo`,
 * sin origen. Mismo marcado que el botón deshabilitado del prototipo; la paridad lo enmascara.
 */
@Component({
  selector: 'pc-boton-nuevo',
  template: `<button class="btn btn-primary btn-sm fw-bold px-3" (click)="abrir()"><i class="bi bi-plus-lg me-1"></i> Nuevo</button>`,
  styles: ':host { display: contents; }',
})
export class BotonNuevo {
  private readonly router = inject(Router);
  /** Ruta de la lista del documento, como `/pedidos`. */
  readonly ruta = input.required<string>();

  protected abrir(): void {
    void this.router.navigateByUrl(`${this.ruta()}/nuevo`);
  }
}
