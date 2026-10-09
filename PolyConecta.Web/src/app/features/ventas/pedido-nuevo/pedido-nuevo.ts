import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { SesionState } from '../../../core/sesion/sesion-state';
import {
  AgenteVentaDto,
  ClienteBusquedaDto,
  DatosPedidoInputDto,
  DomicilioEnvioDto,
  ErrorApi,
  LineaPedidoInputDto,
  PedidosService,
  ProductoBusquedaDto,
} from '../pedidos.service';

export interface LineaTemporal {
  productoId: number;
  clave: string;
  nombre: string;
  unidad: string;
  cantidad: number;
  precioUnitario: number | null;
  subtotal: number;
}

@Component({
  selector: 'pc-pedido-nuevo',
  imports: [CommonModule, FormsModule, OdooBreadcrumb, OdooIcon],
  templateUrl: './pedido-nuevo.html',
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
export class PedidoNuevo implements OnInit {
  protected readonly router = inject(Router);
  private readonly pedidosService = inject(PedidosService);
  private readonly sesion = inject(SesionState);

  protected readonly guardando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly erroresCampos = signal<Record<string, string>>({});

  // Catálogos
  protected readonly clientes = signal<ClienteBusquedaDto[]>([]);
  protected readonly productos = signal<ProductoBusquedaDto[]>([]);
  protected readonly agentes = signal<AgenteVentaDto[]>([]);

  // Campos maestro
  protected readonly clienteId = signal<number | null>(null);
  protected readonly clienteSeleccionado = signal<ClienteBusquedaDto | null>(null);
  protected readonly ordenCompra = signal('');
  protected readonly agenteId = signal<number | null>(null);
  protected readonly fechaPedido = signal(new Date().toISOString().substring(0, 10));
  protected readonly fechaPromesa = signal('');
  protected readonly moneda = signal('MXN');
  protected readonly tipoCambio = signal<number | null>(1);
  protected readonly domiciliosDisponibles = signal<DomicilioEnvioDto[]>([]);
  protected readonly domicilioEntregaId = signal<number | null>(null);

  // Líneas
  protected readonly lineas = signal<LineaTemporal[]>([]);

  // Captura línea actual
  protected readonly productoIdSeleccionado = signal<number | null>(null);
  protected readonly productoSeleccionado = signal<ProductoBusquedaDto | null>(null);
  protected readonly cantidadLinea = signal<number | null>(null);
  protected readonly precioUnitarioLinea = signal<number | null>(null);

  // Totales
  protected readonly subtotal = computed(() =>
    this.lineas().reduce((acc, l) => acc + l.subtotal, 0)
  );
  protected readonly iva = computed(() => this.subtotal() * 0.16);
  protected readonly total = computed(() => this.subtotal() + this.iva());

  async ngOnInit(): Promise<void> {
    try {
      const [clis, prods, agts] = await Promise.all([
        this.pedidosService.buscarClientes().catch(() => []),
        this.pedidosService.buscarProductos().catch(() => []),
        this.pedidosService.listarAgentes().catch(() => []),
      ]);
      this.clientes.set(clis);
      this.productos.set(prods);
      this.agentes.set(agts);

      // Proponer agente del usuario autenticado si existe
      const user = this.sesion.usuario();
      if (user && (user as any).agenteId) {
        this.agenteId.set((user as any).agenteId);
      } else if (agts.length > 0) {
        // Buscar por coincidencia de usuario/nombre
        const coincidente = agts.find(a =>
          a.nombre.toLowerCase().includes(user.nombre.toLowerCase()) ||
          user.nombre.toLowerCase().includes(a.nombre.toLowerCase())
        );
        if (coincidente) this.agenteId.set(coincidente.id);
      }
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al cargar catálogos.');
    }
  }

  protected alCambiarCliente(val: unknown): void {
    const id = val ? Number(val) : null;
    this.clienteId.set(id);
    const cli = this.clientes().find(c => c.id === id) ?? null;
    this.clienteSeleccionado.set(cli);

    if (cli) {
      // Proponer moneda del cliente
      const m = cli.moneda || 'MXN';
      this.moneda.set(m);
      if (m === 'MXN') {
        this.tipoCambio.set(1);
      } else if (this.tipoCambio() === 1 || !this.tipoCambio()) {
        this.tipoCambio.set(18.5); // Sugerencia inicial para USD
      }

      // Proponer domicilios de envío
      const doms = cli.domiciliosEnvio ?? [];
      this.domiciliosDisponibles.set(doms);
      if (doms.length === 1) {
        this.domicilioEntregaId.set(doms[0].id);
      } else {
        this.domicilioEntregaId.set(null);
      }
    } else {
      this.domiciliosDisponibles.set([]);
      this.domicilioEntregaId.set(null);
    }
  }

  protected alCambiarMoneda(m: string): void {
    this.moneda.set(m);
    if (m === 'MXN') {
      this.tipoCambio.set(1);
    } else if (this.tipoCambio() === 1 || !this.tipoCambio()) {
      this.tipoCambio.set(18.5);
    }
  }

  protected alCambiarProducto(val: unknown): void {
    const id = val ? Number(val) : null;
    this.productoIdSeleccionado.set(id);
    const prod = this.productos().find(p => p.id === id) ?? null;
    this.productoSeleccionado.set(prod);
  }

  protected agregarLinea(): void {
    this.error.set(null);
    const prod = this.productoSeleccionado();
    const cant = this.cantidadLinea();
    const precio = this.precioUnitarioLinea();

    if (!prod) {
      this.error.set('Seleccione un producto.');
      return;
    }
    if (!cant || cant <= 0) {
      this.error.set('La cantidad debe ser mayor a 0.');
      return;
    }

    const precioFinal = precio !== null && precio !== undefined && precio > 0 ? precio : null;
    const nueva: LineaTemporal = {
      productoId: prod.id,
      clave: prod.clave,
      nombre: prod.nombre,
      unidad: prod.unidad,
      cantidad: cant,
      precioUnitario: precioFinal,
      subtotal: cant * (precioFinal ?? 0),
    };

    this.lineas.update(ls => [...ls, nueva]);

    // Limpiar campos de captura de línea
    this.productoIdSeleccionado.set(null);
    this.productoSeleccionado.set(null);
    this.cantidadLinea.set(null);
    this.precioUnitarioLinea.set(null);
  }

  protected quitarLinea(index: number): void {
    this.lineas.update(ls => ls.filter((_, i) => i !== index));
  }

  protected async guardar(): Promise<void> {
    this.guardando.set(true);
    this.error.set(null);
    this.erroresCampos.set({});

    const cid = this.clienteId();
    if (!cid) {
      this.error.set('Seleccione un cliente.');
      this.guardando.set(false);
      return;
    }

    if (this.lineas().length === 0) {
      this.error.set('Agregue al menos una línea al pedido.');
      this.guardando.set(false);
      return;
    }

    const payload: DatosPedidoInputDto = {
      clienteId: cid,
      ordenCompraCliente: this.ordenCompra().trim() || null,
      agenteId: this.agenteId(),
      fechaPedido: this.fechaPedido() || null,
      fechaPromesa: this.fechaPromesa() || null,
      domicilioEntregaId: this.domicilioEntregaId(),
      moneda: this.moneda(),
      tipoCambio: this.moneda() === 'MXN' ? 1 : this.tipoCambio(),
      lineas: this.lineas().map(l => ({
        productoId: l.productoId,
        cantidad: l.cantidad,
        precioUnitario: l.precioUnitario,
        metaProduccionKg: null,
        toleranciaPorcentaje: null,
      })),
    };

    try {
      const res = await this.pedidosService.crear(payload);
      void this.router.navigateByUrl(`/ventas/pedidos/${res.id}`);
    } catch (e: unknown) {
      if (e instanceof ErrorApi) {
        this.error.set(e.message);
        if (e.errores) {
          const mapa: Record<string, string> = {};
          for (const err of e.errores) {
            mapa[err.campo] = err.mensaje;
          }
          this.erroresCampos.set(mapa);
        }
      } else {
        this.error.set((e as Error).message || 'Error al guardar el pedido.');
      }
    } finally {
      this.guardando.set(false);
    }
  }

  protected volver(): void {
    void this.router.navigateByUrl('/ventas/pedidos');
  }
}
