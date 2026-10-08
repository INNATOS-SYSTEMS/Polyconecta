import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { etiquetaProducto } from '../../../core/format/producto-etiqueta';
import { Component, computed, inject, input, signal } from '@angular/core';
import { Dialog } from '@angular/cdk/dialog';
import { firstValueFrom } from 'rxjs';
import { abrirDialogo, OdooHardStop } from '../../../shared/odoo-dialog/odoo-dialog';
import { Router } from '@angular/router';
import { fechaCorta, n1 } from '../../../core/format/numero';
import { ShipmentLine } from '../../../core/models/logistica';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { LotPickerModal } from '../../../shared/lot-picker-modal/lot-picker-modal';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { ChatterEntry, OdooChatterDrawer } from '../../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { OdooSmartButtons, botonInteligente } from '../../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { OdooStatusPipeline } from '../../../shared/odoo-status-pipeline/odoo-status-pipeline';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { OdooTabs } from '../../../shared/odoo-tabs/odoo-tabs';
import { LogisticaAcciones, ValidarRecepcion } from './logistica-acciones';
import { InventoryState } from '../../../core/state/inventory-state';
import { ProductionLot } from '../../../core/models/produccion';
import { CONFIG, TipoLogistica } from './tipos';

/**
 * Réplica de Pages/TrasladoFormView, RecepcionFormView y EntregaFormView (.razor) sobre los componentes
 * de la spec 011 (P5 a P7). Validar usa la misma acción que el kanban: en Listo, la recepción confirma
 * lo que entra (D-56) y la entrega se bloquea si hay lotes no liberados por Calidad (hard-stop).
 */
@Component({
  selector: 'pc-logistica-form',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSmartButtons, OdooStatusPipeline, OdooChatterDrawer, OdooTabs, OdooIcon, LotPickerModal],
  templateUrl: './logistica-form.html',
  styles: ':host { display: contents; }',
})
export class LogisticaForm {
  protected readonly producto = (claveONombre?: string | null, nombre?: string) => etiquetaProducto(this.inv.catalogo, claveONombre, nombre);
  protected readonly flow = inject(OperationalFlowState);
  private readonly router = inject(Router);
  protected readonly acciones = inject(LogisticaAcciones);
  private readonly dialog = inject(Dialog);
  private readonly inv = inject(InventoryState);

  readonly tipo = input.required<TipoLogistica>();
  /** Folio con "/" (SC/OUT/31688), resuelto por folioMatcher. */
  readonly folio = input('');

  protected readonly cfg = computed(() => CONFIG[this.tipo()]);
  protected readonly n1 = n1;
  protected readonly pestanas = [{ id: 'operaciones', titulo: 'Operaciones' }];
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
    return this.doc().libre ? botones.map(b => (b.tipo ? botonInteligente(b.tipo, 0, '', true) : { ...b, countBadge: 0, targetRoute: '', deshabilitado: true })) : botones;
  });
  /** El documento libre solo ofrece los lotes que permite su regla (FR-012); la semilla, los del prototipo. */
  protected readonly lotes = computed(() => {
    this.flow.cambios();
    const doc = this.doc();
    return doc.libre ? this.flow.lotesDeDocumentoLibre(doc) : this.cfg().lotes(this.flow);
  });

  /** Lotes de producción que Calidad no ha liberado: el selector explica el hard-stop si se capturan. */
  protected readonly noLiberados = computed(() => {
    this.flow.cambios();
    this.inv.cambios();
    const enRevision = this.flow.manufacturingOrders.flatMap(o => o.produccion).filter(l => l.estado !== 'Aprobado');
    // Un lote rechazado vive en cuarentena del inventario aunque ya no esté en la OF.
    const retenidos = this.inv.lotes
      .filter(l => !InventoryState.esVendible(l.ubicacion))
      .map(l => ({ lote: l.lote, real: l.cantidad, unidad: '', estado: 'Rechazado' as ProductionLot['estado'] }));
    return [...enRevision, ...retenidos];
  });

  protected async validar(): Promise<void> {
    const doc = this.doc();
    const tipo = this.tipo();
    const bloqueados = doc.state === 'Listo' ? this.acciones.noLiberados(tipo, doc) : [];
    if (bloqueados.length > 0) {
      this.acciones.validar(tipo, doc.folio);
      const ref = abrirDialogo<void>(this.dialog, OdooHardStop, { titulo: 'Lote sin liberar', mensaje: this.acciones.motivoHardStop(bloqueados) });
      await firstValueFrom(ref.closed);
      return;
    }
    if (tipo === 'recepcion' && this.acciones.pideConfirmarRecepcion(doc)) {
      const ref = abrirDialogo<boolean>(this.dialog, ValidarRecepcion, { fila: { doc } });
      if (!(await firstValueFrom(ref.closed))) return;
    }
    this.acciones.validar(tipo, doc.folio);
  }

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
