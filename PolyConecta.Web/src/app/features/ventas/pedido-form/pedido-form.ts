import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Dialog } from '@angular/cdk/dialog';
import { firstValueFrom } from 'rxjs';
import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { OdooActionMenu, AccionMenu } from '../../../shared/odoo-action-menu/odoo-action-menu';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooChatterDrawer } from '../../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { OdooDialog } from '../../../shared/odoo-dialog/odoo-dialog';
import { AvisosService } from '../../../shared/odoo-dialog/avisos';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { OdooSmartButtons, SmartButtonModel, botonInteligente } from '../../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { OdooStatusPipeline } from '../../../shared/odoo-status-pipeline/odoo-status-pipeline';
import { EstadoSincronizacion, OdooSyncStatus } from '../../../shared/odoo-sync-status/odoo-sync-status';
import { PaginaNoEncontrada } from '../../../shared/pagina-no-encontrada/pagina-no-encontrada';
import { FirmaPedido } from '../firma-pedido';
import { BorradorPedido } from '../pedido-hoja/borrador-pedido';
import { PedidoHoja } from '../pedido-hoja/pedido-hoja';
import {
  AgenteVentaDto,
  ClienteBusquedaDto,
  ErrorApi,
  PedidoDetalleDto,
  PedidosService,
  ProductoBusquedaDto,
} from '../pedidos.service';

const ETAPAS = ['Borrador', 'Confirmado', 'Autorizado', 'En progreso', 'Hecho'];

/**
 * Formulario del pedido de venta (spec 011 conectada a la API, FR-027): la misma hoja, con edición en su
 * lugar (D-164). Revocar y Cancelar van en el engranaje y piden motivo (FR-025, FR-026); guardar un pedido
 * con firmas avisa que revoca la autorización (D-147).
 */
