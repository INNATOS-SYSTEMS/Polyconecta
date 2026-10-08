import { Component, computed, input, model, output, signal } from '@angular/core';
import { OdooIcon } from '../odoo-icon/odoo-icon';
import { Favorito } from '../../core/lista/favoritos';
import { SearchView, etiquetasCampos } from '../../core/search/search-view';

/** Faceta que llega del contexto de navegación; se ve y se quita igual que un filtro. */
export interface Facet {
  campo: string;
  valor: string;
  onRemove?: () => void;
}

/**
 * Réplica de Components/Views/OdooSearchPanel.razor: la barra de búsqueda la alimenta la vista
 * de búsqueda del modelo; los campos, los filtros y las agrupaciones no los declara la pantalla.
 */
@Component({
  selector: 'pc-odoo-search-panel',
  imports: [OdooIcon],
  templateUrl: './odoo-search-panel.html',
  styles: ':host { display: contents; }',
})
export class OdooSearchPanel<T> {
  readonly view = input.required<SearchView<T>>();
  readonly facetasContexto = input<readonly Facet[]>([]);
  readonly texto = model('');
  readonly filtrosActivos = model<string[]>([]);
  /** Agrupaciones aplicadas, en orden de anidamiento. Solo agrupan las listas que las enlazan. */
  readonly agrupacionesActivas = model<string[]>([]);
  /** Equivale a `AgrupacionesActivasChanged.HasDelegate`: la lista enlaza las agrupaciones. */
  readonly permiteAgrupar = input(false);
  /** Favoritos de la lista (spec 011). Con `conFavoritos`, el menú ofrece guardar, aplicar y borrar. */
  readonly conFavoritos = input(false);
  readonly favoritos = input<readonly Favorito[]>([]);
  readonly guardarFavorito = output<{ nombre: string; porOmision: boolean }>();
  readonly aplicarFavorito = output<Favorito>();
  readonly borrarFavorito = output<string>();
  protected readonly nombreFavorito = signal('');
  protected readonly favoritoPorOmision = signal(false);

  protected readonly menuAbierto = signal(false);

  protected readonly puedeAgrupar = computed(() => this.permiteAgrupar() && this.view().agrupaciones.length > 0);

  protected readonly placeholder = computed(() =>
    this.facetasContexto().length > 0 || this.filtrosActivos().length > 0 || this.agrupacionesActivas().length > 0
      ? ''
      : `Buscar por ${etiquetasCampos(this.view())}...`,
  );

  /** Filtros activos agrupados por campo, en el orden en que aparecen (como GroupBy de LINQ). */
  protected readonly gruposActivos = computed(() => agrupar(this.filtrosActivos(), n => this.campoDe(n)));

  protected readonly gruposDeFiltros = computed(() => agrupar(this.view().filtros, f => f.campo));

  protected campoDe(nombreFiltro: string): string {
    return this.view().filtros.find(f => f.nombre === nombreFiltro)?.campo ?? 'Filtro';
  }

  protected onInput(event: Event): void {
    this.texto.set((event.target as HTMLInputElement).value ?? '');
  }

  protected alternar(nombre: string): void {
    this.filtrosActivos.update(lista => alternarEn(lista, nombre));
  }

  protected alternarAgrupacion(etiqueta: string): void {
    this.agrupacionesActivas.update(lista => alternarEn(lista, etiqueta));
  }

  protected quitarGrupo(campo: string): void {
    this.filtrosActivos.update(lista => lista.filter(n => this.campoDe(n) !== campo));
  }

  protected guardar(): void {
    this.guardarFavorito.emit({ nombre: this.nombreFavorito(), porOmision: this.favoritoPorOmision() });
    this.nombreFavorito.set('');
    this.favoritoPorOmision.set(false);
  }

  protected aplicarF(f: Favorito): void {
    this.filtrosActivos.set([]);
    this.aplicarFavorito.emit(f);
    this.menuAbierto.set(false);
  }

  protected limpiarTodo(): void {
    this.menuAbierto.set(false);
    this.filtrosActivos.set([]);
  }
}

function alternarEn(lista: readonly string[], valor: string): string[] {
  return lista.includes(valor) ? lista.filter(v => v !== valor) : [...lista, valor];
}

function agrupar<T>(items: readonly T[], clave: (item: T) => string): { key: string; items: T[] }[] {
  const grupos: { key: string; items: T[] }[] = [];
  for (const item of items) {
    const key = clave(item);
    const grupo = grupos.find(g => g.key === key);
    if (grupo) grupo.items.push(item);
    else grupos.push({ key, items: [item] });
  }
  return grupos;
}
