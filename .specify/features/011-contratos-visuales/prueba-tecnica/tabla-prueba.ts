import { Component, computed, effect, signal } from '@angular/core';
import {
  FlexRender,
  columnOrderingFeature,
  columnVisibilityFeature,
  createColumnHelper,
  createExpandedRowModel,
  injectTable,
  rowExpandingFeature,
  rowPaginationFeature,
  rowSelectionFeature,
  rowSortingFeature,
  tableFeatures,
  type ExpandedState,
  type PaginationState,
  type RowSelectionState,
  type SortingState,
  type ColumnVisibilityState,
} from '@tanstack/angular-table';
import writeExcelFile from 'write-excel-file/browser';
import { LucideArrowLeft, LucideDownload } from '@lucide/angular';
import { OrigenEnMemoria, type GrupoLista } from './origen';

export interface Pedido { id: string; folio: string; cliente: string; estado: string; total: number }
interface Fila extends Partial<Pedido> { id: string; grupo?: GrupoLista; hijos?: Fila[] }

const CLIENTES = ['EMPRESA MEXICANA', 'BOLSAS DEL NORTE', 'PLÁSTICOS SAN PEDRO', 'EMPAQUES MTY', 'DISTRIBUIDORA CUMBRES'];
const ESTADOS = ['Borrador', 'Confirmado', 'Autorizado', 'Hecho'];
export const PEDIDOS: Pedido[] = Array.from({ length: 57 }, (_, i) => ({
  id: `p${i + 1}`,
  folio: `PV-2026-${String(i + 1).padStart(4, '0')}`,
  cliente: CLIENTES[i % CLIENTES.length],
  estado: ESTADOS[(i * 7) % ESTADOS.length],
  total: 1000 + ((i * 3779) % 50000),
}));

const features = tableFeatures({
  rowSortingFeature,
  rowPaginationFeature,
  columnVisibilityFeature,
  columnOrderingFeature,
  rowSelectionFeature,
  rowExpandingFeature,
  expandedRowModel: createExpandedRowModel(),
});
const col = createColumnHelper<typeof features, Fila>();
const columns = col.columns([
  col.accessor('folio', { header: 'Folio' }),
  col.accessor('cliente', { header: 'Cliente' }),
  col.accessor('estado', { header: 'Estado' }),
  col.accessor('total', { header: 'Total' }),
]);

