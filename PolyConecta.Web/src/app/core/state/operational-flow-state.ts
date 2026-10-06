import { Injectable, inject } from '@angular/core';
import { n1 } from '../format/numero';
import { Delivery, DocumentoLogistica, InterplantTransfer, Reception, ShipmentLine } from '../models/logistica';
import { BomLine, Incidencia, ManufacturingOrder, ProductionLot, SubProductLine } from '../models/produccion';
import { SalesOrder, SalesOrderLine } from '../models/ventas';
import {
  CLAVE_VENDIDA,
  LISTA_PRECIOS,
  PEDIDO_FOLIO,
  carteraExtrusion,
  entregaSemilla,
  incidenciasSemilla,
  ordenesSemilla,
  pedidoSemilla,
  recepcionSemilla,
  trasladoSemilla,
} from '../seed/flujo';
import { EstadoBase } from './estado-base';
import { aplicarMovimientoLibre, comoLotes, lotesPermitidos } from './libre/movimientos-logistica';
import { InventoryState } from './inventory-state';
import { StockOperationState } from './stock-operation-state';

/**
 * Réplica de Services/OperationalFlowState.cs (FR-006). El pedido, el traslado, la recepción y la
 * entrega pasan a colecciones con su documento semilla (research R-02): las operaciones reciben el
 * folio y, sin él, actúan sobre la semilla, igual que el prototipo.
 */
@Injectable({ providedIn: 'root' })
export class OperationalFlowState extends EstadoBase {
  static readonly PedidoFolio = PEDIDO_FOLIO;
  static readonly ClaveVendida = CLAVE_VENDIDA;
  /** Roles que deben firmar la autorización. Un único botón para ambos. */
  static readonly RolesAutorizadores = ['Comercial', 'Cobranza'] as const;

  private readonly inv = inject(InventoryState);
  private readonly ops = inject(StockOperationState);

  readonly pedidos: SalesOrder[] = [pedidoSemilla()];
  readonly manufacturingOrders: ManufacturingOrder[] = [...ordenesSemilla(), ...carteraExtrusion()];
  readonly traslados: InterplantTransfer[] = [trasladoSemilla()];
  readonly recepciones: Reception[] = [recepcionSemilla()];
  readonly entregas: Delivery[] = [entregaSemilla()];
  readonly incidencias: Incidencia[] = incidenciasSemilla();

  constructor() {
    super();
    // Toda OF con componentes nace con su orden de recolección en Borrador.
    for (const of of this.manufacturingOrders.filter(o => o.componentes.length > 0))
      this.ops.asegurarRecoleccion(of.folio, of.componentes, OperationalFlowState.plantaDe(of));
  }

  private static plantaDe(of: ManufacturingOrder): string {
    return of.processType === 'Extrusion' ? 'PIM' : 'SC';
  }

  // ------------------------------------------------------------------ pedido

  pedido(folio = PEDIDO_FOLIO): SalesOrder {
    return this.pedidos.find(p => p.folio === folio) ?? this.pedidos[0];
  }

  /** Atajos del documento único del prototipo, sobre el pedido semilla. */
  get currentOrderStage(): string {
    return this.pedido().stage;
  }

  get pedidoLineas(): SalesOrderLine[] {
    return this.pedido().lineas;
  }

  get procesos() {
    return this.pedido().procesos;
  }

  precioDe(clave: string): number {
    return clave.trim() !== '' ? (LISTA_PRECIOS[clave.trim()] ?? Object.entries(LISTA_PRECIOS).find(([k]) => k.toLowerCase() === clave.trim().toLowerCase())?.[1] ?? 0) : 0;
  }

  agregarLineaPedido(clave: string, producto: string, cantidad: number, unidad: string, folio = PEDIDO_FOLIO): void {
    this.pedido(folio).lineas.push({
      clave,
      producto: producto.trim() === '' ? clave : producto,
      cantidad,
      unidad,
      precioUnitario: this.precioDe(clave),
    });
    this.notify();
  }

