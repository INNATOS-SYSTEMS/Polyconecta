import { Injectable, inject } from '@angular/core';
import { n1 } from '../format/numero';
import { LotAllocation, OperationType, StockOperation, StockOperationLine, declarado, pendiente } from '../models/operaciones';
import { BomLine } from '../models/produccion';
import { RECOLECCION_PIM, RECOLECCION_STC, SECUENCIA_INICIAL, tiposSemilla } from '../seed/operaciones';
import { EstadoBase, suma } from './estado-base';
import { InventoryState } from './inventory-state';

/** Réplica de Services/StockOperationState.cs (FR-006). */
@Injectable({ providedIn: 'root' })
export class StockOperationState extends EstadoBase {
  static readonly RecoleccionPim = RECOLECCION_PIM;
  static readonly RecoleccionStc = RECOLECCION_STC;

  private readonly inv = inject(InventoryState);

  readonly tipos: OperationType[] = tiposSemilla();
  readonly operaciones: StockOperation[] = [];
  private secuencia = SECUENCIA_INICIAL;

  getTipo(codigo: string): OperationType {
    return this.tipos.find(t => t.codigo === codigo) ?? this.tipos[0];
  }

  /** Folio consecutivo de la planta. Público para los documentos libres, que comparten la secuencia. */
  nuevoFolio(planta: string, salida: boolean): string {
    return `${planta}/${salida ? 'OUT' : 'IN'}/${++this.secuencia}`;
  }

  get(folio: string): StockOperation | undefined {
    return this.operaciones.find(o => o.folio === folio);
  }

  deOf(ofFolio: string): StockOperation[] {
    return this.operaciones.filter(o => o.ofFolio === ofFolio);
  }

  tieneRecoleccion(ofFolio: string): boolean {
    return this.operaciones.some(o => o.ofFolio === ofFolio && !o.tipo.esDevolucion);
  }

  /**
   * La recolección nace de la mano de la Orden de Fabricación, en Borrador. Mientras la OF esté en
   * borrador sus líneas siguen a los componentes. No mueve inventario: eso ocurre al validarla.
   */
  asegurarRecoleccion(ofFolio: string, componentes: readonly BomLine[], planta = 'PIM'): StockOperation {
    const tipo = this.getTipo(planta === 'PIM' ? RECOLECCION_PIM : RECOLECCION_STC);
    const lineas: StockOperationLine[] = componentes.map(c => ({
      clave: c.clave,
      producto: c.producto,
      unidad: c.unidad,
      solicitado: c.cantidad,
      asignaciones: [],
      entregado: 0,
    }));

    const existente = this.operaciones.find(o => o.ofFolio === ofFolio && !o.tipo.esDevolucion && o.backorderDe === undefined);
    if (existente) {
      // Solo en Borrador: una vez confirmada, la solicitud ya no sigue a la BoM.
      if (existente.step === 0) {
        existente.lineas = lineas;
        this.notify();
      }
      return existente;
    }

    const op = new StockOperation({
      folio: this.nuevoFolio(planta, true),
      tipo,
      ofFolio,
      origen: tipo.origen,
      destino: tipo.destino,
      step: 0,
      lineas,
    });
    this.operaciones.push(op);
    this.notify();
    return op;
  }

  /** El Planner confirma la OF: su recolección se libera a Almacén. */
  confirmarRecoleccion(ofFolio: string): void {
    for (const op of this.operaciones.filter(o => o.ofFolio === ofFolio && o.step === 0)) op.step = 1;
    this.notify();
  }

  /** Devolución del saldo de WIP al almacén. La cantidad la captura el operador. */
  emitirDevolucion(ofFolio: string, planta = 'PIM'): StockOperation {
    const tipo = this.getTipo(planta === 'PIM' ? 'PIM-REC-RET' : 'STC-REC-RET');
    const porClave = new Map<string, number>();
    for (const l of this.inv.saldoWip(ofFolio)) porClave.set(l.clave, (porClave.get(l.clave) ?? 0) + l.cantidad);
    const op = new StockOperation({
      folio: this.nuevoFolio(planta, false),
      tipo,
      ofFolio,
      origen: tipo.origen,
      destino: tipo.destino,
      step: 1,
      lineas: [...porClave].map(([clave, cantidad]) => ({
        clave,
        producto: this.inv.getProducto(clave)?.nombre ?? clave,
        unidad: this.inv.getProducto(clave)?.unidad ?? 'KGS',
        solicitado: cantidad,
        asignaciones: [],
        entregado: 0,
      })),
    });
    this.operaciones.push(op);
    this.notify();
    return op;
  }

  asignarLote(op: StockOperation, linea: StockOperationLine, lote: string, cantidad: number): void {
    if (cantidad <= 0) return;
    const existente = linea.asignaciones.find(a => a.lote === lote);
    if (existente) existente.cantidad = cantidad;
    else linea.asignaciones.push({ lote, cantidad });
    StockOperationState.recalcularEstado(op);
    this.notify();
  }

  quitarLote(op: StockOperation, linea: StockOperationLine, alloc: LotAllocation): void {
    linea.asignaciones.splice(linea.asignaciones.indexOf(alloc), 1);
    StockOperationState.recalcularEstado(op);
    this.notify();
  }

  private static recalcularEstado(op: StockOperation): void {
    if (op.step >= 3) return;
    op.step = op.lineas.some(l => declarado(l) > 0) ? 2 : 1;
  }

