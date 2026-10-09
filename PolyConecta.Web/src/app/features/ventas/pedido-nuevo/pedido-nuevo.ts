import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { HojaNueva } from '../../../shared/hoja-nueva/hoja-nueva';
import { BorradorPedido } from '../pedido-hoja/borrador-pedido';
import { PedidoHoja } from '../pedido-hoja/pedido-hoja';
import { AgenteVentaDto, ClienteBusquedaDto, PedidosService, ProductoBusquedaDto } from '../pedidos.service';

/**
 * Pedido nuevo en modo libre (spec 011, contratos visuales §1.4): la misma hoja del formulario, en
 * Borrador y sin origen. El agente que no se elija lo propone la API con el del usuario (D-153).
 */
@Component({
  selector: 'pc-pedido-nuevo',
  imports: [HojaNueva, PedidoHoja],
  template: `
    <pc-hoja-nueva lista="Pedidos" ruta="/ventas/pedidos" titulo="Pedido" [stages]="etapas" [error]="error() ?? undefined" [conChatter]="true"
                   (guardar)="guardar()" (descartar)="descartar()">
      <pc-pedido-hoja [borrador]="borrador" [editable]="true" [clientes]="clientes()" [agentes]="agentes()" [productos]="productos()" />
    </pc-hoja-nueva>
  `,
})
export class PedidoNuevo implements OnInit {
  private readonly router = inject(Router);
  private readonly pedidos = inject(PedidosService);

  protected readonly etapas = ['Borrador', 'Confirmado', 'Autorizado', 'En progreso', 'Hecho'];
  protected readonly borrador = new BorradorPedido();
  protected readonly error = signal<string | null>(null);
  protected readonly guardando = signal(false);
  protected readonly clientes = signal<ClienteBusquedaDto[]>([]);
  protected readonly agentes = signal<AgenteVentaDto[]>([]);
  protected readonly productos = signal<ProductoBusquedaDto[]>([]);

  async ngOnInit(): Promise<void> {
    const [clis, agts, prods] = await Promise.all([
      this.pedidos.buscarClientes().catch(() => []),
      this.pedidos.listarAgentes().catch(() => []),
      this.pedidos.buscarProductos().catch(() => []),
    ]);
    this.clientes.set(clis);
    this.agentes.set(agts);
    this.productos.set(prods);
  }

  protected async guardar(): Promise<void> {
    if (this.guardando()) return;
    if (!this.borrador.cliente()) {
      this.error.set('Elige el cliente.');
      return;
    }
    this.guardando.set(true);
    this.error.set(null);
    try {
      const creado = await this.pedidos.crear(this.borrador.payload());
      void this.router.navigateByUrl(`/ventas/pedidos/${creado.id}`);
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo guardar el pedido.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected descartar(): void {
    void this.router.navigateByUrl('/ventas/pedidos');
  }
}