@Component({
  selector: 'app-tabla-prueba',
  imports: [FlexRender, LucideArrowLeft, LucideDownload],
  template: `
    <div class="d-flex gap-2 align-items-center mb-2 flex-wrap" data-prueba="barra">
      <label>Agrupar por
        <select data-prueba="agrupar" (change)="agrupar($any($event.target).value)">
          <option value="">(ninguno)</option><option value="cliente">Cliente</option><option value="estado">Estado</option>
        </select>
      </label>
      <label>Filtrar cliente <input data-prueba="filtro" (input)="filtrar($any($event.target).value)"></label>
      <label>Filas <select data-prueba="tamano" (change)="tamano(+$any($event.target).value)">
        <option>10</option><option>20</option><option>80</option></select></label>
      @for (c of table.getAllLeafColumns(); track c.id) {
        <label><input type="checkbox" [attr.data-prueba]="'ver-' + c.id" [checked]="c.getIsVisible()" (change)="c.toggleVisibility()"> {{ c.id }}</label>
      }
      <button type="button" class="btn btn-outline-secondary btn-sm" data-prueba="mover-total" (click)="moverTotal()"><svg lucideArrowLeft></svg> Total a la izquierda</button>
      <button type="button" class="btn btn-primary btn-sm" data-prueba="exportar" (click)="exportar()"><svg lucideDownload></svg> Exportar</button>
      <span data-prueba="consultas">{{ consultas() }}</span>
    </div>
    <table class="table table-sm" data-prueba="tabla">
      <thead><tr>
        <th><input type="checkbox" data-prueba="todas" [checked]="table.getIsAllPageRowsSelected()" (click)="table.getToggleAllPageRowsSelectedHandler()($event)"></th>
        @for (h of table.getHeaderGroups()[0].headers; track h.id) {
          <th [attr.data-prueba]="'col-' + h.column.id" (click)="h.column.getToggleSortingHandler()?.($event)" style="cursor:pointer">
            <ng-container *flexRenderHeader="h; let v">{{ v }}</ng-container>
            {{ h.column.getIsSorted() === 'asc' ? '▲' : h.column.getIsSorted() === 'desc' ? '▼' : '' }}
          </th>
        }
      </tr></thead>
      <tbody>
        @for (row of table.getRowModel().rows; track row.id) {
          @if (row.original.grupo; as g) {
            <tr data-prueba="grupo" (click)="abrirGrupo(row.original)" style="cursor:pointer;font-weight:600">
              <td [attr.colspan]="row.getVisibleCells().length + 1">{{ row.getIsExpanded() ? '▾' : '▸' }} {{ g.valor }} ({{ g.cantidad }}) · {{ g.totales['total'] }}</td>
            </tr>
          } @else {
            <tr data-prueba="fila">
              <td><input type="checkbox" [checked]="row.getIsSelected()" (click)="row.getToggleSelectedHandler()($event)"></td>
              @for (cell of row.getVisibleCells(); track cell.id) {
                <td><ng-container *flexRenderCell="cell; let v">{{ v }}</ng-container></td>
              }
            </tr>
          }
        }
      </tbody>
    </table>
    <div data-prueba="pager">
      <button type="button" class="btn btn-outline-secondary btn-sm" data-prueba="anterior" (click)="table.previousPage()" [disabled]="!table.getCanPreviousPage()">‹</button>
      <span data-prueba="pagina">{{ pagination().pageIndex + 1 }} / {{ table.getPageCount() }}</span>
      <button type="button" class="btn btn-outline-secondary btn-sm" data-prueba="siguiente" (click)="table.nextPage()" [disabled]="!table.getCanNextPage()">›</button>
      <span data-prueba="total">{{ total() }}</span>
    </div>`,
})
export class TablaPrueba {
  readonly origen = new OrigenEnMemoria<Pedido>(PEDIDOS, ['total']);
  readonly sorting = signal<SortingState>([]);
  readonly pagination = signal<PaginationState>({ pageIndex: 0, pageSize: 10 });
  readonly visibility = signal<ColumnVisibilityState>({});
  readonly order = signal<string[]>(['folio', 'cliente', 'estado', 'total']);
  readonly selection = signal<RowSelectionState>({});
  readonly expanded = signal<ExpandedState>({});
  readonly filtros = signal<Record<string, string>>({});
  readonly agruparPor = signal<string | null>(null);
  readonly data = signal<Fila[]>([]);
  readonly total = signal(0);
  readonly consultas = signal(0);
  private readonly cargadas = new Map<string, Pedido>();

  readonly table = injectTable(() => ({
    features,
    columns,
    data: this.data(),
    getRowId: (r: Fila) => r.id,
    getSubRows: (r: Fila) => r.hijos,
    getRowCanExpand: (r: { original: Fila }) => !!r.original.grupo,
    manualSorting: true,
    sortDescFirst: false,
    manualPagination: true,
    manualExpanding: false,
    // En modo servidor la página y los grupos abiertos los controla la consulta, no TanStack.
    autoResetPageIndex: false,
    autoResetExpanded: false,
    enableSubRowSelection: false,
    rowCount: this.total(),
    state: {
      sorting: this.sorting(),
      pagination: this.pagination(),
      columnVisibility: this.visibility(),
      columnOrder: this.order(),
      rowSelection: this.selection(),
      expanded: this.expanded(),
    },
    onSortingChange: (u: unknown) => this.aplicar(this.sorting, u),
    onPaginationChange: (u: unknown) => this.aplicar(this.pagination, u),
    onColumnVisibilityChange: (u: unknown) => this.aplicar(this.visibility, u),
    onColumnOrderChange: (u: unknown) => this.aplicar(this.order, u),
    onRowSelectionChange: (u: unknown) => this.aplicar(this.selection, u),
    onExpandedChange: (u: unknown) => this.aplicar(this.expanded, u),
  }));