  /** Comprueba si el almacén puede cubrir lo pendiente. No mueve nada: solo informa. */
  comprobarDisponibilidad(op: StockOperation): void {
    op.error = undefined;
    op.warning = undefined;
    const faltantes: string[] = [];
    for (const linea of op.lineas.filter(l => pendiente(l) > 0)) {
      const disponible = op.tipo.esDevolucion
        ? suma(this.inv.saldoWip(op.ofFolio).filter(l => l.clave === linea.clave), l => l.cantidad)
        : this.inv.disponible(linea.clave, op.origen);
      if (disponible < pendiente(linea)) faltantes.push(`${linea.clave}: hay ${n1(disponible)} de ${n1(pendiente(linea))} ${linea.unidad}`);
    }
    op.warning =
      faltantes.length === 0
        ? `Disponibilidad completa en ${op.origen}.`
        : 'Disponibilidad parcial — ' + faltantes.join(' · ') + '. Puede validar por parcialidades y dejar backorder.';
    this.notify();
  }

  /**
   * Almacén valida lo que realmente sale. Lo no cubierto genera un backorder ligado a la misma OF.
   * Devuelve el backorder (o undefined) y el error, como el parámetro out del prototipo.
   */
  validar(op: StockOperation): { backorder?: StockOperation; error?: string } {
    op.error = undefined;
    op.warning = undefined;
    const fallar = (error: string) => {
      op.error = error;
      this.notify();
      return { error };
    };

    if (op.step >= 3) return fallar('El documento ya fue validado.');
    if (op.lineas.every(l => declarado(l) <= 0)) return fallar('Declare al menos un lote antes de validar.');
    for (const linea of op.lineas) {
      if (declarado(linea) > pendiente(linea))
        return fallar(`${linea.clave}: lo declarado (${n1(declarado(linea))}) excede lo pendiente (${n1(pendiente(linea))}).`);
    }

    // Pre-chequeo de todas las asignaciones antes de mover nada: una validación que falla a media
    // aplicación dejaría parte del material movido con el documento todavía en Borrador.
    const demandaPorLote = new Map<string, number>();
    for (const a of op.lineas.flatMap(l => l.asignaciones)) demandaPorLote.set(a.lote, (demandaPorLote.get(a.lote) ?? 0) + a.cantidad);
    for (const [lote, cantidad] of demandaPorLote) {
      const disponible = op.tipo.esDevolucion
        ? suma(this.inv.saldoWip(op.ofFolio).filter(l => l.lote === lote), l => l.cantidad)
        : this.inv.disponibleDeLote(lote, op.origen);
      if (cantidad > disponible) return fallar(`Lote ${lote}: se declararon ${n1(cantidad)} pero solo hay ${n1(disponible)} en ${op.origen}.`);
    }

    for (const linea of op.lineas) {
      for (const a of linea.asignaciones) {
        const ok = op.tipo.esDevolucion
          ? this.inv.devolverDeWip(a.lote, a.cantidad, op.origen, op.ofFolio, op.destino)
          : this.inv.moverAWip(a.lote, a.cantidad, op.destino, op.ofFolio);
        if (!ok) return fallar(`Lote ${a.lote}: el movimiento fue rechazado por el inventario.`);
      }
      linea.entregado += declarado(linea);
      linea.asignaciones.length = 0;
    }

    op.step = 3;
    op.contpaqId = `TR-${op.folio.split('/').at(-1)}`;

    // Parcialidad: el remanente viaja a un backorder, no se pierde.
    let backorder: StockOperation | undefined;
    const pendientes = op.lineas.filter(l => pendiente(l) > 0);
    if (pendientes.length > 0) {
      const planta = op.origen.toUpperCase().startsWith('PIM') ? 'PIM' : 'SC';
      backorder = new StockOperation({
        folio: this.nuevoFolio(planta, !op.tipo.esDevolucion),
        tipo: op.tipo,
        ofFolio: op.ofFolio,
        origen: op.origen,
        destino: op.destino,
        backorderDe: op.folio,
        libre: op.libre,
        // El remanente de una recolección ya liberada nace liberado.
        step: 1,
        lineas: pendientes.map(l => ({ clave: l.clave, producto: l.producto, unidad: l.unidad, solicitado: pendiente(l), asignaciones: [], entregado: 0 })),
      });
      this.operaciones.push(backorder);
      op.warning = `Entrega parcial. Se generó el backorder ${backorder.folio} por el remanente.`;
    }
    this.notify();
    return { backorder };
  }

  cancelar(op: StockOperation): void {
    if (op.step >= 3) return;
    this.operaciones.splice(this.operaciones.indexOf(op), 1);
    this.notify();
  }

  /**
   * El cierre técnico se bloquea mientras quede saldo sin declarar en WIP. No hay arrastre a otra
   * OF: el sobrante vuelve a MP y se vuelve a recolectar.
   */
  puedeCerrarOf(ofFolio: string): { ok: boolean; motivo?: string } {
    const saldo = this.inv.saldoWipTotal(ofFolio);
    if (saldo > 0) return { ok: false, motivo: `Quedan ${n1(saldo)} en WIP sin declarar. Devuelva a almacén o declare como scrap antes de cerrar.` };
    const abiertos = this.operaciones.filter(o => o.ofFolio === ofFolio && o.step < 3);
    if (abiertos.length > 0)
      return { ok: false, motivo: `Hay ${abiertos.length} documento(s) de recolección sin validar: ${abiertos.map(a => a.folio).join(', ')}.` };
    return { ok: true };
  }

  totalRecolectado(ofFolio: string): number {
    return suma(this.operaciones.filter(o => o.ofFolio === ofFolio && !o.tipo.esDevolucion).flatMap(o => o.lineas), l => l.entregado);
  }

  totalDevuelto(ofFolio: string): number {
    return suma(this.operaciones.filter(o => o.ofFolio === ofFolio && o.tipo.esDevolucion).flatMap(o => o.lineas), l => l.entregado);
  }
}