@Component({
  selector: 'pc-pedido-form',
  imports: [
    BotonNuevo, OdooBreadcrumb, OdooActionMenu, OdooSmartButtons, OdooStatusPipeline, OdooSyncStatus,
    OdooChatterDrawer, OdooIcon, OdooDialog, PaginaNoEncontrada, PedidoHoja,
  ],
  templateUrl: './pedido-form.html',
  styles: ':host { display: block; }',
})
export class PedidoForm implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly pedidos = inject(PedidosService);
  private readonly dialog = inject(Dialog);
  private readonly avisos = inject(AvisosService);

  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly noEncontrado = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly pedido = signal<PedidoDetalleDto | null>(null);
  protected readonly borrador = new BorradorPedido();

  protected readonly clientes = signal<ClienteBusquedaDto[]>([]);
  protected readonly agentes = signal<AgenteVentaDto[]>([]);
  protected readonly productos = signal<ProductoBusquedaDto[]>([]);

  /** Revocar y cancelar exigen el motivo (FR-025, FR-026, D-33): queda en la bitácora. */
  protected readonly dialogoMotivo = signal<'revocar' | 'cancelar' | null>(null);
  protected readonly motivo = signal('');
  protected readonly dialogoD147 = signal(false);

  private accion(nombre: string) {
    return computed(() => this.pedido()?.acciones.find(a => a.accion === nombre));
  }
  protected readonly accionConfirmar = this.accion('confirmar');
  protected readonly accionAutorizar = this.accion('autorizar');
  protected readonly accionRevocar = this.accion('revocar');
  protected readonly accionCancelar = this.accion('cancelar');
  protected readonly accionEditar = this.accion('editar');

  /** Se edita en su lugar mientras el estado lo permite (FR-024, D-164). */
  protected readonly editable = computed(() => !!this.accionEditar()?.disponible);

  protected readonly etapas = computed(() => (this.pedido()?.estado === 'Cancelado' ? [...ETAPAS, 'Cancelado'] : ETAPAS));

  protected readonly sincronizacion = computed<EstadoSincronizacion>(() => {
    const s = this.pedido()?.sincronizacion as { estado?: string } | undefined;
    return (s?.estado as EstadoSincronizacion) ?? 'NoAplica';
  });

  protected readonly accionesMenu = computed<AccionMenu[]>(() => {
    const r = this.accionRevocar(), c = this.accionCancelar();
    const menu: AccionMenu[] = [];
    if (r) menu.push({ nombre: 'Revocar autorización', icono: 'firma', deshabilitada: !r.disponible, ejecutar: () => this.pedirMotivo('revocar') });
    if (c) menu.push({ nombre: 'Cancelar pedido', icono: 'cancelar', deshabilitada: !c.disponible, ejecutar: () => this.pedirMotivo('cancelar') });
    return menu;
  });

  protected readonly smartButtons = computed<SmartButtonModel[]>(() => {
    const p = this.pedido();
    if (!p) return [];
    return [botonInteligente('entrega', 0, '', true), botonInteligente('orden', 0, `/produccion/fabricacion?pedido=${p.folio}`, true)];
  });

  /** La bitácora del pedido se guarda y llega en vivo (R-04, L2-T032). */
  protected readonly documentoChatter = computed(() => {
    const p = this.pedido();
    return p ? { tipo: 'ventas.pedido', id: p.id } : undefined;
  });

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    if (!id || isNaN(Number(id))) {
      this.noEncontrado.set(true);
      this.cargando.set(false);
      return;
    }
    try {
      this.pedido.set(await this.pedidos.obtener(id));
    } catch {
      this.noEncontrado.set(true);
      this.cargando.set(false);
      return;
    }
    this.cargarBorrador();
    this.cargando.set(false);
    if (this.editable()) await this.cargarCatalogos();
  }

  private async cargarCatalogos(): Promise<void> {
    const [clis, agts, prods] = await Promise.all([
      this.pedidos.buscarClientes().catch(() => []),
      this.pedidos.listarAgentes().catch(() => []),
      this.pedidos.buscarProductos().catch(() => []),
    ]);
    this.clientes.set(clis);
    this.agentes.set(agts);
    this.productos.set(prods);
    if (!this.borrador.sucio()) this.cargarBorrador();
  }

  private cargarBorrador(): void {
    const p = this.pedido();
    if (p) this.borrador.cargar(p, this.clientes(), this.agentes());
  }

  private async ejecutar(accion: () => Promise<PedidoDetalleDto>, exito: string): Promise<void> {
    this.guardando.set(true);
    this.error.set(null);
    try {
      this.pedido.set(await accion());
      this.cargarBorrador();
      this.avisos.exito(exito);
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo completar la acción.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected confirmar(): void {
    const p = this.pedido();
    if (p) void this.ejecutar(() => this.pedidos.confirmar(p.id, p.rowVersion), 'Pedido confirmado.');
  }

  protected async autorizar(): Promise<void> {
    const p = this.pedido();
    if (!p) return;
    let rol: string | undefined;
    if (p.rolesPorFirmar.length > 1) {
      const ref = this.dialog.open<string | boolean>(FirmaPedido, { data: { fila: { id: p.id, folio: p.folio } } });
      const resultado = await firstValueFrom(ref.closed);
      if (!resultado) return;
      rol = typeof resultado === 'string' ? resultado : undefined;
    }
    await this.ejecutar(() => this.pedidos.autorizar(p.id, p.rowVersion, rol), 'Firma registrada.');
  }

  protected pedirMotivo(accion: 'revocar' | 'cancelar'): void {
    this.motivo.set('');
    this.dialogoMotivo.set(accion);
  }

  protected async confirmarMotivo(): Promise<void> {
    const p = this.pedido(), accion = this.dialogoMotivo(), motivo = this.motivo().trim();
    if (!p || !accion || !motivo) return;
    this.dialogoMotivo.set(null);
    await (accion === 'revocar'
      ? this.ejecutar(() => this.pedidos.revocar(p.id, p.rowVersion, motivo), 'Autorización revocada.')
      : this.ejecutar(() => this.pedidos.cancelar(p.id, p.rowVersion, motivo), 'Pedido cancelado.'));
  }

  protected guardar(): void {
    if ((this.pedido()?.firmas.length ?? 0) > 0) this.dialogoD147.set(true);
    else void this.ejecutarGuardar(false);
  }

  protected async confirmarD147(): Promise<void> {
    this.dialogoD147.set(false);
    await this.ejecutarGuardar(true);
  }

  private async ejecutarGuardar(revocarAutorizacion: boolean): Promise<void> {
    const p = this.pedido();
    if (!p) return;
    this.guardando.set(true);
    this.error.set(null);
    try {
      this.pedido.set(await this.pedidos.editar(p.id, { ...this.borrador.payload(), rowVersion: p.rowVersion, revocarAutorizacion }));
      this.cargarBorrador();
      this.avisos.exito('Pedido guardado.');
    } catch (e: unknown) {
      if (e instanceof ErrorApi && e.code === 'EDICION_REVOCA_AUTORIZACION') {
        this.dialogoD147.set(true);
        return;
      }
      this.error.set((e as Error).message || 'No se pudo guardar el pedido.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected descartar(): void {
    this.error.set(null);
    this.cargarBorrador();
  }

  protected navegar(ruta: string): void {
    void this.router.navigateByUrl(ruta);
  }
}
