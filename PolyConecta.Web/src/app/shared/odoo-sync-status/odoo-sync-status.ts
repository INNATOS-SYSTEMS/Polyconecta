import { Component, computed, input, output } from '@angular/core';
import { BrnPopover, BrnPopoverContent, BrnPopoverTrigger } from '@spartan-ng/brain/popover';
import { OdooIcon } from '../odoo-icon/odoo-icon';

export type EstadoSincronizacion = 'NoAplica' | 'Pendiente' | 'Enviado' | 'Confirmado' | 'Error';

const PRESENTACION: Record<EstadoSincronizacion, { titulo: string; texto: string; icono: string }> = {
  NoAplica: { titulo: 'No se envía a CONTPAQi', texto: 'Este documento todavía no genera movimientos en CONTPAQi.', icono: 'documento' },
  Pendiente: { titulo: 'Pendiente de enviar', texto: 'El movimiento está en la cola y se enviará a CONTPAQi en cuanto el bridge lo tome.', icono: 'pendiente' },
  Enviado: { titulo: 'Enviado a CONTPAQi', texto: 'CONTPAQi está registrando el movimiento.', icono: 'cargando' },
  Confirmado: { titulo: 'Registrado en CONTPAQi', texto: 'El movimiento quedó registrado en CONTPAQi.', icono: 'sincronizado' },
  Error: { titulo: 'Error al enviar a CONTPAQi', texto: 'CONTPAQi no registró el movimiento.', icono: 'aviso' },
};

/**
 * Estado de sincronización con CONTPAQi (CT-15, contratos visuales §1.8). Solo lo llevan los documentos
 * que envían un comando del contrato bridge-v1, y va arriba del chatter: un ícono por estado y, al
 * pulsarlo, un popover con el título, el detalle (folio o error) y "Reintentar" en error para Sistemas (D-93).
 */
@Component({
  selector: 'pc-odoo-sync-status',
  imports: [OdooIcon, BrnPopover, BrnPopoverTrigger, BrnPopoverContent],
  template: `
    <div brnPopover align="end">
      <button type="button" brnPopoverTrigger [class]="'o_sync_icono o_sync_' + estado()" [attr.data-sync]="estado()"
              [attr.aria-label]="'Sincronización con CONTPAQi: ' + presentacion().titulo" [title]="presentacion().titulo">
        <pc-odoo-icon [nombre]="presentacion().icono" contexto="icono" />
      </button>
      <div *brnPopoverContent class="o_dropdown_panel o_sync_popover" data-sync-popover>
        <div class="o_sync_popover_titulo">{{ presentacion().titulo }}</div>
        <div class="text-muted">{{ presentacion().texto }}</div>
        @if (estado() === 'Confirmado' && (folio() || idErp())) {
          <div class="mt-1" data-sync-folio>@if (folio()) {Folio <strong>{{ folio() }}</strong>}@if (folio() && idErp()) { · }@if (idErp()) {Id {{ idErp() }}}</div>
        }
        @if (estado() === 'Error' && error(); as e) {
          <div class="text-danger mt-1" data-sync-error><strong>{{ e.codigo }}</strong> · {{ e.mensaje }}</div>
        }
        @if (estado() === 'Error' && puedeReintentar()) {
          <button type="button" class="btn btn-sm btn-outline-secondary mt-2" data-sync-reintentar (click)="reintentar.emit()"><pc-odoo-icon nombre="reintentar" />Reintentar</button>
        }
      </div>
    </div>
  `,
  styles: ':host { display: inline-block; }',
})
export class OdooSyncStatus {
  readonly estado = input.required<EstadoSincronizacion>();
  readonly folio = input<string | null>(null);
  readonly idErp = input<string | null>(null);
  readonly error = input<{ codigo: string; mensaje: string } | null>(null);
  readonly puedeReintentar = input(false);
  readonly reintentar = output<void>();
  protected readonly presentacion = computed(() => PRESENTACION[this.estado()]);
}
