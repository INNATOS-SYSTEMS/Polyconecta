import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { fechaHora } from '../../../core/format/numero';
import { OrigenEnMemoria } from '../../../core/lista/origen-en-memoria';
import { SesionState } from '../../../core/sesion/sesion-state';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { AvisosService } from '../../../shared/odoo-dialog/avisos';
import { OdooIcon } from '../../../shared/odoo-icon/odoo-icon';
import { OdooList } from '../../../shared/odoo-list/odoo-list';
import { OdooSeleccion } from '../../../shared/odoo-list/odoo-seleccion';
import { AccionMasiva, ColumnaLista } from '../../../shared/odoo-list/columnas';
import { CatalogosService, EstadoCatalogoDto } from '../../catalogos/catalogos.service';

const NOMBRES: Record<string, string> = { productos: 'Productos', clientes: 'Clientes', agentes: 'Agentes', almacenes: 'Almacenes' };
const nombre = (c: string) => NOMBRES[c.toLowerCase()] ?? c;
const fecha = (f: string | null) => (f ? fechaHora(new Date(f)) : 'Nunca');
const entero = (n: number) => n.toLocaleString('en-US');

/**
 * Estado de la sincronización de catálogos con CONTPAQi (FR-014, P-14 de la propuesta aprobada): la lista
 * de los contratos visuales. "Sincronizar todo" es la acción primaria; "Sincronizar ahora" es una acción de
 * la barra de selección. Ejecutar exige Sistemas o Administrador.
 */
@Component({
  selector: 'pc-sincronizacion',
  imports: [OdooBreadcrumb, OdooList, OdooSeleccion, OdooIcon],
  template: `
    <div class="o_control_panel">
      <div class="d-flex align-items-center gap-3">
        @if (puedeEjecutar()) {
          <button class="btn btn-primary btn-sm fw-bold px-3" [disabled]="!!sincronizando()" (click)="sincronizarTodo()" data-sincronizar-todo>
            <pc-odoo-icon [nombre]="sincronizando() === 'todo' ? 'cargando' : 'reintentar'" /> Sincronizar todo
          </button>
        }
        <pc-odoo-breadcrumb [items]="[{ label: 'Sincronización' }]" />
      </div>
      <div class="d-flex align-items-center gap-2"><pc-odoo-seleccion [lista]="tabla" /></div>
      <div class="d-flex align-items-center gap-2"></div>
    </div>
    <div class="p-4">
      @if (error()) {
        <div class="alert alert-danger py-2 px-3 small mb-3" role="alert"><pc-odoo-icon nombre="hard-stop" />{{ error() }}</div>
      }
      <pc-odoo-list #tabla lista="plataforma.sincronizacion" [origen]="origen()" [columnas]="columnas" [idDe]="idDe" [acciones]="acciones()"
                    [filasPulsables]="false" [conPaginador]="false" mensajeVacio="No hay catálogos sincronizados todavía." />
    </div>
  `,
  styles: ':host { display: contents; }',
})
export class Sincronizacion implements OnInit {
  private readonly catalogos = inject(CatalogosService);
  private readonly sesion = inject(SesionState);
  private readonly avisos = inject(AvisosService);

  protected readonly estados = signal<EstadoCatalogoDto[]>([]);
  protected readonly sincronizando = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly puedeEjecutar = computed(() => this.sesion.tienePermiso('plataforma.sincronizacion.ejecutar'));

  protected readonly idDe = (e: EstadoCatalogoDto) => e.catalogo;
  /** Un origen nuevo con cada corrida: la lista vuelve a consultar. */
  protected readonly origen = computed(() => {
    const l = this.estados();
    return new OrigenEnMemoria<EstadoCatalogoDto>({ datos: () => l, id: e => e.catalogo });
  });
  protected readonly columnas: ColumnaLista<EstadoCatalogoDto>[] = [
    { campo: 'catalogo', titulo: 'Catálogo', clase: 'fw-semibold text-primary', texto: e => nombre(e.catalogo) },
    { campo: 'resultado', titulo: 'Estado', tipo: 'estado', texto: e => (e.error || e.resultado === 'Error' ? 'Error' : e.resultado ? 'Correcta' : 'Sin corridas') },
    { campo: 'ultimaCorrida', titulo: 'Última corrida', texto: e => fecha(e.ultimaCorrida) },
    { campo: 'ultimaExitosa', titulo: 'Última correcta', texto: e => fecha(e.ultimaExitosa) },
    { campo: 'leidos', titulo: 'Leídos', clase: 'text-end', texto: e => entero(e.leidos) },
    { campo: 'cambiados', titulo: 'Cambiados', clase: 'text-end', texto: e => entero(e.cambiados) },
    { campo: 'archivados', titulo: 'Archivados', clase: 'text-end', texto: e => entero(e.archivados) },
    { campo: 'duracionMs', titulo: 'Duración', clase: 'text-end', texto: e => `${(e.duracionMs / 1000).toFixed(1)} s` },
    { campo: 'error', titulo: 'Último error', texto: e => e.error ?? '' },
  ];
  protected readonly acciones = computed<AccionMasiva[]>(() =>
    this.puedeEjecutar() ? [{ nombre: 'Sincronizar ahora', icono: 'reintentar', ejecutar: ids => void this.sincronizar(ids) }] : []);

  async ngOnInit(): Promise<void> {
    try {
      this.estados.set(await this.catalogos.obtenerEstadoSincronizacion());
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo leer el estado de la sincronización.');
    }
  }

  protected async sincronizarTodo(): Promise<void> {
    this.sincronizando.set('todo');
    this.error.set(null);
    try {
      this.estados.set(await this.catalogos.sincronizarTodo());
      this.avisos.exito('Sincronización de todos los catálogos terminada.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo sincronizar.');
    } finally {
      this.sincronizando.set(null);
    }
  }

  private async sincronizar(catalogos: string[]): Promise<void> {
    this.error.set(null);
    for (const c of catalogos) {
      this.sincronizando.set(c);
      try {
        const r = await this.catalogos.sincronizarCatalogo(c);
        this.estados.update(l => l.map(e => (e.catalogo.toLowerCase() === c.toLowerCase() ? r : e)));
        this.avisos.exito(`${nombre(c)} sincronizado.`);
      } catch (e: unknown) {
        this.error.set((e as Error).message || `No se pudo sincronizar ${nombre(c)}.`);
      }
    }
    this.sincronizando.set(null);
  }
}