  quitarLineaPedido(linea: SalesOrderLine, folio = PEDIDO_FOLIO): void {
    const lineas = this.pedido(folio).lineas;
    lineas.splice(lineas.indexOf(linea), 1);
    this.notify();
  }

  setOrderStage(stage: string, folio = PEDIDO_FOLIO): void {
    this.pedido(folio).stage = stage;
    this.notify();
  }

  firmasRecogidas(folio = PEDIDO_FOLIO): number {
    return this.pedido(folio).firmas.size;
  }

  firmaPendiente(folio = PEDIDO_FOLIO): string | undefined {
    const firmas = this.pedido(folio).firmas;
    return OperationalFlowState.RolesAutorizadores.find(r => !firmas.has(r));
  }

  puedeFirmar(folio = PEDIDO_FOLIO): boolean {
    return this.pedido(folio).stage === 'Confirmado' && this.firmaPendiente(folio) !== undefined;
  }

  /**
   * Botón único de Autorizar: registra la firma del rol pendiente. Con las dos firmas el pedido
   * pasa a Autorizado. Sin sesión todavía, la firma se atribuye al siguiente rol pendiente.
   */
  autorizar(folio = PEDIDO_FOLIO): void {
    const pedido = this.pedido(folio);
    if (!this.puedeFirmar(folio)) {
      this.notify();
      return;
    }
    const rol = this.firmaPendiente(folio)!;
    pedido.firmas.set(rol, rol === 'Comercial' ? pedido.agente : 'Crédito y Cobranza');
    if (pedido.firmas.size < OperationalFlowState.RolesAutorizadores.length) {
      this.notify();
      return;
    }
    pedido.stage = 'Autorizado';
    this.notify();
  }

  revocarFirmas(folio = PEDIDO_FOLIO): void {
    const pedido = this.pedido(folio);
    pedido.firmas.clear();
    this.inv.liberarReservasDe(pedido.folio);
    if (pedido.stage === 'Autorizado') pedido.stage = 'Confirmado';
    this.notify();
  }

  liberarReservasPedido(folio = PEDIDO_FOLIO): void {
    this.inv.liberarReservasDe(this.pedido(folio).folio);
  }

  // ------------------------------------------------------------------ órdenes de fabricación

  getOrder(folio: string): ManufacturingOrder | undefined {
    return this.manufacturingOrders.find(o => o.folio === folio);
  }

  /** Órdenes de fabricación de un pedido: de 1 a 3 según la especificación del producto. */
  ordenesDePedido(pedidoFolio: string): ManufacturingOrder[] {
    return this.manufacturingOrders.filter(o => o.pedidoFolio === pedidoFolio);
  }

  getSecondaries(folio: string): ManufacturingOrder[] {
    return this.manufacturingOrders.filter(o => o.originFolio === folio);
  }

  getLotesDisponiblesTraslado(): ProductionLot[] {
    return this.getOrder('EXT-2026-0001')?.produccion.filter(l => l.estado === 'Aprobado') ?? [];
  }

  getLotesDisponiblesEntrega(): ProductionLot[] {
    return this.getOrder('BOL-2026-0001')?.produccion.filter(l => l.estado === 'Aprobado') ?? [];
  }

  agregarComponente(folio: string, clave: string, producto: string, cantidad: number, unidad: string): void {
    const of = this.getOrder(folio);
    if (!of) return;
    of.componentes.push({ clave, producto, cantidad, unidad });
    this.ops.asegurarRecoleccion(folio, of.componentes, OperationalFlowState.plantaDe(of));
    this.notify();
  }

  agregarSubproducto(folio: string, clave: string, producto: string, cantidad: number, unidad: string): void {
    const of = this.getOrder(folio);
    if (!of) return;
    of.subproductos.push({ clave, producto, cantidad, unidad, producido: false, almacenDestino: of.almacenFalla });
    this.notify();
  }

