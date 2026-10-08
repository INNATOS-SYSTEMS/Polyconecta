import { Injectable, inject } from '@angular/core';
import { TransicionKanban } from '../../core/kanban/kanban';
import { ordenCultural } from '../../core/format/numero';
import { ConsultaLista, OrigenDeLista, ResultadoLista } from '../../core/lista/origen';
import { OrigenEnMemoria } from '../../core/lista/origen-en-memoria';
import { ManufacturingOrder } from '../../core/models/produccion';
import { adaptarVista, SearchView } from '../../core/search/search-view';
import { FABRICACION } from '../../core/search/views';
import { OperationalFlowState } from '../../core/state/operational-flow-state';
import { StockOperationState } from '../../core/state/stock-operation-state';

export const ETAPAS_OF = ['Borrador', 'Planeado', 'En progreso', 'Hecho'];

/** Fila de la lista de OF: la orden con su lugar en la cadena (raíz, hija, nieta). */
export interface FilaOf {
  id: string;
  of: ManufacturingOrder;
  folio: string;
  producto: string;
  proceso: string;
  cantidad: number;
  unidad: string;
  estado: string;
  pedido: string;
  /** Profundidad en la cadena de OF; 0 en la raíz o si se ordena por una columna. */
  nivel: number;
  /** Raíz con OF hijas visibles: lleva la insignia "maestra". */
  maestra: boolean;
  /** Folio de la orden maestra de su cadena (la raíz sin origen): la agrupación "Orden maestra". */
  raiz: string;
}

/** Orden maestra de una OF: sube por `originFolio` hasta la que no tiene origen. */
export function raizDe(of: ManufacturingOrder, ordenes: readonly ManufacturingOrder[]): string {
  let actual = of;
  const vistos = new Set<string>();
  while (actual.originFolio !== undefined && !vistos.has(actual.folio)) {
    vistos.add(actual.folio);
    const origen = ordenes.find(o => o.folio === actual.originFolio);
    if (!origen) break;
    actual = origen;
  }
  return actual.folio;
}

/**
 * Origen de la lista de OF (spec 011, P2): filtra con la vista FABRICACION, como la barra de búsqueda, y
 * sin orden elegido devuelve las OF en orden de cadena (cada hija debajo de su origen), como el prototipo.
 * Con un orden elegido, o agrupando, la lista es plana. La jerarquía se ve agrupando por "Orden maestra"
 * (revisión del líder, D-142): la lista ya no sangra ni pone flecha a las hijas.
 */
export class OrigenOf implements OrigenDeLista<FilaOf> {
  consultas = 0;
  private readonly base: OrigenEnMemoria<FilaOf>;
  /** La vista FABRICACION sobre las filas, más la agrupación "Orden maestra". */
  readonly vista: SearchView<FilaOf>;

  constructor(private readonly ordenes: () => ManufacturingOrder[]) {
    const vista = adaptarVista(FABRICACION, (f: FilaOf) => f.of);
    this.vista = { ...vista, agrupaciones: [...vista.agrupaciones, { etiqueta: 'Orden maestra', clave: f => f.raiz }] };
    this.base = new OrigenEnMemoria<FilaOf>({
      datos: () => {
        const todas = this.ordenes();
        return todas.map(of => fila(of, 0, false, raizDe(of, todas)));
      },
      id: f => f.id,
      vista: this.vista,
    });
  }

  async consultar(c: ConsultaLista): Promise<ResultadoLista<FilaOf>> {
    this.consultas++;
    if (c.orden.length || c.agruparPor.length > c.grupo.length) return this.base.resolver(c);
    const todas = this.base.resolver({ ...c, pagina: 0, tamano: Number.MAX_SAFE_INTEGER }).filas;
    const arbol = enArbol(todas.map(f => f.of), this.ordenes());
    return { filas: arbol.slice(c.pagina * c.tamano, (c.pagina + 1) * c.tamano), grupos: null, total: arbol.length, totales: {} };
  }
}

