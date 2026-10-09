import { Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { importe, n2 } from '../../../core/format/numero';
import { nombreProducto } from '../../../core/format/producto';
import { OrigenBusqueda } from '../../../core/lista/origen-busqueda';
import { OrigenEnMemoria } from '../../../core/lista/origen-en-memoria';
import { ProductRef } from '../../../core/models/inventario';
import { OdooDate } from '../../../shared/odoo-date/odoo-date';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { emptyDraft, LineDraft, OdooLineCapture } from '../../../shared/odoo-line-capture/odoo-line-capture';
import { OdooMaestro } from '../../../shared/odoo-maestro/odoo-maestro';
import { OdooMany2one } from '../../../shared/odoo-many2one/odoo-many2one';
import { OdooTabs, PcPestana } from '../../../shared/odoo-tabs/odoo-tabs';
import { AgenteVentaDto, ClienteBusquedaDto, FirmaDetalleDto, PedidosService, ProductoBusquedaDto } from '../pedidos.service';
import { BorradorPedido, MONEDAS_PEDIDO } from './borrador-pedido';

interface Moneda { codigo: string }

const dos = (n: number) => String(n).padStart(2, '0');
/** Como el pedido de la spec 011: 27/04/2026. */
const fechaTexto = (d: Date | null): string => (d ? `${dos(d.getDate())}/${dos(d.getMonth() + 1)}/${d.getFullYear()}` : '—');

/**
 * Hoja del pedido de venta, la misma en el formulario y en "Nuevo" (spec 011, contratos visuales §1.3 a
 * §1.6): maestro en dos columnas, pestañas con la captura arriba de las líneas, y "Procesos requeridos".
 * Con `editable` el maestro y las líneas se editan en su lugar (D-164); sin él, todo es texto.
 */
@Component({
  selector: 'pc-pedido-hoja',
  imports: [FormsModule, OdooMaestro, OdooMany2one, OdooDate, OdooTabs, PcPestana, OdooLineCapture, OdooIcon],
  templateUrl: './pedido-hoja.html',
  styles: ':host { display: block; }',
  // Con cambios sin guardar, un 401 abre el diálogo de sesión en lugar de ir a /login (spec 003, caso límite).
  host: { '[attr.data-captura-pendiente]': 'borrador().sucio()' },
})
export class PedidoHoja {
  protected readonly importe = importe;
  protected readonly n2 = n2;
  protected readonly fechaTexto = fechaTexto;
  /** "Clave - Nombre" (D-141); la API ya puede traer la clave en el nombre. */
  protected readonly producto = (clave: string, nombre: string) => (nombre.startsWith(clave) ? nombre : nombreProducto(clave, nombre));

  readonly borrador = input.required<BorradorPedido>();
  readonly editable = input(false);
  readonly clientes = input<readonly ClienteBusquedaDto[]>([]);
  readonly agentes = input<readonly AgenteVentaDto[]>([]);
  readonly productos = input<readonly ProductoBusquedaDto[]>([]);
  readonly firmas = input<readonly FirmaDetalleDto[]>([]);
  readonly firmasPendientes = input<readonly string[]>([]);
  /** El formulario muestra subtotal, IVA y total; "Nuevo" no (spec 011). */
  readonly conTotales = input(false);

  protected readonly pestanas = [{ id: 'detalle', titulo: 'Detalle' }, { id: 'firmas', titulo: 'Firmas' }];

  private readonly pedidos = inject(PedidosService);

  /** El cliente se busca en el servidor mientras se escribe: cualquier cliente activo, no solo los primeros. */
  protected readonly origenClientes = new OrigenBusqueda<ClienteBusquedaDto>(t => this.pedidos.buscarClientes(t));
  /** Un origen nuevo cuando llega el catálogo: el many2one vuelve a consultar (lee la lista aquí, no en el cierre). */
  protected readonly origenAgentes = computed(() => {
    const lista = this.agentes();
    return new OrigenEnMemoria<AgenteVentaDto>({ datos: () => lista, id: a => String(a.id), buscables: ['clave', 'nombre'] });
  });
  protected readonly origenMonedas = new OrigenEnMemoria<Moneda>({ datos: () => MONEDAS_PEDIDO.map(codigo => ({ codigo })), id: m => m.codigo, buscables: ['codigo'] });

  protected readonly textoCliente = (c: ClienteBusquedaDto) => c.nombre;
  protected readonly textoAgente = (a: AgenteVentaDto) => a.nombre;
  protected readonly textoMoneda = (m: Moneda) => m.codigo;
  protected readonly idPorId = (r: { id: number }) => String(r.id);
  protected readonly idMoneda = (m: Moneda) => m.codigo;
  protected readonly moneda = computed(() => ({ codigo: this.borrador().moneda() }));

  /** Catálogo de la captura: la unidad es la base del producto y no se edita (D-127). */
  protected readonly catalogo = computed<ProductRef[]>(() =>
    this.todosLosProductos().map(p => ({ clave: p.clave, nombre: p.nombre, unidad: p.unidad, clasificacion: 'Bolsa' as const })));

  /** Lo que la búsqueda trajo del servidor, además de lo que se cargó al abrir. */
  private readonly encontrados = signal<ProductoBusquedaDto[]>([]);
  private readonly todosLosProductos = computed(() => {
    const vistos = new Set<number>();
    return [...this.productos(), ...this.encontrados()].filter(p => !vistos.has(p.id) && vistos.add(p.id));
  });
  private esperaBusqueda: ReturnType<typeof setTimeout> | undefined;

  protected buscarProductos(texto: string): void {
    clearTimeout(this.esperaBusqueda);
    const t = texto.split(' — ')[0].trim();
    if (t.length < 2) return;
    this.esperaBusqueda = setTimeout(() => {
      void this.pedidos.buscarProductos(t).then(r => this.encontrados.update(e => [...e, ...r])).catch(() => undefined);
    }, 250);
  }

  protected readonly draft = signal<LineDraft>(emptyDraft());
  protected readonly editandoLinea = signal<number | null>(null);
  protected readonly errorLinea = signal<string | null>(null);

  protected readonly iva = computed(() => this.borrador().subtotal() * 0.16);
  protected readonly total = computed(() => this.borrador().subtotal() + this.iva());

  protected campoTexto(valor: string, cambiar: (v: string) => void): void {
    cambiar(valor);
    this.borrador().marcar();
  }

  protected agregar(d: LineDraft): void {
    this.errorLinea.set(null);
    const prod = this.todosLosProductos().find(p => p.clave === d.clave);
    if (!prod) {
      this.errorLinea.set(`El producto ${d.clave} no está en el catálogo de CONTPAQi.`);
      return;
    }
    const b = this.borrador();
    const i = this.editandoLinea();
    const previa = i !== null ? b.lineas()[i] : null;
    const linea = {
      id: previa?.id ?? null, productoId: prod.id, clave: prod.clave, producto: prod.nombre, unidad: prod.unidad,
      cantidad: d.cantidad, precioUnitario: d.precioUnitario ?? null,
    };
    b.lineas.update(ls => (i !== null ? ls.map((l, k) => (k === i ? linea : l)) : [...ls, linea]));
    b.marcar();
    this.editandoLinea.set(null);
    this.draft.set(emptyDraft());
  }

  protected editarLinea(i: number): void {
    const l = this.borrador().lineas()[i];
    this.editandoLinea.set(i);
    this.draft.set({ clave: l.clave, producto: l.producto, cantidad: l.cantidad, unidad: l.unidad, precioUnitario: l.precioUnitario ?? 0 });
  }

  protected cancelarLinea(): void {
    this.editandoLinea.set(null);
    this.draft.set(emptyDraft());
  }

  protected quitarLinea(i: number): void {
    this.borrador().lineas.update(ls => ls.filter((_, k) => k !== i));
    this.borrador().marcar();
    if (this.editandoLinea() === i) this.cancelarLinea();
  }
}