  quitarSubproducto(folio: string, line: SubProductLine): void {
    const lista = this.getOrder(folio)?.subproductos;
    if (lista) lista.splice(lista.indexOf(line), 1);
    this.notify();
  }

  /** Alta manual de un lote producido. El lote va en el primer campo de la captura. */
  agregarLoteProduccion(folio: string, lote: string, real: number, unidad: string): void {
    const of = this.getOrder(folio);
    if (!of) return;
    of.produccion.push({
      lote,
      real,
      unidad: unidad.trim() === '' ? of.unidad : unidad,
      estado: of.calidadRequerida ? 'En revisión' : 'Aprobado',
    });
    of.sequenceCounter = Math.max(of.sequenceCounter, of.produccion.length);
    this.notify();
  }

  quitarLoteProduccion(folio: string, lote: ProductionLot): void {
    const lista = this.getOrder(folio)?.produccion;
    if (lista) lista.splice(lista.indexOf(lote), 1);
    this.notify();
  }

  quitarComponente(folio: string, line: BomLine): void {
    const of = this.getOrder(folio);
    if (!of) return;
    of.componentes.splice(of.componentes.indexOf(line), 1);
    this.ops.asegurarRecoleccion(folio, of.componentes, OperationalFlowState.plantaDe(of));
    this.notify();
  }

  planear(folio: string): void {
    const of = this.getOrder(folio);
    if (!of || of.componentes.length === 0) return; // guard: requiere componentes
    of.state = 'Planeado';
    // Confirmar la OF libera su recolección a Almacén.
    this.ops.confirmarRecoleccion(folio);
    this.notify();
  }

  agregarPlaneacion(
    folio: string, centroTrabajo: string, producto: string, cantidad: number, unidad: string,
    horasAsignadas: number, inicio: Date, fin: Date, operador: string,
  ): void {
    const of = this.getOrder(folio);
    if (!of) return;
    of.planeacion.push({ centroTrabajo, producto, cantidad, unidad, horasAsignadas, fechaInicio: inicio, fechaFin: fin, operador });
    if (of.state === 'Planeado') of.state = 'En progreso';
    const pedido = this.pedido();
    if (pedido.stage === 'Autorizado') pedido.stage = 'En progreso';
    this.notify();
  }

  registrarPesajeRollo(folio: string, pesoBruto: number, tara: number): void {
    const of = this.getOrder(folio);
    if (!of) return;
    of.sequenceCounter++;
    of.produccion.push({ lote: `R${String(of.sequenceCounter).padStart(3, '0')}-${PEDIDO_FOLIO}`, real: pesoBruto - tara, unidad: 'KGS', estado: 'En revisión' });
    this.notify();
  }

  aprobarLote(lote: ProductionLot): void {
    lote.estado = 'Aprobado';
    this.notify();
  }

  rechazarLote(lote: ProductionLot): void {
    lote.estado = 'Rechazado';
    if (!lote.lote.endsWith('.S')) lote.lote += '.S';
    this.notify();
  }

  cerrarProduccion(folio: string): void {
    const of = this.getOrder(folio);
    if (!of) return;
    if (of.calidadRequerida && of.produccion.some(l => l.estado === 'En revisión')) return; // hard-stop: falta calidad
    of.state = 'Hecho';
    if (this.manufacturingOrders.every(o => o.state === 'Hecho')) this.pedido().stage = 'Hecho';
    this.notify();
  }

  // ------------------------------------------------------------------ logística

  traslado(folio?: string): InterplantTransfer {
    return this.traslados.find(t => t.folio === folio) ?? this.traslados[0];
  }

  recepcion(folio?: string): Reception {
    return this.recepciones.find(r => r.folio === folio) ?? this.recepciones[0];
  }

  entrega(folio?: string): Delivery {
    return this.entregas.find(e => e.folio === folio) ?? this.entregas[0];
  }

