import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { importe, n2 } from '../../../core/format/numero';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Dialog } from '@angular/cdk/dialog';
import { firstValueFrom } from 'rxjs';
import { BotonNuevo } from '../../../shared/boton-nuevo/boton-nuevo';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { OdooSmartButtons, SmartButtonModel, botonInteligente } from '../../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { OdooStatusPipeline } from '../../../shared/odoo-status-pipeline/odoo-status-pipeline';
import { ChatterEntry, OdooChatterDrawer } from '../../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { PaginaNoEncontrada } from '../../../shared/pagina-no-encontrada/pagina-no-encontrada';
import { OdooDialog } from '../../../shared/odoo-dialog/odoo-dialog';
import { FirmaPedido } from '../firma-pedido';
import {
  PedidosService,
  PedidoDetalleDto,
  EditarPedidoInputDto,
  AgenteVentaDto,
  ErrorApi,
} from '../pedidos.service';

export interface LineaEditable {
  productoId: number;
  clave: string;
  producto: string;
  unidad: string;
  cantidad: number;
  precioUnitario: number | null;
}

@Component({
  selector: 'pc-pedido-form',
  imports: [
    FormsModule,
    BotonNuevo,
    OdooBreadcrumb,
    OdooSmartButtons,
    OdooStatusPipeline,
    OdooChatterDrawer,
    OdooIcon,
    OdooDialog,
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
  protected readonly importe = importe;
  protected readonly n2 = n2;
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly pedidosService = inject(PedidosService);
  private readonly dialog = inject(Dialog);

  protected readonly id = signal<string>(this.route.snapshot.paramMap.get('id') ?? '');
  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly noEncontrado = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly exito = signal<string | null>(null);
  protected readonly erroresCampos = signal<Record<string, string>>({});

  protected readonly pedido = signal<PedidoDetalleDto | null>(null);

  // Edición y D-147
  protected readonly editando = signal(false);
  protected readonly mostrarDialogoD147 = signal(false);
  protected readonly ordenCompraEditada = signal('');
  protected readonly fechaPromesaEditada = signal('');
  protected readonly domicilioEntregaIdEditado = signal<number | null>(null);
  protected readonly monedaEditada = signal('MXN');
  protected readonly tipoCambioEditado = signal<number | null>(1);
  protected readonly agenteIdEditado = signal<number | null>(null);
  protected readonly lineasEditables = signal<LineaEditable[]>([]);
  protected readonly domiciliosDisponibles = signal<Array<{ id: number; texto: string }>>([]);
  protected readonly agentes = signal<AgenteVentaDto[]>([]);

  protected readonly stages = ['Borrador', 'Confirmado', 'Autorizado', 'Cancelado'];

  protected readonly subtotalCalculado = computed(() => {
    if (this.editando()) {
      return this.lineasEditables().reduce((acc, l) => acc + (l.cantidad * (l.precioUnitario ?? 0)), 0);
    }
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
  protected readonly accionEditar = computed(() =>
    this.pedido()?.acciones.find(a => a.accion === 'editar')
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

    if (!rol && p.rolesPorFirmar.length > 1) {
      const ref = this.dialog.open<string | boolean>(FirmaPedido, {
        data: { fila: { id: p.id, folio: p.folio } },
      });
      const resultado = await firstValueFrom(ref.closed);
      if (!resultado) return;
      rol = typeof resultado === 'string' ? resultado : undefined;
    }

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

  // --- Modo edición y D-147 ---

  protected async iniciarEdicion(): Promise<void> {
    const p = this.pedido();
    if (!p) return;
    this.ordenCompraEditada.set(p.ordenCompraCliente ?? '');
    this.fechaPromesaEditada.set(p.fechaPromesa ?? '');
    this.domicilioEntregaIdEditado.set(p.domicilioEntrega?.id ?? null);
    this.monedaEditada.set(p.moneda);
    this.tipoCambioEditado.set(p.tipoCambio ?? 1);
    this.agenteIdEditado.set(p.agente?.id ?? null);
    this.lineasEditables.set(p.lineas.map(l => ({
      productoId: l.productoId,
      clave: l.clave,
      producto: l.producto,
      unidad: l.unidad,
      cantidad: l.cantidad,
      precioUnitario: l.precioUnitario,
    })));
    if (p.domicilioEntrega) {
      this.domiciliosDisponibles.set([{ id: p.domicilioEntrega.id, texto: p.domicilioEntrega.texto }]);
    } else {
      this.domiciliosDisponibles.set([]);
    }
    void this.pedidosService.buscarClientes(p.cliente.clave).then(clis => {
      const cli = clis.find(c => c.id === p.cliente.id) ?? clis[0];
      if (cli?.domiciliosEnvio?.length) {
        this.domiciliosDisponibles.set(cli.domiciliosEnvio);
      }
    }).catch(() => {});
    this.erroresCampos.set({});
    this.error.set(null);
    this.exito.set(null);
    this.editando.set(true);

    if (this.agentes().length === 0) {
      try {
        const ags = await this.pedidosService.listarAgentes();
        this.agentes.set(ags);
      } catch {
        // Ignorar
      }
    }
  }

  protected cancelarEdicion(): void {
    this.editando.set(false);
    this.error.set(null);
    this.erroresCampos.set({});
  }

  protected intentarGuardarEdicion(): void {
    const p = this.pedido();
    if (!p) return;
    if (p.firmas.length > 0) {
      this.mostrarDialogoD147.set(true);
    } else {
      void this.ejecutarGuardarEdicion(false);
    }
  }

  protected async confirmarD147(): Promise<void> {
    this.mostrarDialogoD147.set(false);
    await this.ejecutarGuardarEdicion(true);
  }

  protected cancelarD147(): void {
    this.mostrarDialogoD147.set(false);
  }

  protected async ejecutarGuardarEdicion(revocarAutorizacion: boolean): Promise<void> {
    const p = this.pedido();
    if (!p) return;
    this.guardando.set(true);
    this.error.set(null);
    this.erroresCampos.set({});

    const payload: EditarPedidoInputDto = {
      rowVersion: p.rowVersion,
      revocarAutorizacion,
      clienteId: p.cliente.id,
      ordenCompraCliente: this.ordenCompraEditada().trim() || null,
      agenteId: this.agenteIdEditado(),
      fechaPedido: p.fechaPedido,
      fechaPromesa: this.fechaPromesaEditada() || null,
      domicilioEntregaId: this.domicilioEntregaIdEditado(),
      moneda: this.monedaEditada(),
      tipoCambio: this.monedaEditada() === 'MXN' ? 1 : this.tipoCambioEditado(),
      lineas: this.lineasEditables().map(l => ({
        productoId: l.productoId,
        cantidad: l.cantidad,
        precioUnitario: l.precioUnitario,
        metaProduccionKg: null,
        toleranciaPorcentaje: null,
      })),
    };

    try {
      const res = await this.pedidosService.editar(p.id, payload);
      this.pedido.set(res);
      this.editando.set(false);
      this.exito.set('Pedido actualizado exitosamente.');
    } catch (e: unknown) {
      if (e instanceof ErrorApi) {
        if (e.code === 'EDICION_REVOCA_AUTORIZACION') {
          this.mostrarDialogoD147.set(true);
          return;
        }
        this.error.set(e.message);
        if (e.errores) {
          const mapa: Record<string, string> = {};
          for (const err of e.errores) {
            mapa[err.campo] = err.mensaje;
          }
          this.erroresCampos.set(mapa);
        }
      } else {
        this.error.set((e as Error).message || 'Error al actualizar el pedido.');
      }
    } finally {
      this.guardando.set(false);
    }
  }

  protected actualizarCantidadLinea(index: number, valor: string | number): void {
    const cant = Number(valor) || 0;
    this.lineasEditables.update(ls =>
      ls.map((l, i) => (i === index ? { ...l, cantidad: cant } : l))
    );
  }

  protected actualizarPrecioLinea(index: number, valor: string | number | null): void {
    const precio = valor !== null && valor !== '' && !isNaN(Number(valor)) ? Number(valor) : null;
    this.lineasEditables.update(ls =>
      ls.map((l, i) => (i === index ? { ...l, precioUnitario: precio } : l))
    );
  }

  protected alCambiarMonedaEdicion(m: string): void {
    this.monedaEditada.set(m);
    if (m === 'MXN') {
      this.tipoCambioEditado.set(1);
    } else if (this.tipoCambioEditado() === 1 || !this.tipoCambioEditado()) {
      this.tipoCambioEditado.set(18.5);
    }
  }

  protected navegar(ruta: string): void {
    void this.router.navigateByUrl(ruta);
  }
}
