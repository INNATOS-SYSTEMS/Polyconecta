import { Component, input } from '@angular/core';

/** Réplica de Components/Forms/OdooStatusPipeline.razor. */
@Component({
  selector: 'pc-odoo-status-pipeline',
  template: `
    <div class="o_statusbar_pipeline">
      @for (stage of stages(); track stage; let i = $index) {
        <div class="arrow-step" [class.active]="stage === currentStage()" [class.done]="stage !== currentStage() && i < stages().indexOf(currentStage())">
          {{ stage }}
        </div>
      }
    </div>
  `,
  styles: ':host { display: contents; }',
})
export class OdooStatusPipeline {
  readonly stages = input<readonly string[]>(['Borrador', 'Confirmada', 'Hecho']);
  readonly currentStage = input('Borrador');
}
