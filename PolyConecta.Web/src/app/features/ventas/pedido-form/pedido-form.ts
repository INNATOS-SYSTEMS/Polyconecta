import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { OdooSmartButtons, SmartButtonModel, botonInteligente } from '../../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { OdooStatusPipeline } from '../../../shared/odoo-status-pipeline/odoo-status-pipeline';
import { ChatterEntry, OdooChatterDrawer } from '../../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { PaginaNoEncontrada } from '../../../shared/pagina-no-encontrada/pagina-no-encontrada';
import { PedidosService, PedidoDetalleDto } from '../pedidos.service';

@Component({
  selector: 'pc-pedido-form',
  imports: [
    CommonModule,
    BotonNuevo,
    OdooBreadcrumb,
    OdooSmartButtons,
    OdooStatusPipeline,
    OdooChatterDrawer,
    OdooIcon,
    PaginaNoEncontrada,
  ],
  templateUrl: './pedido-form.html',
  styles: `
    :host { display: block; }
    .o_form_view {
      padding: 1.5rem 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }
    .o_form_sheet {
      background: white;
      border: 1px solid var(--border-color, #e2e8f0);
      border-radius: 8px;
      padding: 2rem;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }
  `,
})
export class PedidoForm implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly pedidosService = inject(PedidosService);

  protected readonly id = signal<string>(this.route.snapshot.paramMap.get('id') ?? '');
  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly noEncontrado = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly exito = signal<string | null>(null);

  protected readonly pedido = signal<PedidoDetalleDto | null>(null);

  protected readonly stages = ['Borrador', 'Confirmado', 'Autorizado', 'Cancelado'];

  protected readonly subtotalCalculado = computed(() => {
    const p = this.pedido();
    if (!p) return 0;
    return p.lineas.reduce((acc, l) => acc + (l.subtotal ?? (l.cantidad * (l.precioUnitario ?? 0))), 0);
  });

  protected readonly ivaCalculado = computed(() => this.subtotalCalculado() * 0.16);
  protected readonly totalCalculado = computed(() => this.subtotalCalculado() + this.ivaCalculado());

  protected readonly smartButtons = computed<SmartButtonModel[]>(() => {
    const p = this.pedido();
    if (!p) return [];
    return [
      botonInteligente('entrega', 0, '', true),
      botonInteligente('orden', 0, `/produccion/fabricacion?pedido=${p.folio}`, true),
    ];
  });

  protected readonly accionConfirmar = computed(() =>
    this.pedido()?.acciones.find(a => a.accion === 'confirmar')
  );
  protected readonly accionAutorizar = computed(() =>
    this.pedido()?.acciones.find(a => a.accion === 'autorizar')
  );
  protected readonly accionRevocar = computed(() =>
    this.pedido()?.acciones.find(a => a.accion === 'revocar')
  );
  protected readonly accionCancelar = computed(() =>
    this.pedido()?.acciones.find(a => a.accion === 'cancelar')
  );

  protected readonly avisoEdicion = computed(() => {
    const ed = this.pedido()?.acciones.find(a => a.accion === 'editar');
    return ed?.aviso ?? null;
  });

  protected readonly chatterEntries = computed<ChatterEntry[]>(() => {
    const p = this.pedido();
    if (!p) return [];
    const entradas: ChatterEntry[] = [
      { author: 'Sistema', timestamp: p.fechaPedido, text: `Pedido ${p.folio} creado en Borrador.` },
    ];
    for (const f of p.firmas) {
      entradas.push({
        author: 'Sistema',
        timestamp: f.fecha,
        text: `Autorización de ${f.rol} firmada por ${f.usuario}${f.suplente ? ' (suplente)' : ''}.`,
      });
    }
    return entradas;
  });

  async ngOnInit(): Promise<void> {
    await this.cargarPedido();
  }

  protected async cargarPedido(): Promise<void> {
    const paramId = this.id();
    if (!paramId || isNaN(Number(paramId))) {
      this.noEncontrado.set(true);
      this.cargando.set(false);
      return;
    }

    this.cargando.set(true);
    this.error.set(null);
    try {
      const p = await this.pedidosService.obtener(paramId);
      this.pedido.set(p);
    } catch {
      this.noEncontrado.set(true);
    } finally {
      this.cargando.set(false);
    }
  }

  protected async confirmar(): Promise<void> {
    const p = this.pedido();
    if (!p) return;
    this.guardando.set(true);
    this.error.set(null);
    this.exito.set(null);
    try {
      const res = await this.pedidosService.confirmar(p.id, p.rowVersion);
      this.pedido.set(res);
      this.exito.set('Pedido confirmado exitosamente.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al confirmar el pedido.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected async autorizar(rol?: string | null): Promise<void> {
    const p = this.pedido();
    if (!p) return;
    this.guardando.set(true);
    this.error.set(null);
    this.exito.set(null);
    try {
      const res = await this.pedidosService.autorizar(p.id, p.rowVersion, rol);
      this.pedido.set(res);
      this.exito.set('Firma de autorización registrada exitosamente.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al autorizar el pedido.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected async revocar(): Promise<void> {
    const p = this.pedido();
    if (!p) return;
    this.guardando.set(true);
    this.error.set(null);
    this.exito.set(null);
    try {
      const res = await this.pedidosService.revocar(p.id, p.rowVersion, 'Revocación manual');
      this.pedido.set(res);
      this.exito.set('Autorización revocada.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al revocar la autorización.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected async cancelar(): Promise<void> {
    const p = this.pedido();
    if (!p) return;
    this.guardando.set(true);
    this.error.set(null);
    this.exito.set(null);
    try {
      const res = await this.pedidosService.cancelar(p.id, p.rowVersion, 'Cancelación manual');
      this.pedido.set(res);
      this.exito.set('Pedido cancelado.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al cancelar el pedido.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected navegar(ruta: string): void {
    void this.router.navigateByUrl(ruta);
  }
}