function fila(of: ManufacturingOrder, nivel: number, maestra: boolean, raiz: string): FilaOf {
  return {
    id: of.folio, of, folio: of.folio, producto: of.producto, proceso: of.processLabel, cantidad: of.cantidad, unidad: of.unidad,
    estado: of.state, pedido: of.pedidoFolio, nivel, maestra, raiz,
  };
}

/** Recorrido en profundidad por `originFolio`: raíz sin origen, o cuyo origen quedó fuera del filtro. */
export function enArbol(visibles: ManufacturingOrder[], todas: readonly ManufacturingOrder[] = visibles): FilaOf[] {
  const folios = new Set(visibles.map(o => o.folio));
  const porFolio = (a: ManufacturingOrder, b: ManufacturingOrder) => ordenCultural(a.folio, b.folio);
  const plano: FilaOf[] = [];
  const recorrer = (o: ManufacturingOrder, nivel: number) => {
    const hijos = visibles.filter(h => h.originFolio === o.folio).sort(porFolio);
    plano.push(fila(o, nivel, nivel === 0 && hijos.length > 0, raizDe(o, todas)));
    hijos.forEach(h => recorrer(h, nivel + 1));
  };
  visibles.filter(o => o.originFolio === undefined || !folios.has(o.originFolio)).sort(porFolio).forEach(o => recorrer(o, 0));
  return plano;
}

/**
 * Acciones de la OF (aclaración P2): las usan el formulario y el kanban, para que cada transición tenga
 * una sola regla.
 */
@Injectable({ providedIn: 'root' })
export class FabricacionAcciones {
  private readonly flow = inject(OperationalFlowState);
  private readonly ops = inject(StockOperationState);

  /** Confirmar (Borrador → Planeado): requiere componentes y libera la recolección a Almacén. */
  confirmar(folio: string): string | undefined {
    const of = this.flow.getOrder(folio);
    if (!of) return 'La orden no existe.';
    if (of.state !== 'Borrador') return 'La orden ya fue confirmada.';
    if (of.componentes.length === 0) return 'Agregue al menos un componente antes de confirmar.';
    this.flow.planear(folio);
    return undefined;
  }

  /** Motivo por el que la OF no se puede cerrar todavía, o `undefined` si se puede. */
  motivoParaNoCerrar(folio: string): string | undefined {
    const of = this.flow.getOrder(folio);
    if (!of) return 'La orden no existe.';
    if (of.produccion.length === 0) return 'Registre la producción antes de cerrar.';
    if (of.calidadRequerida && of.produccion.some(l => l.estado === 'En revisión')) return 'Hay lotes en revisión de Calidad: no se puede cerrar (hard-stop).';
    const wip = this.ops.puedeCerrarOf(folio);
    return wip.ok ? undefined : wip.motivo;
  }

  /** Cerrar producción (→ Hecho). */
  cerrar(folio: string): string | undefined {
    const motivo = this.motivoParaNoCerrar(folio);
    if (motivo) return motivo;
    this.flow.cerrarProduccion(folio);
    return undefined;
  }

  origen(filtroPedido: () => string | undefined): OrigenOf {
    return new OrigenOf(() => this.flow.manufacturingOrders.filter(o => !filtroPedido() || o.pedidoFolio === filtroPedido()));
  }

  /** Transiciones que se arrastran en el kanban (aclaración P2). Planeado → En progreso la avanza el sistema al planear. */
  transiciones(): TransicionKanban<FilaOf>[] {
    return [
      { desde: 'Borrador', hacia: 'Planeado', nombre: 'Confirmar', ejecutar: f => this.confirmar(f.folio) },
      { desde: 'En progreso', hacia: 'Hecho', nombre: 'Cerrar producción', ejecutar: f => this.cerrar(f.folio) },
    ];
  }
}
