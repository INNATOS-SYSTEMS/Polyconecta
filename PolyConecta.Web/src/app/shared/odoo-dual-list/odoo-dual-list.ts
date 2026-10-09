import { Component, computed, input, model, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { OdooIcon } from '../odoo-icon/odoo-icon';
import { AccionPermisoItem, ModuloPermisoItem, ObjetoPermisoItem } from './odoo-dual-list.types';

export * from './odoo-dual-list.types';

interface NodoVisibleObjeto {
  objeto: string;
  etiqueta: string;
  tipo?: string;
  acciones: AccionPermisoItem[];
}

interface NodoVisibleModulo {
  modulo: string;
  etiqueta: string;
  objetos: NodoVisibleObjeto[];
}

/**
 * Selector dual de permisos en árbol Módulo › Objeto/Funcionalidad › Acción (D-148, CT-24).
 * Presenta dos paneles (disponibles y asignados) permitiendo mover acciones individuales o nodos completos,
 * con filtrado en tiempo real en cada panel y navegación por teclado.
 */
@Component({
  selector: 'pc-odoo-dual-list',
  standalone: true,
  imports: [FormsModule, OdooIcon],
  template: `
    <div class="o_dual_list d-flex flex-column flex-md-row gap-3 align-items-stretch" [class.o_disabled]="deshabilitado()">
      <!-- Panel Izquierdo: Disponibles -->
      <div class="o_dual_panel card flex-fill border shadow-sm" data-dual-panel="disponibles">
        <div class="card-header bg-light py-2 px-3 d-flex justify-content-between align-items-center">
          <span class="fw-semibold small text-secondary">{{ tituloDisponibles() }}</span>
          <span class="badge bg-secondary-subtle text-secondary border small" data-conteo-disponibles>
            {{ totalDisponibles() }}
          </span>
        </div>
        <div class="p-2 border-bottom bg-white">
          <div class="input-group input-group-sm">
            <span class="input-group-text bg-transparent border-end-0 text-muted">
              <pc-odoo-icon nombre="buscar" />
            </span>
            <input
              type="text"
              class="form-control border-start-0"
              placeholder="Buscar en disponibles..."
              [ngModel]="filtroIzq()"
              (ngModelChange)="filtroIzq.set($event)"
              [disabled]="deshabilitado()"
              data-buscar-disponibles />
          </div>
        </div>
        <div class="card-body p-2 o_tree_container overflow-auto" style="height: 340px;" tabindex="0" (keydown)="tecladoPanel($event, 'izq')">
          @if (arbolDisponibles().length === 0) {
            <div class="text-muted text-center py-4 small">No hay permisos disponibles</div>
          } @else {
            <ul class="list-unstyled mb-0 o_tree_root">
              @for (mod of arbolDisponibles(); track mod.modulo) {
                <li class="o_tree_node mb-1">
                  <div class="d-flex align-items-center gap-1 py-1 px-2 rounded o_node_header"
                       [class.o_selected]="estaModuloMarcado(mod, 'izq')"
                       (dblclick)="moverModulo(mod, true)">
                    <button type="button" class="btn btn-sm btn-link p-0 text-muted border-0 text-decoration-none"
                            (click)="toggleExpandir('izq-mod-' + mod.modulo, 'izq')">
                      <pc-odoo-icon [nombre]="estaExpandido('izq-mod-' + mod.modulo, 'izq') ? 'chevron-down' : 'chevron-right'" />
                    </button>
                    <input type="checkbox" class="form-check-input mt-0 me-1"
                           [checked]="estaModuloMarcado(mod, 'izq')"
                           [indeterminate]="esModuloParcial(mod, 'izq')"
                           (change)="marcarModulo(mod, 'izq', $any($event.target).checked)"
                           [disabled]="deshabilitado()"
                           [attr.data-check-modulo]="mod.modulo" />
                    <span class="fw-semibold small text-dark flex-grow-1 user-select-none">{{ mod.etiqueta }}</span>
                  </div>

                  @if (estaExpandido('izq-mod-' + mod.modulo, 'izq')) {
                    <ul class="list-unstyled ms-3 ps-2 border-start o_tree_children">
                      @for (obj of mod.objetos; track obj.objeto) {
                        <li class="o_tree_node mb-1">
                          <div class="d-flex align-items-center gap-1 py-1 px-2 rounded o_node_header"
                               [class.o_selected]="estaObjetoMarcado(obj, 'izq')"
                               (dblclick)="moverObjeto(obj, true)">
                            <button type="button" class="btn btn-sm btn-link p-0 text-muted border-0 text-decoration-none"
                                    (click)="toggleExpandir('izq-obj-' + mod.modulo + '-' + obj.objeto, 'izq')">
                              <pc-odoo-icon [nombre]="estaExpandido('izq-obj-' + mod.modulo + '-' + obj.objeto, 'izq') ? 'chevron-down' : 'chevron-right'" />
                            </button>
                            <input type="checkbox" class="form-check-input mt-0 me-1"
                                   [checked]="estaObjetoMarcado(obj, 'izq')"
                                   [indeterminate]="esObjetoParcial(obj, 'izq')"
                                   (change)="marcarObjeto(obj, 'izq', $any($event.target).checked)"
                                   [disabled]="deshabilitado()"
                                   [attr.data-check-objeto]="obj.objeto" />
                            <span class="small text-secondary flex-grow-1 user-select-none">{{ obj.etiqueta }}</span>
                          </div>

                          @if (estaExpandido('izq-obj-' + mod.modulo + '-' + obj.objeto, 'izq')) {
                            <ul class="list-unstyled ms-3 ps-2 border-start o_tree_leafs">
                              @for (act of obj.acciones; track act.clave) {
                                <li class="d-flex align-items-center gap-2 py-1 px-2 rounded o_node_leaf"
                                    [class.o_selected]="marcadosIzq().has(act.clave)"
                                    (dblclick)="moverAccion(act.clave, true)"
                                    [attr.data-permiso-item]="act.clave">
                                  <input type="checkbox" class="form-check-input mt-0"
                                         [checked]="marcadosIzq().has(act.clave)"
                                         (change)="toggleMarcarAccion(act.clave, 'izq')"
                                         [disabled]="deshabilitado()"
                                         [attr.data-check-permiso]="act.clave" />
                                  <span class="small user-select-none">{{ act.etiqueta }}</span>
                                </li>
                              }
                            </ul>
                          }
                        </li>
                      }
                    </ul>
                  }
                </li>
              }
            </ul>
          }
        </div>
      </div>

      <!-- Botones centrales de acción -->
      <div class="d-flex flex-md-column justify-content-center align-items-center gap-2 o_dual_actions py-2">
        <button
          type="button"
          class="btn btn-sm btn-outline-primary o_btn_transfer"
          (click)="asignarMarcados()"
          [disabled]="deshabilitado() || marcadosIzq().size === 0"
          title="Asignar seleccionados"
          aria-label="Asignar seleccionados"
          data-btn-asignar>
          <pc-odoo-icon nombre="chevron-right" />
        </button>
        <button
          type="button"
          class="btn btn-sm btn-outline-primary o_btn_transfer"
          (click)="desasignarMarcados()"
          [disabled]="deshabilitado() || marcadosDer().size === 0"
          title="Quitar seleccionados"
          aria-label="Quitar seleccionados"
          data-btn-quitar>
          <pc-odoo-icon nombre="chevron-left" />
        </button>
        <button
          type="button"
          class="btn btn-sm btn-outline-secondary o_btn_transfer mt-md-2"
          (click)="asignarTodo()"
          [disabled]="deshabilitado() || totalDisponibles() === 0"
          title="Asignar todo"
          aria-label="Asignar todo"
          data-btn-asignar-todo>
          <span class="fw-bold">»</span>
        </button>
        <button
          type="button"
          class="btn btn-sm btn-outline-secondary o_btn_transfer"
          (click)="desasignarTodo()"
          [disabled]="deshabilitado() || totalAsignados() === 0"
          title="Quitar todo"
          aria-label="Quitar todo"
          data-btn-quitar-todo>
          <span class="fw-bold">«</span>
        </button>
      </div>

      <!-- Panel Derecho: Asignados -->
      <div class="o_dual_panel card flex-fill border shadow-sm" data-dual-panel="asignados">
        <div class="card-header bg-light py-2 px-3 d-flex justify-content-between align-items-center">
          <span class="fw-semibold small text-secondary">{{ tituloSeleccionados() }}</span>
          <span class="badge bg-primary-subtle text-primary border small" data-conteo-asignados>
            {{ totalAsignados() }}
          </span>
        </div>
        <div class="p-2 border-bottom bg-white">
          <div class="input-group input-group-sm">
            <span class="input-group-text bg-transparent border-end-0 text-muted">
              <pc-odoo-icon nombre="buscar" />
            </span>
            <input
              type="text"
              class="form-control border-start-0"
              placeholder="Buscar en asignados..."
              [ngModel]="filtroDer()"
              (ngModelChange)="filtroDer.set($event)"
              [disabled]="deshabilitado()"
              data-buscar-asignados />
          </div>
        </div>
        <div class="card-body p-2 o_tree_container overflow-auto" style="height: 340px;" tabindex="0" (keydown)="tecladoPanel($event, 'der')">
          @if (arbolAsignados().length === 0) {
            <div class="text-muted text-center py-4 small">No hay permisos asignados</div>
          } @else {
            <ul class="list-unstyled mb-0 o_tree_root">
              @for (mod of arbolAsignados(); track mod.modulo) {
                <li class="o_tree_node mb-1">
                  <div class="d-flex align-items-center gap-1 py-1 px-2 rounded o_node_header"
                       [class.o_selected]="estaModuloMarcado(mod, 'der')"
                       (dblclick)="moverModulo(mod, false)">
                    <button type="button" class="btn btn-sm btn-link p-0 text-muted border-0 text-decoration-none"
                            (click)="toggleExpandir('der-mod-' + mod.modulo, 'der')">
                      <pc-odoo-icon [nombre]="estaExpandido('der-mod-' + mod.modulo, 'der') ? 'chevron-down' : 'chevron-right'" />
                    </button>
                    <input type="checkbox" class="form-check-input mt-0 me-1"
                           [checked]="estaModuloMarcado(mod, 'der')"
                           [indeterminate]="esModuloParcial(mod, 'der')"
                           (change)="marcarModulo(mod, 'der', $any($event.target).checked)"
                           [disabled]="deshabilitado()"
                           [attr.data-check-modulo-asig]="mod.modulo" />
                    <span class="fw-semibold small text-dark flex-grow-1 user-select-none">{{ mod.etiqueta }}</span>
                  </div>

                  @if (estaExpandido('der-mod-' + mod.modulo, 'der')) {
                    <ul class="list-unstyled ms-3 ps-2 border-start o_tree_children">
                      @for (obj of mod.objetos; track obj.objeto) {
                        <li class="o_tree_node mb-1">
                          <div class="d-flex align-items-center gap-1 py-1 px-2 rounded o_node_header"
                               [class.o_selected]="estaObjetoMarcado(obj, 'der')"
                               (dblclick)="moverObjeto(obj, false)">
                            <button type="button" class="btn btn-sm btn-link p-0 text-muted border-0 text-decoration-none"
                                    (click)="toggleExpandir('der-obj-' + mod.modulo + '-' + obj.objeto, 'der')">
                              <pc-odoo-icon [nombre]="estaExpandido('der-obj-' + mod.modulo + '-' + obj.objeto, 'der') ? 'chevron-down' : 'chevron-right'" />
                            </button>
                            <input type="checkbox" class="form-check-input mt-0 me-1"
                                   [checked]="estaObjetoMarcado(obj, 'der')"
                                   [indeterminate]="esObjetoParcial(obj, 'der')"
                                   (change)="marcarObjeto(obj, 'der', $any($event.target).checked)"
                                   [disabled]="deshabilitado()"
                                   [attr.data-check-objeto-asig]="obj.objeto" />
                            <span class="small text-secondary flex-grow-1 user-select-none">{{ obj.etiqueta }}</span>
                          </div>

                          @if (estaExpandido('der-obj-' + mod.modulo + '-' + obj.objeto, 'der')) {
                            <ul class="list-unstyled ms-3 ps-2 border-start o_tree_leafs">
                              @for (act of obj.acciones; track act.clave) {
                                <li class="d-flex align-items-center gap-2 py-1 px-2 rounded o_node_leaf"
                                    [class.o_selected]="marcadosDer().has(act.clave)"
                                    (dblclick)="moverAccion(act.clave, false)"
                                    [attr.data-permiso-item-asig]="act.clave">
                                  <input type="checkbox" class="form-check-input mt-0"
                                         [checked]="marcadosDer().has(act.clave)"
                                         (change)="toggleMarcarAccion(act.clave, 'der')"
                                         [disabled]="deshabilitado()"
                                         [attr.data-check-permiso-asig]="act.clave" />
                                  <span class="small user-select-none">{{ act.etiqueta }}</span>
                                </li>
                              }
                            </ul>
                          }
                        </li>
                      }
                    </ul>
                  }
                </li>
              }
            </ul>
          }
        </div>
      </div>
    </div>
  `,
  styles: `
    .o_dual_list {
      width: 100%;
    }
    .o_dual_panel {
      min-width: 280px;
    }
    .o_tree_container:focus-visible {
      outline: 2px solid var(--brand-primary, #714B67);
      outline-offset: -1px;
    }
    .o_node_header:hover, .o_node_leaf:hover {
      background-color: rgba(113, 75, 103, 0.08);
      cursor: pointer;
    }
    .o_selected {
      background-color: rgba(113, 75, 103, 0.14) !important;
    }
    .o_btn_transfer {
      width: 38px;
      height: 38px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      padding: 0;
    }
    .o_disabled {
      opacity: 0.65;
      pointer-events: none;
    }
  `,
})
export class OdooDualList {
  readonly arbol = input.required<ModuloPermisoItem[]>();
  readonly seleccionados = model<string[]>([]);
  readonly deshabilitado = input(false);
  readonly tituloDisponibles = input('Permisos disponibles');
  readonly tituloSeleccionados = input('Permisos asignados');

  protected readonly filtroIzq = signal('');
  protected readonly filtroDer = signal('');

  readonly marcadosIzq = signal<Set<string>>(new Set());
  readonly marcadosDer = signal<Set<string>>(new Set());

  private readonly colapsadosIzq = signal<Set<string>>(new Set());
  private readonly colapsadosDer = signal<Set<string>>(new Set());

  // Conjunto de claves actualmente asignadas
  private readonly conjuntoAsignados = computed(() => new Set(this.seleccionados()));

  // Árbol filtrado para el panel disponible (claves no asignadas)
  readonly arbolDisponibles = computed(() => {
    const asignados = this.conjuntoAsignados();
    const filtro = this.filtroIzq().trim().toLowerCase();
    return this.filtrarYConstruirArbol(this.arbol(), clave => !asignados.has(clave), filtro);
  });

  // Árbol filtrado para el panel asignado (claves asignadas)
  readonly arbolAsignados = computed(() => {
    const asignados = this.conjuntoAsignados();
    const filtro = this.filtroDer().trim().toLowerCase();
    return this.filtrarYConstruirArbol(this.arbol(), clave => asignados.has(clave), filtro);
  });

  readonly totalDisponibles = computed(() => {
    let cuenta = 0;
    for (const m of this.arbolDisponibles()) {
      for (const o of m.objetos) cuenta += o.acciones.length;
    }
    return cuenta;
  });

  readonly totalAsignados = computed(() => {
    let cuenta = 0;
    for (const m of this.arbolAsignados()) {
      for (const o of m.objetos) cuenta += o.acciones.length;
    }
    return cuenta;
  });

  private filtrarYConstruirArbol(
    modulos: ModuloPermisoItem[],
    filtroAsignacion: (clave: string) => boolean,
    textoFiltro: string
  ): NodoVisibleModulo[] {
    const resultado: NodoVisibleModulo[] = [];

    for (const m of modulos) {
      const objetosVisibles: NodoVisibleObjeto[] = [];
      const modCoincide = !textoFiltro || m.etiqueta.toLowerCase().includes(textoFiltro);

      for (const o of m.objetos) {
        const objCoincide = modCoincide || (!textoFiltro || o.etiqueta.toLowerCase().includes(textoFiltro));
        const accionesValidas = o.acciones.filter(a => {
          if (!filtroAsignacion(a.clave)) return false;
          if (objCoincide) return true;
          return a.etiqueta.toLowerCase().includes(textoFiltro) || a.clave.toLowerCase().includes(textoFiltro);
        });

        if (accionesValidas.length > 0) {
          objetosVisibles.push({
            objeto: o.objeto,
            etiqueta: o.etiqueta,
            tipo: o.tipo,
            acciones: accionesValidas,
          });
        }
      }

      if (objetosVisibles.length > 0) {
        resultado.push({
          modulo: m.modulo,
          etiqueta: m.etiqueta,
          objetos: objetosVisibles,
        });
      }
    }

    return resultado;
  }

  // --- Manejo de colapso y expansión ---
  estaExpandido(id: string, panel: 'izq' | 'der'): boolean {
    const colapsados = panel === 'izq' ? this.colapsadosIzq() : this.colapsadosDer();
    return !colapsados.has(id); // Por omisión expandidos
  }

  toggleExpandir(id: string, panel: 'izq' | 'der'): void {
    const setColapsados = panel === 'izq' ? this.colapsadosIzq : this.colapsadosDer;
    setColapsados.update(prev => {
      const nuevo = new Set(prev);
      if (nuevo.has(id)) nuevo.delete(id);
      else nuevo.add(id);
      return nuevo;
    });
  }

  // --- Selección / Marcado con checkboxes ---
  toggleMarcarAccion(clave: string, panel: 'izq' | 'der'): void {
    const set = panel === 'izq' ? this.marcadosIzq : this.marcadosDer;
    set.update(prev => {
      const nuevo = new Set(prev);
      if (nuevo.has(clave)) nuevo.delete(clave);
      else nuevo.add(clave);
      return nuevo;
    });
  }

  marcarObjeto(obj: NodoVisibleObjeto, panel: 'izq' | 'der', marcar: boolean): void {
    const set = panel === 'izq' ? this.marcadosIzq : this.marcadosDer;
    set.update(prev => {
      const nuevo = new Set(prev);
      for (const a of obj.acciones) {
        if (marcar) nuevo.add(a.clave);
        else nuevo.delete(a.clave);
      }
      return nuevo;
    });
  }

  marcarModulo(mod: NodoVisibleModulo, panel: 'izq' | 'der', marcar: boolean): void {
    const set = panel === 'izq' ? this.marcadosIzq : this.marcadosDer;
    set.update(prev => {
      const nuevo = new Set(prev);
      for (const o of mod.objetos) {
        for (const a of o.acciones) {
          if (marcar) nuevo.add(a.clave);
          else nuevo.delete(a.clave);
        }
      }
      return nuevo;
    });
  }

  estaObjetoMarcado(obj: NodoVisibleObjeto, panel: 'izq' | 'der'): boolean {
    const marcados = panel === 'izq' ? this.marcadosIzq() : this.marcadosDer();
    if (obj.acciones.length === 0) return false;
    return obj.acciones.every(a => marcados.has(a.clave));
  }

  esObjetoParcial(obj: NodoVisibleObjeto, panel: 'izq' | 'der'): boolean {
    const marcados = panel === 'izq' ? this.marcadosIzq() : this.marcadosDer();
    const count = obj.acciones.filter(a => marcados.has(a.clave)).length;
    return count > 0 && count < obj.acciones.length;
  }

  estaModuloMarcado(mod: NodoVisibleModulo, panel: 'izq' | 'der'): boolean {
    const marcados = panel === 'izq' ? this.marcadosIzq() : this.marcadosDer();
    let total = 0;
    let marcadosCount = 0;
    for (const o of mod.objetos) {
      for (const a of o.acciones) {
        total++;
        if (marcados.has(a.clave)) marcadosCount++;
      }
    }
    return total > 0 && marcadosCount === total;
  }

  esModuloParcial(mod: NodoVisibleModulo, panel: 'izq' | 'der'): boolean {
    const marcados = panel === 'izq' ? this.marcadosIzq() : this.marcadosDer();
    let total = 0;
    let marcadosCount = 0;
    for (const o of mod.objetos) {
      for (const a of o.acciones) {
        total++;
        if (marcados.has(a.clave)) marcadosCount++;
      }
    }
    return marcadosCount > 0 && marcadosCount < total;
  }

  // --- Movimientos / Transferencias ---
  asignarMarcados(): void {
    if (this.deshabilitado()) return;
    const porMover = Array.from(this.marcadosIzq());
    if (porMover.length === 0) return;

    const conjunto = new Set(this.seleccionados());
    for (const c of porMover) conjunto.add(c);
    this.seleccionados.set(Array.from(conjunto));
    this.marcadosIzq.set(new Set());
  }

  desasignarMarcados(): void {
    if (this.deshabilitado()) return;
    const porQuitar = this.marcadosDer();
    if (porQuitar.size === 0) return;

    this.seleccionados.set(this.seleccionados().filter(c => !porQuitar.has(c)));
    this.marcadosDer.set(new Set());
  }

  asignarTodo(): void {
    if (this.deshabilitado()) return;
    const conjunto = new Set(this.seleccionados());
    for (const m of this.arbolDisponibles()) {
      for (const o of m.objetos) {
        for (const a of o.acciones) conjunto.add(a.clave);
      }
    }
    this.seleccionados.set(Array.from(conjunto));
    this.marcadosIzq.set(new Set());
  }

  desasignarTodo(): void {
    if (this.deshabilitado()) return;
    const enPanelDer = new Set<string>();
    for (const m of this.arbolAsignados()) {
      for (const o of m.objetos) {
        for (const a of o.acciones) enPanelDer.add(a.clave);
      }
    }
    this.seleccionados.set(this.seleccionados().filter(c => !enPanelDer.has(c)));
    this.marcadosDer.set(new Set());
  }

  moverAccion(clave: string, haciaAsignados: boolean): void {
    if (this.deshabilitado()) return;
    if (haciaAsignados) {
      const conjunto = new Set(this.seleccionados());
      conjunto.add(clave);
      this.seleccionados.set(Array.from(conjunto));
      this.marcadosIzq.update(s => {
        const n = new Set(s);
        n.delete(clave);
        return n;
      });
    } else {
      this.seleccionados.set(this.seleccionados().filter(c => c !== clave));
      this.marcadosDer.update(s => {
        const n = new Set(s);
        n.delete(clave);
        return n;
      });
    }
  }

  moverObjeto(obj: NodoVisibleObjeto, haciaAsignados: boolean): void {
    if (this.deshabilitado()) return;
    const claves = obj.acciones.map(a => a.clave);
    if (haciaAsignados) {
      const conjunto = new Set(this.seleccionados());
      for (const c of claves) conjunto.add(c);
      this.seleccionados.set(Array.from(conjunto));
    } else {
      const conjuntoQuitar = new Set(claves);
      this.seleccionados.set(this.seleccionados().filter(c => !conjuntoQuitar.has(c)));
    }
  }

  moverModulo(mod: NodoVisibleModulo, haciaAsignados: boolean): void {
    if (this.deshabilitado()) return;
    const claves: string[] = [];
    for (const o of mod.objetos) {
      for (const a of o.acciones) claves.push(a.clave);
    }
    if (haciaAsignados) {
      const conjunto = new Set(this.seleccionados());
      for (const c of claves) conjunto.add(c);
      this.seleccionados.set(Array.from(conjunto));
    } else {
      const conjuntoQuitar = new Set(claves);
      this.seleccionados.set(this.seleccionados().filter(c => !conjuntoQuitar.has(c)));
    }
  }

  tecladoPanel(event: KeyboardEvent, panel: 'izq' | 'der'): void {
    if (event.key === 'Enter' || (event.altKey && (event.key === 'ArrowRight' || event.key === 'ArrowLeft'))) {
      event.preventDefault();
      if (panel === 'izq') this.asignarMarcados();
      else this.desasignarMarcados();
    }
  }
}
