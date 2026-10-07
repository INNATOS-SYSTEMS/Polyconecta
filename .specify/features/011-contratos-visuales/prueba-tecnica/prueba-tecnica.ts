import { Component } from '@angular/core';
import { TablaPrueba } from './tabla-prueba';
import { KanbanPrueba } from './kanban-prueba';
import { CamposPrueba } from './campos-prueba';

@Component({
  selector: 'app-prueba-tecnica',
  imports: [TablaPrueba, KanbanPrueba, CamposPrueba],
  template: `
    <div class="p-3" style="overflow:auto;height:100%">
      <h5>Tabla (TanStack, modo servidor)</h5><app-tabla-prueba />
      <h5 class="mt-4">Kanban (CDK)</h5><app-kanban-prueba />
      <h5 class="mt-4">Campos (Spartan brain)</h5><app-campos-prueba />
    </div>`,
})
export class PruebaTecnica {}
