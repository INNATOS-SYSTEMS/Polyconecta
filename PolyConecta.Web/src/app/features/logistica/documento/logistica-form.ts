import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { Component, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { fechaCorta, n1 } from '../../../core/format/numero';
import { ShipmentLine } from '../../../core/models/logistica';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { LotPickerModal } from '../../../shared/lot-picker-modal/lot-picker-modal';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { ChatterEntry, OdooChatterDrawer } from '../../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { OdooSmartButtons } from '../../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { OdooStatusPipeline } from '../../../shared/odoo-status-pipeline/odoo-status-pipeline';
import { CONFIG, TipoLogistica } from './tipos';

/** Réplica de Pages/TrasladoFormView, RecepcionFormView y EntregaFormView (.razor). */
@Component({
  selector: 'pc-logistica-form',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSmartButtons, OdooStatusPipeline, OdooChatterDrawer, LotPickerModal],
  templateUrl: './logistica-form.html',
  styles: ':host { display: contents; }',
})
export class LogisticaForm {
  protected readonly flow = inject(OperationalFlowState);
  private readonly router = inject(Router);

  readonly tipo = input.required<TipoLogistica>();
  /** Folio con "/" (SC/OUT/31688), resuelto por folioMatcher. */
  readonly folio = input('');

  protected readonly cfg = computed(() => CONFIG[this.tipo()]);
  protected readonly n1 = n1;
  protected readonly fechaCorta = fechaCorta;
  protected readonly lotModalLine = signal<ShipmentLine | null>(null);

  /** Devuelve el mismo objeto mutado: sin equal:false no avisaría a sus dependientes. */
  protected readonly doc = computed(() => {
    this.flow.cambios();
    return this.cfg().documento(this.flow, this.folio());
  }, { equal: () => false });

  protected readonly chatterEntries = computed<ChatterEntry[]>(() => [
    { author: 'Sistema', timestamp: 'hoy', text: this.doc().libre ? `${this.cfg().tituloForm} creada con Nuevo, sin documento de origen.` : this.cfg().chatter },
  ]);
  protected readonly smartButtons = computed(() => {
    this.flow.cambios();
    // FR-014: un documento libre no tiene origen; sus smart buttons quedan vacíos y deshabilitados.
    const botones = this.cfg().smartButtons(this.flow);
    return this.doc().libre ? botones.map(b => ({ ...b, countBadge: 0, targetRoute: '', deshabilitado: true })) : botones;
  });
  /** El documento libre solo ofrece los lotes que permite su regla (FR-012); la semilla, los del prototipo. */
  protected readonly lotes = computed(() => {
    this.flow.cambios();
    const doc = this.doc();
    return doc.libre ? this.flow.lotesDeDocumentoLibre(doc) : this.cfg().lotes(this.flow);
  });

  protected navegar(ruta: string): void {
    void this.router.navigateByUrl(ruta);
  }

  protected seleccionados(folios: string[]): void {
    const linea = this.lotModalLine();
    if (!linea) return;
    linea.lotesSeleccionados.splice(0, linea.lotesSeleccionados.length, ...folios);
    this.flow.cambios.update(n => n + 1);
  }
}
