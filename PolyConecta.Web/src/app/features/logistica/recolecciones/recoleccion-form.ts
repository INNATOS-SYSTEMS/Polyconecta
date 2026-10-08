import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { etiquetaProducto } from '../../../core/format/producto-etiqueta';
import { Component, computed, inject, input, signal } from '@angular/core';
import { Dialog } from '@angular/cdk/dialog';
import { firstValueFrom } from 'rxjs';
import { Router, RouterLink } from '@angular/router';
import { fechaCorta, n1 } from '../../../core/format/numero';
import { LotBalance } from '../../../core/models/inventario';
import { StockOperationLine, declarado } from '../../../core/models/operaciones';
import { InventoryState } from '../../../core/state/inventory-state';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { StockOperationState } from '../../../core/state/stock-operation-state';
import { abrirDialogo } from '../../../shared/odoo-dialog/odoo-dialog';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { OdooTabs } from '../../../shared/odoo-tabs/odoo-tabs';
import { ETAPAS_RECOLECCION, RecoleccionAcciones, ValidarRecoleccion } from '../recoleccion-acciones';
import { LotQuantityPickerModal } from '../../../shared/lot-quantity-picker-modal/lot-quantity-picker-modal';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { ChatterEntry, OdooChatterDrawer } from '../../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { OdooSmartButtons, SmartButtonModel, botonInteligente } from '../../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { OdooStatusPipeline } from '../../../shared/odoo-status-pipeline/odoo-status-pipeline';

/**
 * Réplica de Pages/RecoleccionFormView.razor (/recolecciones/{*Folio}) sobre los componentes de la spec 011 (P3).
 * Validar usa la misma acción que el kanban: si es parcial, primero el diálogo de cantidades (aclaración P3).
 */
@Component({
  selector: 'pc-recoleccion-form',
  imports: [BotonNuevo, OdooBreadcrumb, OdooSmartButtons, OdooStatusPipeline, OdooChatterDrawer, OdooTabs, OdooIcon, LotQuantityPickerModal, RouterLink],
  templateUrl: './recoleccion-form.html',
  styles: ':host { display: contents; }',
})
export class RecoleccionForm {
  protected readonly producto = (claveONombre?: string | null, nombre?: string) => etiquetaProducto(this.inv.catalogo, claveONombre, nombre);
  protected readonly ops = inject(StockOperationState);
  private readonly inv = inject(InventoryState);
  private readonly router = inject(Router);
  private readonly acciones = inject(RecoleccionAcciones);
  private readonly dialog = inject(Dialog);

  readonly folio = input<string | undefined>(undefined);

  protected readonly stages = ETAPAS_RECOLECCION;
  protected readonly pestanas = [{ id: 'operaciones', titulo: 'Operaciones' }];
  protected readonly n1 = n1;
  protected readonly fechaCorta = fechaCorta;
  protected readonly declarado = declarado;
  protected readonly lotModalLine = signal<StockOperationLine | null>(null);
  protected readonly chatterEntries: ChatterEntry[] = [{ author: 'Sistema', timestamp: 'hoy', text: 'Recolección solicitada por Producción.' }];

  private readonly version = computed(() => this.ops.cambios() + this.inv.cambios());

  constructor() {
    // Las recolecciones nacen con las Órdenes de Fabricación: inyectar el flujo garantiza que existan.
    inject(OperationalFlowState);
  }

  /** Devuelve el mismo objeto mutado: sin equal:false no avisaría a sus dependientes. */
  protected readonly op = computed(() => {
    this.version();
    const f = this.folio();
    return f ? this.ops.get(f) : undefined;
  }, { equal: () => false });

  protected readonly lotesElegibles = computed<LotBalance[]>(() => {
    this.version();
    const op = this.op();
    const linea = this.lotModalLine();
    if (!op || !linea) return [];
    return op.tipo.esDevolucion
      ? this.inv.saldoWip(op.ofFolio).filter(l => l.clave === linea.clave)
      : this.inv.lotesDisponibles(linea.clave, op.origen);
  });

  protected readonly smartButtons = computed<SmartButtonModel[]>(() => {
    this.version();
    const op = this.op();
    // FR-014: una operación libre no tiene OF; su botón queda vacío y deshabilitado.
    const sinOf = op !== undefined && op.ofFolio === '';
    const lista: SmartButtonModel[] = [
      sinOf
        ? botonInteligente('orden', 0, '', true)
        : botonInteligente('orden', 1, `/fabricacion/${op?.ofFolio ?? ''}`),
    ];
    const hermanas = op && !sinOf ? this.ops.deOf(op.ofFolio).filter(o => o.folio !== op.folio) : [];
    if (hermanas.length > 0)
      lista.push(botonInteligente('recoleccion', hermanas.length, '/recolecciones'));
    return lista;
  });

  protected etiquetaLotes(l: StockOperationLine): string {
    return l.asignaciones.length > 0 ? `${l.asignaciones.length} lote(s) · ${n1(declarado(l))}` : 'Seleccionar';
  }

  protected navegar(ruta: string): void {
    void this.router.navigateByUrl(ruta);
  }

  protected async validar(): Promise<void> {
    const op = this.op();
    if (!op) return;
    this.lotModalLine.set(null);
    if (this.acciones.esParcial(op) && op.lineas.some(l => declarado(l) > 0)) {
      const ref = abrirDialogo<boolean>(this.dialog, ValidarRecoleccion, { fila: { folio: op.folio } });
      if (!(await firstValueFrom(ref.closed))) return;
    }
    const { backorder } = this.acciones.validar(op.folio);
    if (backorder) this.navegar(`/recolecciones/${backorder.folio}`);
  }

  protected cancelar(): void {
    const op = this.op();
    if (!op) return;
    this.ops.cancelar(op);
    this.navegar('/recolecciones');
  }

  protected agregar(a: { lote: string; cantidad: number }): void {
    this.ops.asignarLote(this.op()!, this.lotModalLine()!, a.lote, a.cantidad);
  }
}
