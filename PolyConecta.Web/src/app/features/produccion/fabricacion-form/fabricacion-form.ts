import { Component, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { fechaCorta, fechaHora, n1 } from '../../../core/format/numero';
import { BomLine, PlanningLine, ProductionLot, SubProductLine } from '../../../core/models/produccion';
import { InventoryState } from '../../../core/state/inventory-state';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { StockOperationState } from '../../../core/state/stock-operation-state';
import { Crumb, OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { ChatterEntry, OdooChatterDrawer } from '../../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { LineDraft, OdooLineCapture, emptyDraft } from '../../../shared/odoo-line-capture/odoo-line-capture';
import { OdooSmartButtons, SmartButtonModel } from '../../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { OdooStatusPipeline } from '../../../shared/odoo-status-pipeline/odoo-status-pipeline';

type Tab = 'componentes' | 'subproductos' | 'produccion' | 'planeacion';

/** Réplica de Pages/FabricacionFormView.razor. */
@Component({
  selector: 'pc-fabricacion-form',
  imports: [OdooBreadcrumb, OdooSmartButtons, OdooStatusPipeline, OdooLineCapture, OdooChatterDrawer],
  templateUrl: './fabricacion-form.html',
  styles: ':host { display: contents; }',
})
export class FabricacionForm {
  protected readonly flow = inject(OperationalFlowState);
  protected readonly inv = inject(InventoryState);
  private readonly ops = inject(StockOperationState);
  private readonly router = inject(Router);

  readonly folioOf = input('');
  /** Contexto de navegación: de qué lista se llegó aquí (?pedido=). */
  readonly pedido = input<string | undefined>(undefined);

  protected readonly stages = ['Borrador', 'Planeado', 'En progreso', 'Hecho'];
  protected readonly almacenMp = InventoryState.AlmacenMateriaPrima;
  protected readonly n1 = n1;
  protected readonly fechaCorta = fechaCorta;
  protected readonly fechaHora = fechaHora;

  protected readonly tab = signal<Tab>('componentes');
  protected readonly editingComponente = signal<BomLine | null>(null);
  protected readonly editingPlaneacion = signal<PlanningLine | null>(null);
  protected readonly draftComponente = signal<LineDraft>(emptyDraft());
  protected readonly draftSubproducto = signal<LineDraft>(emptyDraft());
  protected readonly draftProduccion = signal<LineDraft>(emptyDraft());

  protected readonly chatterEntries: ChatterEntry[] = [{ author: 'Sistema', timestamp: 'hoy', text: 'Orden de Fabricación generada.' }];

  private readonly version = computed(() => this.flow.cambios() + this.inv.cambios() + this.ops.cambios());

  /** Devuelve el mismo objeto mutado: sin equal:false no avisaría a sus dependientes. */
  protected readonly of = computed(() => {
    this.version();
    return this.flow.getOrder(this.folioOf());
  }, { equal: () => false });

  protected readonly breadcrumb = computed<Crumb[]>(() => {
    const p = this.pedido();
    return !p
      ? [{ label: 'Órdenes de Fabricación', url: '/fabricacion' }, { label: this.folioOf() }]
      : [
          { label: 'Pedidos', url: '/pedidos' },
          { label: p, url: `/pedidos/${p}` },
          { label: 'Órdenes de Fabricación', url: `/fabricacion?pedido=${p}` },
          { label: this.folioOf() },
        ];
  });

  protected readonly puedeCerrar = computed(() => {
    const of = this.of();
    return (
      of !== undefined &&
      (!of.calidadRequerida || of.produccion.every(l => l.estado !== 'En revisión')) &&
      of.produccion.length > 0 &&
      this.ops.puedeCerrarOf(of.folio).ok
    );
  });

  protected readonly smartButtons = computed<SmartButtonModel[]>(() => {
    this.version();
    const folio = this.folioOf();
    // Primarias y secundarias llegan al pedido directamente; la jerarquía se navega en la lista.
    const list: SmartButtonModel[] = [{ label: 'Pedido', countBadge: 1, iconClass: 'bi bi-cart-check', targetRoute: `/pedidos/${this.of()?.pedidoFolio}` }];
    // El traslado interplanta cuelga de la orden que tiene secundarias (la que genera el envío).
    if (this.flow.getSecondaries(folio).length > 0)
      list.push({ label: 'Traslado', countBadge: 1, iconClass: 'bi bi-truck', targetRoute: `/traslados/${this.flow.traslado().folio}` });
    const recolecciones = this.ops.deOf(folio);
    if (recolecciones.length > 0)
      list.push({ label: 'Recolección', countBadge: recolecciones.length, iconClass: 'bi bi-box-arrow-right', targetRoute: `/recolecciones/${recolecciones[0].folio}` });
    list.push({ label: 'Calidad', countBadge: 1, iconClass: 'bi bi-shield-check', targetRoute: `/calidad/${folio}` });
    return list;
  });

  protected existencia(clave: string): number {
    this.version();
    return this.inv.disponible(clave, InventoryState.AlmacenMateriaPrima);
  }

  protected goToTab(tab: Tab): void {
    this.tab.set(tab);
    this.editingComponente.set(null);
    this.editingPlaneacion.set(null);
  }

  protected toggleCalidadRequerida(event: Event): void {
    const of = this.of();
    if (of) of.calidadRequerida = (event.target as HTMLInputElement).checked;
    this.flow.cambios.update(n => n + 1);
  }

  protected volverALista(): void {
    const p = this.pedido();
    void this.router.navigateByUrl(!p ? '/fabricacion' : `/fabricacion?pedido=${p}`);
  }

  protected navegar(ruta: string): void {
    void this.router.navigateByUrl(ruta);
  }

  protected guardarComponente(d: LineDraft): void {
    const editando = this.editingComponente();
    const producto = d.producto.trim() === '' ? d.clave : d.producto;
    if (editando) {
      Object.assign(editando, { clave: d.clave, producto, cantidad: d.cantidad, unidad: d.unidad });
      this.editingComponente.set(null);
      this.flow.cambios.update(n => n + 1);
    } else {
      this.flow.agregarComponente(this.folioOf(), d.clave, producto, d.cantidad, d.unidad);
    }
    this.draftComponente.set(emptyDraft());
  }

  protected editarComponente(line: BomLine): void {
    this.editingComponente.set(line);
    this.draftComponente.set({ clave: line.clave, producto: line.producto, cantidad: line.cantidad, unidad: line.unidad });
  }

  protected cancelarEdicionComponente(): void {
    this.editingComponente.set(null);
    this.draftComponente.set(emptyDraft());
  }

  protected editarSubproducto(sp: SubProductLine): void {
    this.draftSubproducto.set({ clave: sp.clave, producto: sp.producto, cantidad: sp.cantidad, unidad: sp.unidad });
    this.flow.quitarSubproducto(this.folioOf(), sp);
  }

  protected guardarSubproducto(d: LineDraft): void {
    this.flow.agregarSubproducto(this.folioOf(), d.clave, d.producto.trim() === '' ? d.clave : d.producto, d.cantidad, d.unidad);
    this.draftSubproducto.set(emptyDraft());
  }

  protected toggleProducido(sp: SubProductLine, event: Event): void {
    sp.producido = (event.target as HTMLInputElement).checked;
    this.flow.cambios.update(n => n + 1);
  }

  protected guardarProduccion(d: LineDraft): void {
    this.flow.agregarLoteProduccion(this.folioOf(), d.clave, d.cantidad, d.unidad);
    this.draftProduccion.set(emptyDraft());
  }

  protected claseEstado(lote: ProductionLot): string {
    return lote.estado === 'Aprobado' ? 'bg-success' : lote.estado === 'Rechazado' ? 'bg-danger' : 'bg-secondary';
  }
}
