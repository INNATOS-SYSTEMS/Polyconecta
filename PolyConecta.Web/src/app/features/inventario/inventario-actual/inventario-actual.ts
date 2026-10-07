import { Component, computed, inject, signal } from '@angular/core';
import { n1, ordenCultural } from '../../../core/format/numero';
import { StockQuant } from '../../../core/models/inventario';
import { aplicar } from '../../../core/search/search-view';
import { INVENTARIO_ACTUAL } from '../../../core/search/views';
import { InventoryState } from '../../../core/state/inventory-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooPager } from '../../../shared/odoo-pager/odoo-pager';
import { OdooSearchPanel } from '../../../shared/odoo-search-panel/odoo-search-panel';

interface Grupo {
  ruta: string;
  etiqueta: string;
  nivel: number;
  cantidad: number;
  total: number;
  unidad?: string;
}

interface Renglon {
  grupo?: Grupo;
  quant?: StockQuant;
}

/** Réplica de Pages/InventarioActualList.razor (en /inventario y /ventas/inventario). */
@Component({
  selector: 'pc-inventario-actual',
  imports: [OdooBreadcrumb, OdooSearchPanel, OdooPager],
  templateUrl: './inventario-actual.html',
  styles: ':host { display: contents; }',
})
export class InventarioActual {
  private readonly inv = inject(InventoryState);
  protected readonly vista = INVENTARIO_ACTUAL;
  protected readonly n1 = n1;

  protected readonly searchText = signal('');
  protected readonly filtros = signal<string[]>([]);
  protected readonly agrupaciones = signal<string[]>([...(INVENTARIO_ACTUAL.agrupacionesPorDefecto ?? [])]);
  /** Grupos expandidos, por su ruta de claves. Como en Odoo, abren plegados. */
  protected readonly abiertos = signal(new Set<string>());

  protected readonly filtradas = computed(() => {
    this.inv.cambios();
    return aplicar(this.vista, this.inv.existencias(), this.searchText(), this.filtros()).sort(
      (a, b) => ordenCultural(a.ubicacion, b.ubicacion) || ordenCultural(a.producto.clave, b.producto.clave) || ordenCultural(a.lote, b.lote),
    );
  });

  /**
   * Aplana la agrupación anidada en renglones: encabezado de grupo y, si está abierto, su contenido.
   * El total solo se muestra cuando todo el grupo comparte unidad.
   */
  protected readonly renglones = computed<Renglon[]>(() => {
    const claves = this.agrupaciones()
      .map(e => this.vista.agrupaciones.find(a => a.etiqueta === e))
      .filter(a => a !== undefined);
    const abiertos = this.abiertos();
    const res: Renglon[] = [];
    const agregar = (items: StockQuant[], nivel: number, ruta: string) => {
      if (nivel === claves.length) {
        res.push(...items.map(quant => ({ quant })));
        return;
      }
      const grupos = new Map<string, StockQuant[]>();
      for (const q of items) {
        const k = claves[nivel]!.clave(q);
        grupos.set(k, [...(grupos.get(k) ?? []), q]);
      }
      for (const [clave, g] of [...grupos].sort(([a], [b]) => ordenCultural(a, b))) {
        const rutaGrupo = `${ruta}/${clave}`;
        const unidades = [...new Set(g.map(q => q.producto.unidad))];
        res.push({
          grupo: {
            ruta: rutaGrupo,
            etiqueta: clave,
            nivel,
            cantidad: g.length,
            total: g.reduce((t, q) => t + q.cantidad, 0),
            unidad: unidades.length === 1 ? unidades[0] : undefined,
          },
        });
        if (abiertos.has(rutaGrupo)) agregar(g, nivel + 1, rutaGrupo);
      }
    };
    agregar(this.filtradas(), 0, '');
    return res;
  });

  protected alternarGrupo(ruta: string): void {
    this.abiertos.update(s => {
      const n = new Set(s);
      if (!n.delete(ruta)) n.add(ruta);
      return n;
    });
  }
}
