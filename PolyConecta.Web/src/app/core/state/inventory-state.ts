import { Injectable } from '@angular/core';
import { LotBalance, ProductClass, ProductRef, StockQuant } from '../models/inventario';
import { ALMACEN_MATERIA_PRIMA, WIP_PIM, WIP_STC, catalogoSemilla, lotesSemilla } from '../seed/inventario';
import { EstadoBase, contieneSinMayusculas, igualSinMayusculas, suma } from './estado-base';

/** Réplica de Services/InventoryState.cs (FR-006). */
@Injectable({ providedIn: 'root' })
export class InventoryState extends EstadoBase {
  static readonly AlmacenMateriaPrima = ALMACEN_MATERIA_PRIMA;
  static readonly WipPim = WIP_PIM;
  static readonly WipStc = WIP_STC;

  static classLabel(c: ProductClass): string {
    switch (c) {
      case 'Bolsa':
        return 'Bolsas';
      case 'RolloImpreso':
        return 'Rollos impresos';
      case 'RolloLiso':
        return 'Rollos sin imprimir';
      case 'RolloMaestro':
        return 'Rollos maestros';
      case 'MateriaPrima':
        return 'Materia prima';
      default:
        return 'Scrap';
    }
  }

  /** Ubicaciones excluidas del material vendible (SPEC-007 FR-011). */
  static esVendible(ubicacion: string): boolean {
    return !contieneSinMayusculas(ubicacion, 'Cuarentena') && !contieneSinMayusculas(ubicacion, 'Scrap');
  }

  readonly catalogo: ProductRef[] = catalogoSemilla();
  readonly lotes: LotBalance[] = lotesSemilla();

  getProducto(clave: string): ProductRef | undefined {
    return this.catalogo.find(p => igualSinMayusculas(p.clave, clave));
  }

  get ubicaciones(): string[] {
    return [...new Set(this.lotes.map(l => l.ubicacion))].sort(ordinal);
  }

  fisico(clave: string, ubicacion?: string): number {
    return suma(this.lotesDe(clave, ubicacion), l => l.cantidad);
  }

  reservado(clave: string, ubicacion?: string): number {
    return suma(this.lotesDe(clave, ubicacion).filter(l => l.estado === 'Reservado'), l => l.cantidad);
  }

  enWip(clave: string, ubicacion?: string): number {
    return suma(this.lotesDe(clave, ubicacion).filter(l => l.estado === 'EnWip'), l => l.cantidad);
  }

  /** Físico menos todo lo comprometido. Es la cifra que decide si un pedido requiere proceso. */
  disponible(clave: string, ubicacion?: string): number {
    return suma(this.lotesDisponibles(clave, ubicacion), l => l.cantidad);
  }

  lotesDe(clave: string, ubicacion?: string): LotBalance[] {
    return this.lotes.filter(l => igualSinMayusculas(l.clave, clave) && (ubicacion === undefined || l.ubicacion === ubicacion));
  }

  /** Cantidad libre de un lote concreto en una ubicación. Usado para pre-validar una recolección. */
  disponibleDeLote(lote: string, ubicacion: string): number {
    return suma(this.lotes.filter(l => l.lote === lote && l.ubicacion === ubicacion && l.estado === 'Libre'), l => l.cantidad);
  }

  lotesDisponibles(clave: string, ubicacion?: string): LotBalance[] {
    return this.lotesDe(clave, ubicacion).filter(l => l.estado === 'Libre' && InventoryState.esVendible(l.ubicacion));
  }

  /** Existencias del Inventario Actual: un renglón por lote y ubicación. */
  existencias(): StockQuant[] {
    return this.lotes
      .filter(l => l.cantidad > 0)
      .map(l => ({ lote: l, producto: this.getProducto(l.clave) }))
      .filter((x): x is { lote: LotBalance; producto: ProductRef } => x.producto !== undefined)
      .map(x => ({ producto: x.producto, ubicacion: x.lote.ubicacion, lote: x.lote.lote, cantidad: x.lote.cantidad }));
  }

  /**
   * Reserva lógica: el material sigue en su almacén pero deja de estar disponible. Se ejecuta al
   * Autorizar el pedido. Con cantidad menor al lote, el lote se parte y el remanente queda libre.
   */
  reservar(lote: string, documentoFolio: string, cantidad?: number): boolean {
    const l = this.lotes.find(x => x.lote === lote);
    if (!l || l.estado !== 'Libre') return false;
    if (cantidad !== undefined && cantidad > 0 && cantidad < l.cantidad) {
      this.lotes.push({ lote: l.lote, clave: l.clave, ubicacion: l.ubicacion, cantidad: l.cantidad - cantidad, estado: 'Libre' });
      l.cantidad = cantidad;
    }
    l.estado = 'Reservado';
    l.comprometidoPor = documentoFolio;
    this.notify();
    return true;
  }

  liberarReservasDe(documentoFolio: string): void {
    for (const l of this.lotes.filter(x => x.comprometidoPor === documentoFolio && x.estado === 'Reservado')) {
      l.estado = 'Libre';
      l.comprometidoPor = undefined;
    }
    this.notify();
  }

  /** Mueve cantidad de un lote a WIP: reserva física. El material sigue existiendo, ya no está disponible. */
  moverAWip(lote: string, cantidad: number, wip: string, ofFolio: string): boolean {
    const origen = this.lotes.find(l => l.lote === lote && l.estado === 'Libre');
    if (!origen || cantidad <= 0 || cantidad > origen.cantidad) return false;

    origen.cantidad -= cantidad;
    if (origen.cantidad <= 0) this.quitar(origen);

    const destino = this.lotes.find(l => l.lote === lote && l.ubicacion === wip && l.comprometidoPor === ofFolio);
    if (destino) destino.cantidad += cantidad;
    else this.lotes.push({ lote, clave: origen.clave, ubicacion: wip, cantidad, estado: 'EnWip', comprometidoPor: ofFolio });

    this.notify();
    return true;
  }

  /**
   * Devolución WIP → almacén origen. La cantidad la captura el operador: el sistema no asume el
   * saldo teórico porque un rollo a medio consumir no pesa lo que el sistema supone.
   */
  devolverDeWip(lote: string, cantidad: number, wip: string, ofFolio: string, almacenDestino: string): boolean {
    const enWip = this.lotes.find(l => l.lote === lote && l.ubicacion === wip && l.comprometidoPor === ofFolio);
    if (!enWip || cantidad <= 0 || cantidad > enWip.cantidad) return false;

    enWip.cantidad -= cantidad;
    if (enWip.cantidad <= 0) this.quitar(enWip);

    const destino = this.lotes.find(l => l.lote === lote && l.ubicacion === almacenDestino && l.estado === 'Libre');
    if (destino) destino.cantidad += cantidad;
    else this.lotes.push({ lote, clave: enWip.clave, ubicacion: almacenDestino, cantidad, estado: 'Libre' });

    this.notify();
    return true;
  }

  saldoWip(ofFolio: string): LotBalance[] {
    return this.lotes.filter(l => l.estado === 'EnWip' && l.comprometidoPor === ofFolio);
  }

  saldoWipTotal(ofFolio: string): number {
    return suma(this.saldoWip(ofFolio), l => l.cantidad);
  }

  private quitar(l: LotBalance): void {
    this.lotes.splice(this.lotes.indexOf(l), 1);
  }
}

/** OrderBy de LINQ con cadenas usa la cultura; para claves ASCII coincide con el orden ordinal. */
const ordinal = (a: string, b: string): number => (a < b ? -1 : a > b ? 1 : 0);
