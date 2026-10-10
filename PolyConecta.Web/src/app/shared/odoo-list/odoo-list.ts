import { NgTemplateOutlet } from '@angular/common';
import { BrnPopover, BrnPopoverContent, BrnPopoverTrigger } from '@spartan-ng/brain/popover';
import { Component, computed, effect, inject, input, model, OnInit, output, signal, untracked } from '@angular/core';
import {
  columnOrderingFeature,
  columnVisibilityFeature,
  createExpandedRowModel,
  injectTable,
  rowExpandingFeature,
  rowSelectionFeature,
  rowSortingFeature,
  tableFeatures,
  type ColumnDef,
  type ColumnVisibilityState,
  type ExpandedState,
  type Row,
  type RowSelectionState,
  type SortingState,
} from '@tanstack/angular-table';
import { ActivatedRoute, Router } from '@angular/router';
import { AlmacenDeFavoritos, Favorito, FavoritosEnNavegador } from '../../core/lista/favoritos';
import { ConsultaLista, consultaInicial, FiltroLista, GrupoLista, OrigenDeLista, TAMANOS_PAGINA } from '../../core/lista/origen';
import { AvisosService } from '../odoo-dialog/avisos';
import { OdooIcon } from '../odoo-icon/odoo-icon';
import { OdooPager } from '../odoo-pager/odoo-pager';
import { AccionMasiva, ColumnaLista, esNumerica, formatear, leerColumna, textoColumna } from './columnas';
import { exportarExcel } from './exportar';

/** Fila de la tabla: un registro o un grupo (sintético, research R-02). */
interface Fila<T> {
  id: string;
  dato?: T;
  grupo?: GrupoLista & { ruta: { campo: string; valor: string }[]; nivel: number };
  hijos?: Fila<T>[];
}

const features = tableFeatures({
  rowSortingFeature,
  columnVisibilityFeature,
  columnOrderingFeature,
  rowSelectionFeature,
  rowExpandingFeature,
  expandedRowModel: createExpandedRowModel(),
});

/**
 * Tabla de lista (contratos visuales, docs/diseno/07): TanStack Table en modo servidor. Nunca ordena,
 * filtra, agrupa ni pagina por su cuenta: cada vista la pide al `OrigenDeLista`. Pinta la misma tabla de
 * Bootstrap que las listas de la réplica, para no cambiar su estructura.
 */
@Component({
  selector: 'pc-odoo-list',
  imports: [NgTemplateOutlet, OdooIcon, OdooPager, BrnPopover, BrnPopoverTrigger, BrnPopoverContent],
  templateUrl: './odoo-list.html',
  styles: ':host { display: block; }',
})
export class OdooList<T> implements OnInit {
  private readonly avisos = inject(AvisosService);

  /** Llave de la lista (`ventas.pedidos`): favoritos y nombre del archivo exportado. */
  readonly lista = input.required<string>();
  readonly origen = input.required<OrigenDeLista<T>>();
  readonly columnas = input.required<ColumnaLista<T>[]>();
  readonly idDe = input.required<(fila: T) => string>();
  readonly seleccion = input(true);
  readonly acciones = input<AccionMasiva[]>([]);
  readonly filtros = input<FiltroLista[]>([]);
  readonly ordenInicial = input<SortingState>([]);
  readonly tamanoInicial = input(80);
  readonly mensajeVacio = input('No hay registros que mostrar.');
  /** Sin paginador, la lista muestra hasta `tamanoInicial` filas (pantallas del prototipo que no paginan). */
  readonly conPaginador = input(true);
  /** Filas que abren un formulario al pulsarlas. Sin formulario (incidencias), `false`. */
  readonly filasPulsables = input(true);
  /** Dónde viven los favoritos: en el navegador (réplica en memoria) o por usuario en la base (listas HTTP). */
  readonly almacenFavoritos = input<AlmacenDeFavoritos>(new FavoritosEnNavegador());
  /**
   * Refleja búsqueda, filtros con nombre, agrupación, orden, página y tamaño en la URL y los restaura al
   * abrirla, recargar o compartir el enlace (02 §7, US4 escenario 1). Un favorito aplicado también se ve.
   */
  readonly estadoEnUrl = input(false);

