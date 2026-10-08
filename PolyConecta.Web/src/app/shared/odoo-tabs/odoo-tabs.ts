import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, contentChildren, Directive, inject, input, model, TemplateRef } from '@angular/core';
import { BrnTabs, BrnTabsContent, BrnTabsList, BrnTabsTrigger } from '@spartan-ng/brain/tabs';

/** Contenido de una pestaña: `<ng-template pcPestana="detalle">…</ng-template>`. */
@Directive({ selector: 'ng-template[pcPestana]' })
export class PcPestana {
  readonly id = input.required<string>({ alias: 'pcPestana' });
  readonly plantilla = inject(TemplateRef);
}

/**
 * Pestañas de la hoja (spec 011): `BrnTabs` de Spartan (teclado y ARIA) con el marcado de Bootstrap que
 * ya usan los formularios (`nav nav-tabs`), así que se ven igual.
 */
@Component({
  selector: 'pc-odoo-tabs',
  imports: [BrnTabs, BrnTabsList, BrnTabsTrigger, BrnTabsContent, NgTemplateOutlet],
  template: `
    <div class="o_tabs" [brnTabs]="actual()" (brnTabsChange)="cambiar($event)">
      <ul class="nav nav-tabs mb-3" brnTabsList role="tablist">
        @for (p of pestanas(); track p.id) {
          <li class="nav-item"><button type="button" class="nav-link" [class.active]="p.id === actual()" [brnTabsTrigger]="p.id" [attr.data-pestana]="p.id"
              [disabled]="!!p.deshabilitada" [attr.title]="p.deshabilitada || null">{{ p.titulo }}</button></li>
        }
      </ul>
      @for (p of pestanas(); track p.id) {
        <div [brnTabsContent]="p.id">
          @if (p.id === actual()) {
            @for (c of contenidos(); track c.id()) {
              @if (c.id() === p.id) { <ng-container *ngTemplateOutlet="c.plantilla" /> }
            }
          }
        </div>
      }
    </div>
  `,
})
export class OdooTabs {
  /** `deshabilitada` lleva el motivo, que se muestra al pasar el cursor. */
  readonly pestanas = input.required<{ id: string; titulo: string; deshabilitada?: string }[]>();
  readonly activa = model<string | undefined>(undefined);
  protected readonly contenidos = contentChildren(PcPestana);

  /** La activa es la elegida o, si no hay, la primera. */
  protected readonly actual = computed(() => this.activa() ?? this.pestanas()[0]?.id);

  protected cambiar(id: string | undefined): void {
    if (id) this.activa.set(id);
  }
}