  comprobarDisponibilidadTraslado(): void {
    this.notify();
  }

  comprobarDisponibilidadRecepcion(): void {
    this.notify();
  }

  comprobarDisponibilidadEntrega(): void {
    this.notify();
  }

  validarTraslado(folio?: string): void {
    const doc = this.traslado(folio);
    this.validarDocumento(doc, 4, 3, doc.libre ? this.lotesDeDocumentoLibre(doc) : this.getLotesDisponiblesTraslado(), 'Traslado');
  }

  validarRecepcion(folio?: string): void {
    const doc = this.recepcion(folio);
    this.validarDocumento(doc, 3, 2, doc.libre ? this.lotesDeDocumentoLibre(doc) : this.getLotesDisponiblesTraslado(), 'Recepción');
  }

  validarEntrega(folio?: string): void {
    const doc = this.entrega(folio);
    this.validarDocumento(doc, 3, 2, doc.libre ? this.lotesDeDocumentoLibre(doc) : this.getLotesDisponiblesEntrega(), 'Entrega');
  }

  /** Lotes que puede tomar un documento libre según su regla (FR-012); la semilla usa los del prototipo. */
  lotesDeDocumentoLibre(doc: DocumentoLogistica): ProductionLot[] {
    return comoLotes(lotesPermitidos(doc, this.inv), this.inv);
  }

  private validarDocumento(doc: DocumentoLogistica, hecho: number, cierre: number, pool: ProductionLot[], nombre: string): void {
    if (doc.step >= hecho) {
      this.notify();
      return;
    }
    if (doc.step === cierre) {
      const r = OperationalFlowState.cerrarSalida(doc.lineas, pool, nombre);
      doc.warning = r.warning;
      doc.error = r.error;
      if (!r.ok) {
        // 0 capturado: la salida de inventario no ocurrió, no se avanza la etapa.
        this.notify();
        return;
      }
    }
    doc.step++;
    if (doc.libre && doc.step >= hecho) aplicarMovimientoLibre(doc, this.inv);
    this.notify();
  }

  /**
   * Cierra la operación de inventario de un documento de logística: compara lo capturado (suma de
   * lotes seleccionados) contra la demanda. Sin nada capturado rechaza el cierre; si es parcial lo
   * permite con una advertencia.
   */
  private static cerrarSalida(lineas: ShipmentLine[], pool: ProductionLot[], nombre: string): { ok: boolean; warning?: string; error?: string } {
    const realDe = (l: ShipmentLine) => l.lotesSeleccionados.reduce((t, f) => t + (pool.find(x => x.lote === f)?.real ?? 0), 0);
    let real = 0;
    let demanda = 0;
    for (const l of lineas) {
      real += realDe(l);
      demanda += l.demanda;
    }
    if (real === 0)
      return { ok: false, error: `No se puede validar: no hay lotes capturados. Selecciona al menos un lote antes de cerrar el ${nombre.toLowerCase()}.` };
    for (const l of lineas) l.entregado = realDe(l);
    return { ok: true, warning: real < demanda ? `${nombre} parcial: ${n1(real)} de ${n1(demanda)} ${lineas[0]?.unidad ?? ''}.` : undefined };
  }

  resetAll(): void {
    this.pedido().stage = 'Borrador';
    for (const of of this.manufacturingOrders) {
      of.state = 'Borrador';
      of.componentes.length = 0;
      of.subproductos.forEach(s => (s.producido = false));
      of.produccion.length = 0;
      of.planeacion.length = 0;
      of.sequenceCounter = 0;
    }
    for (const doc of [this.traslado(), this.recepcion(), this.entrega()]) {
      doc.step = 0;
      doc.warning = undefined;
      doc.error = undefined;
      doc.lineas.forEach(l => {
        l.entregado = 0;
        l.lotesSeleccionados.length = 0;
      });
    }
    this.notify();
  }
}