  /** Estado de la barra de búsqueda, enlazado en dos sentidos para que un favorito lo pueda cambiar. */
  readonly busqueda = model('');
  readonly nombrados = model<string[]>([]);
  readonly agruparPor = model<string[]>([]);

  readonly abrir = output<T>();
  readonly consultaCambio = output<ConsultaLista>();

  readonly sorting = signal<SortingState>([]);
  readonly pagina = signal(0);
  readonly tamano = signal(80);
  readonly visibilidad = signal<ColumnVisibilityState>({});
  readonly ordenColumnas = signal<string[]>([]);
  readonly seleccionadas = signal<RowSelectionState>({});
  readonly expandidas = signal<ExpandedState>({});
  readonly filas = signal<Fila<T>[]>([]);
  readonly total = signal(0);
  readonly totales = signal<Record<string, number>>({});
  readonly cargando = signal(false);
  readonly menuTamano = signal(false);
  readonly favoritos = signal<Favorito[]>([]);
  protected readonly tamanos = TAMANOS_PAGINA;
  private peticion = 0;
  private readonly router = inject(Router, { optional: true });
  private readonly ruta = inject(ActivatedRoute, { optional: true });
  /** La página que trae la URL: el primer reinicio de página la respeta en lugar de volver a 0. */
  private paginaDeUrl: number | null = null;
  /** No se escribe la URL hasta haber leído la que trajo la ruta. */
  private readonly urlLeida = signal(false);

  readonly columnasVisibles = computed(() => {
    const vis = this.visibilidad();
    const orden = this.ordenColumnas();
    const porCampo = new Map(this.columnas().map(c => [c.campo, c]));
    return orden.map(c => porCampo.get(c)).filter((c): c is ColumnaLista<T> => !!c && vis[c.campo] !== false);
  });

  readonly hayTotales = computed(() => this.columnasVisibles().some(c => c.sumable));

  private readonly defs = computed<ColumnDef<typeof features, Fila<T>, unknown>[]>(() =>
    this.columnas().map(c => ({
      id: c.campo,
      accessorFn: (f: Fila<T>) => (f.dato ? leerColumna(c, f.dato) : undefined),
      enableSorting: c.ordenable !== false,
      header: c.titulo,
    })) as ColumnDef<typeof features, Fila<T>, unknown>[],
  );

  readonly table = injectTable(() => ({
    features,
    columns: this.defs(),
    data: this.filas(),
    getRowId: (f: Fila<T>) => f.id,
    getSubRows: (f: Fila<T>) => f.hijos,
    getRowCanExpand: (r: Row<typeof features, Fila<T>>) => !!r.original.grupo,
    enableRowSelection: (r: Row<typeof features, Fila<T>>) => !r.original.grupo,
    enableSubRowSelection: false,
    manualSorting: true,
    manualExpanding: false,
    sortDescFirst: false,
    // Modo servidor: la página y los grupos abiertos los controla la consulta (research R-02).
    autoResetExpanded: false,
    state: {
      sorting: this.sorting(),
      columnVisibility: this.visibilidad(),
      columnOrder: this.ordenColumnas(),
      rowSelection: this.seleccionadas(),
      expanded: this.expandidas(),
    },
    onSortingChange: (u: unknown) => this.aplicar(this.sorting, u),
    onColumnVisibilityChange: (u: unknown) => this.aplicar(this.visibilidad, u),
    onColumnOrderChange: (u: unknown) => this.aplicar(this.ordenColumnas, u),
    onRowSelectionChange: (u: unknown) => this.aplicar(this.seleccionadas, u),
    onExpandedChange: (u: unknown) => this.aplicar(this.expandidas, u),
  }));

