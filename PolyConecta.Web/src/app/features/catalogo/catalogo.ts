import { fechaCampo } from '../../core/format/numero';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DIALOG_DATA, Dialog, DialogRef } from '@angular/cdk/dialog';
import { firstValueFrom } from 'rxjs';
import { EtapaKanban, TransicionKanban } from '../../core/kanban/kanban';
import { OrigenEnMemoria } from '../../core/lista/origen-en-memoria';
import { SearchView } from '../../core/search/search-view';
import { BotonNuevo } from '../../shared/boton-nuevo/boton-nuevo';
import { OdooActionMenu } from '../../shared/odoo-action-menu/odoo-action-menu';
import { OdooBreadcrumb } from '../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { ChatterEntry, OdooChatterDrawer } from '../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { OdooDate } from '../../shared/odoo-date/odoo-date';
import { AvisosService } from '../../shared/odoo-dialog/avisos';
import { abrirDialogo, OdooConfirmacion, OdooDialog, OdooHardStop } from '../../shared/odoo-dialog/odoo-dialog';
import { ICONOS, OdooIcon } from '../../shared/odoo-icon/odoo-icon';
import { LotPickerModal } from '../../shared/lot-picker-modal/lot-picker-modal';
import { LotQuantityPickerModal } from '../../shared/lot-quantity-picker-modal/lot-quantity-picker-modal';
import { ProductionLot } from '../../core/models/produccion';
import { LotBalance } from '../../core/models/inventario';
import { StockOperationLine } from '../../core/models/operaciones';
import { OdooKanban } from '../../shared/odoo-kanban/odoo-kanban';
import { emptyDraft, LineDraft, OdooLineCapture } from '../../shared/odoo-line-capture/odoo-line-capture';
import { ColumnaLista } from '../../shared/odoo-list/columnas';
import { OdooList } from '../../shared/odoo-list/odoo-list';
import { OdooMany2one } from '../../shared/odoo-many2one/odoo-many2one';
import { OdooNumber } from '../../shared/odoo-number/odoo-number';
import { OdooPager } from '../../shared/odoo-pager/odoo-pager';
import { OdooSearchPanel } from '../../shared/odoo-search-panel/odoo-search-panel';
import { OdooSmartButtons, SmartButtonModel } from '../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { OdooStatusPipeline } from '../../shared/odoo-status-pipeline/odoo-status-pipeline';
import { OdooSyncStatus } from '../../shared/odoo-sync-status/odoo-sync-status';
import { OdooTabs, PcPestana } from '../../shared/odoo-tabs/odoo-tabs';
import { OdooViewSwitcher } from '../../shared/odoo-view-switcher/odoo-view-switcher';
import { PaginaNoEncontrada } from '../../shared/pagina-no-encontrada/pagina-no-encontrada';
import { PaginaPendiente } from '../../shared/pagina-pendiente/pagina-pendiente';
import { PedidoEjemplo, pedidosEjemplo, PRODUCTOS_EJEMPLO, ProductoEjemplo } from './datos';

/** Firma de la transición Confirmado → Autorizado en el kanban de ejemplo: devuelve el nombre de quien firma. */
@Component({
  selector: 'pc-catalogo-firma',
  imports: [OdooDialog],
  template: `
    <pc-odoo-dialog titulo="Autorizar pedido" textoPrimario="Firmar" [primarioDeshabilitado]="!firmante()" (confirmar)="ref.close(firmante())" (cancelar)="ref.close()">
      <p>Firma para autorizar {{ datos.fila.folio }}.</p>
      <label class="form-label small text-muted" for="firmante">Quien autoriza</label>
      <input id="firmante" class="form-control form-control-sm" data-firmante (input)="firmante.set($any($event.target).value)" />
    </pc-odoo-dialog>
  `,
})
class FirmaEjemplo {
  protected readonly ref = inject<DialogRef<string>>(DialogRef);
  protected readonly datos = inject<{ fila: PedidoEjemplo }>(DIALOG_DATA);
  protected readonly firmante = signal('');
}

const VISTA: SearchView<PedidoEjemplo> = {
  campos: [{ etiqueta: 'Folio', valor: p => p.folio }, { etiqueta: 'Cliente', valor: p => p.cliente }],
  filtros: [
    { nombre: 'Borrador', campo: 'Estado', condicion: p => p.estado === 'Borrador' },
    { nombre: 'Hecho', campo: 'Estado', condicion: p => p.estado === 'Hecho' },
    { nombre: 'Más de $30,000', campo: 'Total', condicion: p => p.total > 30000 },
  ],
  agrupaciones: [{ etiqueta: 'Cliente', clave: p => p.cliente }, { etiqueta: 'Estado', clave: p => p.estado }],
};

/**
 * Galería viva (spec 011, E4): cada componente con sus estados y datos de ejemplo. Es la referencia de
 * `docs/diseno/07-contratos-visuales.md` y la prueba de sus comportamientos (`e2e/catalogo/`).
 */
