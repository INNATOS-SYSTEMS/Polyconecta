import { Component, computed, input, output, ViewEncapsulation } from '@angular/core';
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
          <div class="mt-1" data-sync-folio>@if (folio()) {Folio <strong>{{ folio() }}</strong>}@if (folio() && idErp()) { · }@if (idErp()) {Contpaq ID <strong>{{ idErp() }}</strong>}</div>
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
  // Estilos aquí y no en app.css, fuera de la carga inicial. Sin encapsulación: el popover se pinta en otra capa.
  encapsulation: ViewEncapsulation.None,
  styles: `
    pc-odoo-sync-status { display: inline-block; }
    .o_sync_icono {
        display: inline-flex; align-items: center; justify-content: center;
        width: 30px; height: 30px; border-radius: 50%; border: 1px solid var(--border-color); background: white; cursor: pointer; padding: 0;
    }
    .o_sync_icono.o_sync_NoAplica { color: var(--text-muted); }
    .o_sync_icono.o_sync_Pendiente, .o_sync_icono.o_sync_Enviado { color: #B54708; border-color: #F5D9B5; background: #FFF8EF; }
    .o_sync_icono.o_sync_Confirmado { color: #16794C; border-color: #BFE3CF; background: #F1FAF5; }
    .o_sync_icono.o_sync_Error { color: #B42318; border-color: #F4C7C3; background: #FEF3F2; }
    .o_sync_popover { padding: 0.75rem 0.9rem; min-width: 260px; max-width: 340px; font-size: 0.85rem; }
    .o_sync_popover_titulo { font-weight: 600; margin-bottom: 0.25rem; }
  `,
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
