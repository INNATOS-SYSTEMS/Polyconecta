import { Component, computed, input, output } from '@angular/core';
import { OdooIcon } from '../odoo-icon/odoo-icon';

export type EstadoSincronizacion = 'NoAplica' | 'Pendiente' | 'Enviado' | 'Confirmado' | 'Error';

const PRESENTACION: Record<EstadoSincronizacion, { texto: string; icono: string }> = {
  NoAplica: { texto: 'No se envía a CONTPAQi', icono: 'documento' },
  Pendiente: { texto: 'Pendiente de enviar a CONTPAQi', icono: 'pendiente' },
  Enviado: { texto: 'Enviado a CONTPAQi', icono: 'cargando' },
  Confirmado: { texto: 'En CONTPAQi', icono: 'sincronizado' },
  Error: { texto: 'Error al enviar a CONTPAQi', icono: 'aviso' },
};

/**
 * Estado de sincronización de un documento con CONTPAQi (CT-15, spec 011). Va junto a las etapas del
 * formulario. "Reintentar" aparece solo en `Error` y solo si `puedeReintentar` (Sistemas, D-93).
 * En la réplica ningún documento sincroniza todavía: F1 lo conecta.
 */
@Component({
  selector: 'pc-odoo-sync-status',
  imports: [OdooIcon],
  template: `
    <span class="o_sync_status" [class]="'o_sync_status o_sync_' + estado()" role="status" [attr.data-sync]="estado()" [title]="detalle()">
      <pc-odoo-icon [nombre]="presentacion().icono" />
      <span>{{ presentacion().texto }}@if (estado() === 'Confirmado' && folio()) { · {{ folio() }}}</span>
    </span>
    @if (estado() === 'Error' && error(); as e) {
      <div class="small text-danger mt-1" data-sync-error><strong>{{ e.codigo }}</strong> · {{ e.mensaje }}</div>
    }
    @if (estado() === 'Error' && puedeReintentar()) {
      <button type="button" class="btn btn-sm btn-outline-secondary mt-1" data-sync-reintentar (click)="reintentar.emit()"><pc-odoo-icon nombre="reintentar" />Reintentar</button>
    }
  `,
  styles: ':host { display: inline-flex; flex-direction: column; align-items: flex-start; }',
})
export class OdooSyncStatus {
  readonly estado = input.required<EstadoSincronizacion>();
  readonly folio = input<string | null>(null);
  readonly idErp = input<string | null>(null);
  readonly error = input<{ codigo: string; mensaje: string } | null>(null);
  readonly puedeReintentar = input(false);
  readonly reintentar = output<void>();
  protected readonly presentacion = computed(() => PRESENTACION[this.estado()]);
  protected readonly detalle = computed(() => [this.folio() && `Folio ${this.folio()}`, this.idErp() && `Id ${this.idErp()}`].filter(Boolean).join(' · '));
}