  readonly consulta = computed<ConsultaLista>(() => consultaInicial({
    pagina: this.pagina(),
    tamano: this.tamano(),
    orden: this.sorting().map(s => ({ campo: s.id, desc: s.desc })),
    filtros: this.filtros(),
    nombrados: this.nombrados(),
    busqueda: this.busqueda().trim() || null,
    agruparPor: this.agruparPor(),
  }));

  /** Ids de los registros seleccionados (sin el prefijo de fila). */
  readonly idsSeleccionados = computed(() => Object.keys(this.seleccionadas()).filter(k => this.seleccionadas()[k]).map(k => k.slice(2)));

  /** Registros seleccionados entre los cargados: para decidir si una acción contextual aplica. */
  readonly filasSeleccionadas = computed(() => {
    const marcadas = this.seleccionadas();
    const salida: T[] = [];
    const recorrer = (filas: Fila<T>[]) => {
      for (const f of filas) {
        if (f.dato && marcadas[f.id]) salida.push(f.dato);
        if (f.hijos) recorrer(f.hijos);
      }
    };
    recorrer(this.filas());
    return salida;
  });

  readonly inicio = computed(() => (this.total() === 0 ? 0 : this.pagina() * this.tamano() + 1));
  readonly fin = computed(() => Math.min(this.total(), (this.pagina() + 1) * this.tamano()));

  constructor() {
    // Cambian los filtros, la búsqueda o la agrupación: vuelve a la primera página.
    effect(() => {
      this.filtros();
      this.nombrados();
      this.busqueda();
      this.agruparPor();
      untracked(() => {
        this.pagina.set(this.paginaDeUrl ?? 0);
        this.paginaDeUrl = null;
      });
    });
    // Vuelve a consultar al cambiar la consulta y también al cambiar el origen (datos que llegan después de pintar).
    effect(() => {
      const c = this.consulta();
      this.origen();
      untracked(() => void this.cargar(c));
    });
    effect(() => {
      const c = this.consulta();
      if (this.estadoEnUrl() && this.urlLeida()) untracked(() => this.escribirUrl(c));
    });
  }

  async ngOnInit(): Promise<void> {
    this.ordenColumnas.set(this.columnas().map(c => c.campo));
    this.visibilidad.set(Object.fromEntries(this.columnas().filter(c => c.visible === false).map(c => [c.campo, false])));
    this.sorting.set(this.ordenInicial());
    this.tamano.set(this.tamanoInicial());
    // Un enlace con estado manda sobre el favorito por omisión.
    const desdeUrl = this.estadoEnUrl() && this.leerUrl();
    this.urlLeida.set(true);
    try {
      this.favoritos.set(await this.almacenFavoritos().listar(this.lista()));
    } catch {
      this.favoritos.set([]); // sin favoritos, la lista sigue funcionando
    }
    const porOmision = this.favoritos().find(f => f.porOmision);
    if (porOmision && !desdeUrl) this.aplicarFavorito(porOmision);
  }

  // --- Estado en la URL (02 §7) ---

  /** Aplica el estado que trae la URL; `false` si no trae ninguno. */
  private leerUrl(): boolean {
    const p = this.ruta?.snapshot.queryParamMap;
    if (!p || !['q', 'filtro', 'agrupar', 'orden', 'pagina', 'tamano'].some(k => p.has(k))) return false;
    this.busqueda.set(p.get('q') ?? '');
    this.nombrados.set(p.getAll('filtro'));
    this.agruparPor.set(p.getAll('agrupar'));
    this.sorting.set(p.getAll('orden').map(o => (o.startsWith('-') ? { id: o.slice(1), desc: true } : { id: o, desc: false })));
    const tamano = Number(p.get('tamano'));
    if ((TAMANOS_PAGINA as readonly number[]).includes(tamano)) this.tamano.set(tamano);
    const pagina = Number(p.get('pagina'));
    this.paginaDeUrl = Number.isInteger(pagina) && pagina > 1 ? pagina - 1 : null;
    return true;
  }