@Component({
  selector: 'pc-catalogo',
  imports: [FormsModule, OdooList, OdooKanban, OdooSearchPanel, OdooMany2one, OdooDate, OdooNumber, OdooTabs, PcPestana,
    OdooActionMenu, OdooSyncStatus, OdooIcon, OdooBreadcrumb, OdooSmartButtons, OdooStatusPipeline, OdooLineCapture,
    OdooChatterDrawer, BotonNuevo, OdooViewSwitcher, OdooPager, PaginaPendiente, PaginaNoEncontrada, LotPickerModal, LotQuantityPickerModal],
  templateUrl: './catalogo.html',
  styles: `
    :host { display: block; }
    .o_catalogo { display: flex; gap: 1.5rem; padding: 1.5rem; }
    .o_catalogo_indice { position: sticky; top: 0; align-self: flex-start; min-width: 190px; font-size: 0.85rem; }
    .o_catalogo_indice a { display: block; padding: 0.2rem 0; text-decoration: none; }
    .o_catalogo_cuerpo { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 2rem; }
    .o_catalogo_seccion { background: white; border: 1px solid var(--border-color); border-radius: 8px; padding: 1.25rem; }
    .o_catalogo_seccion > h2 { font-size: 1.1rem; font-weight: 700; margin-bottom: 0.25rem; }
    .o_catalogo_seccion > p { color: var(--text-muted); font-size: 0.85rem; }
    .o_catalogo_estados { display: grid; grid-template-columns: 110px repeat(4, minmax(0, 1fr)); gap: 0.75rem; align-items: center; font-size: 0.8rem; }
    .o_catalogo_estado { color: var(--text-muted); font-size: 0.75rem; text-transform: uppercase; }
    .o_catalogo_fila { display: flex; flex-wrap: wrap; gap: 1rem; align-items: flex-start; }
    .o_catalogo_campo { width: 280px; }
    .o_catalogo_campo label { display: block; font-size: 0.75rem; color: var(--text-muted); margin-bottom: 0.2rem; }
  `,
})
export class Catalogo {
  private readonly dialog = inject(Dialog);
  protected readonly avisos = inject(AvisosService);

  // --- Lista y kanban ---
  protected readonly pedidos = signal(pedidosEjemplo());
  protected readonly vista = VISTA;
  protected readonly origen = new OrigenEnMemoria<PedidoEjemplo>({ datos: () => this.pedidos(), id: p => p.id, vista: VISTA, sumables: ['total'] });
  protected readonly origenVacio = new OrigenEnMemoria<PedidoEjemplo>({ datos: () => [], id: p => p.id });
  protected readonly idPedido = (p: PedidoEjemplo) => p.id;
  protected readonly etapaPedido = (p: PedidoEjemplo) => p.estado;
  protected readonly fechaCampo = fechaCampo;
  protected readonly columnas: ColumnaLista<PedidoEjemplo>[] = [
    { campo: 'folio', titulo: 'Folio', clase: 'fw-semibold text-primary' },
    { campo: 'cliente', titulo: 'Cliente' },
    { campo: 'fecha', titulo: 'Fecha', tipo: 'fecha' },
    { campo: 'total', titulo: 'Total', tipo: 'moneda', sumable: true },
    { campo: 'estado', titulo: 'Estado', tipo: 'estado' },
  ];
  protected readonly busqueda = signal('');
  protected readonly nombrados = signal<string[]>([]);
  protected readonly agrupaciones = signal<string[]>([]);
  protected readonly etapas: EtapaKanban[] = [
    { valor: 'Borrador', titulo: 'Borrador' }, { valor: 'Confirmado', titulo: 'Confirmado' }, { valor: 'Autorizado', titulo: 'Autorizado' },
    { valor: 'En progreso', titulo: 'En progreso' }, { valor: 'Hecho', titulo: 'Hecho', plegada: true },
  ];
  protected readonly transiciones: TransicionKanban<PedidoEjemplo>[] = [
    { desde: 'Borrador', hacia: 'Confirmado', nombre: 'Confirmar', ejecutar: p => this.cambiarEstado(p, 'Confirmado') },
    { desde: 'Confirmado', hacia: 'Autorizado', nombre: 'Autorizar', dialogo: FirmaEjemplo, ejecutar: p => this.cambiarEstado(p, 'Autorizado') },
  ];
  protected readonly ultimoEvento = signal('');

  private cambiarEstado(p: PedidoEjemplo, estado: string): undefined {
    this.pedidos.update(ps => ps.map(x => (x.id === p.id ? { ...x, estado } : x)));
    return undefined;
  }