  private readonly consulta = computed(() => ({
    pagina: this.pagination().pageIndex,
    tamano: this.pagination().pageSize,
    orden: this.sorting().map(s => ({ campo: s.id, desc: s.desc })),
    filtros: this.filtros(),
    agruparPor: this.agruparPor(),
  }));

  constructor() {
    (window as unknown as { __tabla: unknown }).__tabla = this;
    effect(() => {
      const c = this.consulta();
      void this.origen.consultar(c).then(r => {
        this.consultas.set(this.origen.consultas);
        this.total.set(r.total);
        this.expanded.set({});
        this.data.set(r.grupos
          ? r.grupos.map(g => ({ id: `g:${g.valor}`, grupo: g }))
          : r.filas.map(f => { this.cargadas.set(f.id, f); return f; }));
      });
    });
  }

  private aplicar<T>(s: { (): T; set(v: T): void }, u: unknown): void {
    s.set(typeof u === 'function' ? (u as (p: T) => T)(s()) : (u as T));
  }

  agrupar(campo: string): void { this.agruparPor.set(campo || null); this.pagination.update(p => ({ ...p, pageIndex: 0 })); }
  filtrar(v: string): void { this.filtros.set(v ? { cliente: v } : {}); this.pagination.update(p => ({ ...p, pageIndex: 0 })); }
  tamano(n: number): void { this.pagination.set({ pageIndex: 0, pageSize: n }); }
  moverTotal(): void { this.order.set(['total', ...this.order().filter(c => c !== 'total')]); }

  /** Agrupación en el servidor: abrir un grupo pide sus filas con el filtro del grupo. */
  async abrirGrupo(fila: Fila): Promise<void> {
    const campo = this.agruparPor()!;
    if (!fila.hijos) {
      const r = await this.origen.consultar({ pagina: 0, tamano: 1000, orden: this.consulta().orden, filtros: { ...this.filtros(), [campo]: fila.grupo!.valor }, agruparPor: null });
      this.consultas.set(this.origen.consultas);
      r.filas.forEach(f => this.cargadas.set(f.id, f));
      this.data.set(this.data().map(d => d.id === fila.id ? { ...d, hijos: r.filas } : d));
    }
    this.expanded.update(e => ({ ...(e as Record<string, boolean>), [fila.id]: !(e as Record<string, boolean>)[fila.id] }));
  }

  /** Exporta las seleccionadas o, sin selección, todas las del filtro actual, con las columnas visibles en su orden. */
  async exportar(): Promise<void> {
    const ids = Object.keys(this.selection()).filter(k => this.selection()[k]);
    const filas = ids.length
      ? ids.map(id => this.cargadas.get(id)!).filter(Boolean)
      : (await this.origen.consultar({ ...this.consulta(), pagina: 0, tamano: 100000, agruparPor: null })).filas;
    const cols = this.table.getVisibleLeafColumns().map(c => c.id as keyof Pedido);
    const hoja = [cols.map(c => String(c)), ...filas.map(f => cols.map(c => f[c]))];
    const blob = await writeExcelFile(hoja).toBlob();
    (window as unknown as { __ultimaExportacion?: { filas: number; columnas: string[]; bytes: number; zip: boolean } }).__ultimaExportacion = {
      filas: filas.length, columnas: cols, bytes: blob.size, zip: new Uint8Array(await blob.slice(0, 2).arrayBuffer()).join() === '80,75',
    };
  }
}