  private escribirUrl(c: ConsultaLista): void {
    if (!this.router || !this.ruta) return;
    const vacio = <V>(v: V[]) => (v.length ? v : null);
    void this.router.navigate([], {
      relativeTo: this.ruta,
      queryParams: {
        q: c.busqueda,
        filtro: vacio(c.nombrados),
        agrupar: vacio(c.agruparPor),
        orden: vacio(c.orden.map(o => (o.desc ? `-${o.campo}` : o.campo))),
        pagina: c.pagina > 0 ? c.pagina + 1 : null,
        tamano: c.tamano !== this.tamanoInicial() ? c.tamano : null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  private aplicar<V>(s: { (): V; set(v: V): void }, u: unknown): void {
    s.set(typeof u === 'function' ? (u as (p: V) => V)(s()) : (u as V));
  }

  private async cargar(c: ConsultaLista): Promise<void> {
    const id = ++this.peticion;
    this.cargando.set(true);
    this.consultaCambio.emit(c);
    const r = await this.origen().consultar(c);
    if (id !== this.peticion) return; // llegó una consulta más nueva
    this.cargando.set(false);
    this.total.set(r.total);
    this.totales.set(r.totales);
    this.expandidas.set({});
    this.filas.set(r.grupos ? r.grupos.map(g => this.filaGrupo(g, [], 0)) : (r.filas ?? []).map(d => this.filaDato(d)));
  }

  /** Vuelve a pedir la vista actual: después de que un documento cambió de estado. */
  recargar(): void {
    void this.cargar(this.consulta());
  }

  private filaDato(d: T): Fila<T> {
    return { id: `r:${this.idDe()(d)}`, dato: d };
  }

  private filaGrupo(g: GrupoLista, padre: { campo: string; valor: string }[], nivel: number): Fila<T> {
    const ruta = [...padre, { campo: g.campo, valor: g.valor }];
    return { id: `g:${ruta.map(r => `${r.campo}=${r.valor}`).join('/')}`, grupo: { ...g, ruta, nivel } };
  }

  /** Abre o cierra un grupo; la primera vez pide sus filas (o subgrupos) al origen. */
  async alternarGrupo(fila: Fila<T>): Promise<void> {
    const g = fila.grupo!;
    if (!fila.hijos) {
      const r = await this.origen().consultar({ ...this.consulta(), pagina: 0, tamano: 200, grupo: g.ruta });
      const hijos = r.grupos ? r.grupos.map(sg => this.filaGrupo(sg, g.ruta, g.nivel + 1)) : r.filas.map(d => this.filaDato(d));
      this.filas.set(reemplazar(this.filas(), fila.id, { ...fila, hijos }));
    }
    this.expandidas.update(e => {
      const actual = (e === true ? {} : e) as Record<string, boolean>;
      return { ...actual, [fila.id]: !actual[fila.id] };
    });
  }

  alternarOrden(campo: string): void {
    const col = this.table.getColumn(campo);
    if (col?.getCanSort()) col.toggleSorting(undefined, false);
  }

  cambiarPagina(delta: number): void {
    const ultima = Math.max(0, Math.ceil(this.total() / this.tamano()) - 1);
    this.pagina.set(Math.min(ultima, Math.max(0, this.pagina() + delta)));
  }

  cambiarTamano(n: number): void {
    this.tamano.set(n);
    this.pagina.set(0);
    this.menuTamano.set(false);
  }

  alternarColumna(campo: string): void {
    this.visibilidad.update(v => ({ ...v, [campo]: v[campo] === false }));
  }

  /** Mueve una columna un lugar entre las visibles: salta las ocultas para que el cambio se vea. */
  moverColumna(campo: string, delta: number): void {
    const orden = [...this.ordenColumnas()];
    const i = orden.indexOf(campo);
    let j = i + delta;
    while (j >= 0 && j < orden.length && this.visibilidad()[orden[j]] === false) j += delta;
    if (i < 0 || j < 0 || j >= orden.length) return;
    [orden[i], orden[j]] = [orden[j], orden[i]];
    this.ordenColumnas.set(orden);
  }

  limpiarSeleccion(): void {
    this.seleccionadas.set({});
  }

  /** Exporta las seleccionadas o, sin selección, todas las del filtro, con las columnas visibles en su orden (research R-06). */
  async exportar(): Promise<number> {
    const ids = this.idsSeleccionados();
    const r = await this.origen().consultar({ ...this.consulta(), pagina: 0, tamano: Number.MAX_SAFE_INTEGER, agruparPor: [], grupo: [], ids: ids.length ? ids : null });
    await exportarExcel(this.lista(), this.columnasVisibles(), r.filas);
    return r.filas.length;
  }

  // --- Favoritos (research R-08) ---

  async guardarFavorito(nombre: string, porOmision: boolean): Promise<void> {
    const c = this.consulta();
    try {
      await this.almacenFavoritos().guardar({
        id: `${Date.now()}`, lista: this.lista(), nombre, porOmision,
        filtros: c.filtros, nombrados: c.nombrados, busqueda: c.busqueda, agruparPor: c.agruparPor, orden: c.orden, tamano: c.tamano,
        columnas: this.ordenColumnas().map(campo => ({ campo, visible: this.visibilidad()[campo] !== false })),
      });
      this.favoritos.set(await this.almacenFavoritos().listar(this.lista()));
      this.avisos.exito(`Favorito "${nombre.trim()}" guardado.`);
    } catch (e) {
      this.avisos.error((e as Error).message);
    }
  }

  aplicarFavorito(f: Favorito): void {
    this.busqueda.set(f.busqueda ?? '');
    if (f.nombrados) this.nombrados.set(f.nombrados);
    this.agruparPor.set(f.agruparPor);
    this.sorting.set(f.orden.map(o => ({ id: o.campo, desc: o.desc })));
    this.tamano.set(f.tamano);
    if (f.columnas.length) {
      const conocidas = new Set(this.columnas().map(c => c.campo));
      const orden = f.columnas.map(c => c.campo).filter(c => conocidas.has(c));
      this.ordenColumnas.set([...orden, ...[...conocidas].filter(c => !orden.includes(c))]);
      this.visibilidad.set(Object.fromEntries(f.columnas.map(c => [c.campo, c.visible])));
    }
  }

  async borrarFavorito(id: string): Promise<void> {
    await this.almacenFavoritos().borrar(this.lista(), id);
    this.favoritos.set(await this.almacenFavoritos().listar(this.lista()));
  }

  // --- Presentación ---

  protected readonly texto = textoColumna;
  protected readonly esNumerica = esNumerica;
  protected totalDe(c: ColumnaLista<T>, totales: Record<string, number>): string {
    return c.sumable && totales[c.campo] !== undefined ? formatear(c.tipo, totales[c.campo]) : '';
  }
  protected ordenDe(campo: string): false | 'asc' | 'desc' {
    return this.sorting().find(s => s.id === campo) ? (this.sorting().find(s => s.id === campo)!.desc ? 'desc' : 'asc') : false;
  }
  protected abrirFila(f: Fila<T>): void {
    if (f.dato) this.abrir.emit(f.dato);
  }
}

function reemplazar<T>(filas: Fila<T>[], id: string, nueva: Fila<T>): Fila<T>[] {
  return filas.map(f => (f.id === id ? nueva : f.hijos ? { ...f, hijos: reemplazar(f.hijos, id, nueva) } : f));
}