  // --- Campos ---
  protected readonly origenProductos = new OrigenEnMemoria<ProductoEjemplo>({ datos: () => PRODUCTOS_EJEMPLO, id: p => p.clave, buscables: ['clave', 'nombre'] });
  protected readonly textoProducto = (p: ProductoEjemplo) => `${p.clave} · ${p.nombre}`;
  protected readonly idProducto = (p: ProductoEjemplo) => p.clave;
  protected producto: ProductoEjemplo | null = null;
  protected readonly productoFijo = PRODUCTOS_EJEMPLO[0];
  protected fecha: Date | null = new Date(2026, 9, 7);
  protected readonly fechaMin = new Date(2026, 9, 1);
  protected readonly fechaMax = new Date(2026, 9, 31);
  protected cantidad: number | null = 5500;
  protected precio: number | null = 5.7;
  protected porcentaje: number | null = 12.5;

  // --- Formulario ---
  protected readonly pestanas = [{ id: 'detalle', titulo: 'Detalle' }, { id: 'otra', titulo: 'Otra información' }];
  protected readonly accionesMenu = [
    { nombre: 'Imprimir', icono: 'imprimir', ejecutar: () => this.avisos.exito('Imprimir') },
    { nombre: 'Duplicar', icono: 'documento', ejecutar: () => this.avisos.exito('Duplicar') },
    { nombre: 'Cancelar documento', icono: 'cancelar', deshabilitada: true, ejecutar: () => undefined },
  ];
  protected readonly migas = [{ label: 'Pedidos', url: '/catalogo' }, { label: 'PV-2026-0001' }];
  protected readonly botonesInteligentes: SmartButtonModel[] = [
    { label: 'Entrega', countBadge: 1, iconClass: 'entrega', targetRoute: '/catalogo' },
    { label: 'Fabricación', countBadge: 3, iconClass: 'fabricacion', targetRoute: '/catalogo' },
    { label: 'Pedido', countBadge: 0, iconClass: 'pedido', targetRoute: '', deshabilitado: true },
  ];
  protected readonly etapasDocumento = ['Borrador', 'Confirmado', 'Autorizado', 'En progreso', 'Hecho'];
  protected readonly catalogoProductos = PRODUCTOS_EJEMPLO.map(p => ({ ...p, clasificacion: 'Bolsa' as const }));
  protected borrador: LineDraft = emptyDraft();
  protected readonly lineas = signal<LineDraft[]>([]);
  protected mensajes: ChatterEntry[] = [{ author: 'Sistema', timestamp: 'Hoy', text: 'Creación del documento.' }];

  protected readonly nombresIconos = Object.keys(ICONOS);
  protected readonly avisarArchivar = (ids: string[]) => this.avisos.aviso(`Archivar ${ids.length} pedidos: lo decide cada fase.`);

  protected quitarLinea(i: number): void {
    this.lineas.update(ls => ls.filter((_, j) => j !== i));
  }

  // --- Selección de lotes ---
  protected readonly verLotes = signal(false);
  protected readonly verCantidades = signal(false);
  protected lotesElegidos: string[] = [];
  protected readonly lotes: ProductionLot[] = [
    { lote: 'R001-BOL-2026-0001', real: 1200, unidad: 'PZA', estado: 'Aprobado' },
    { lote: 'R002-BOL-2026-0001', real: 800, unidad: 'PZA', estado: 'Aprobado' },
    { lote: 'R003-BOL-2026-0001', real: 500, unidad: 'PZA', estado: 'En revisión' },
  ];
  protected readonly saldos: LotBalance[] = [
    { lote: 'L-MP-0001', clave: 'MP0001', ubicacion: 'SC/Stock', cantidad: 450, estado: 'Libre' },
    { lote: 'L-MP-0002', clave: 'MP0001', ubicacion: 'SC/Stock', cantidad: 300, estado: 'Libre' },
  ];
  protected lineaStock: StockOperationLine = { clave: 'MP0001', producto: 'RESINA BAJA DENSIDAD', unidad: 'KG', solicitado: 600, asignaciones: [], entregado: 0 };

  protected agregarLinea(l: LineDraft): void {
    this.lineas.update(ls => [...ls, l]);
    this.borrador = emptyDraft();
  }

  // --- Diálogos y avisos ---
  protected readonly respuesta = signal('');

  async abrirConfirmacion(): Promise<void> {
    const ref = abrirDialogo<boolean>(this.dialog, OdooConfirmacion, { titulo: 'Confirmar pedido', mensaje: '¿Confirmar el pedido PV-2026-0001?', confirmar: 'Confirmar' });
    this.respuesta.set((await firstValueFrom(ref.closed)) ? 'Confirmado' : 'Cancelado');
  }

  async abrirHardStop(): Promise<void> {
    const ref = abrirDialogo<void>(this.dialog, OdooHardStop, { titulo: 'Lote en cuarentena', mensaje: 'El lote R001-BOL-2026-0007 no está liberado por Calidad: no se puede mover (hard-stop).' });
    await firstValueFrom(ref.closed);
  }
}
