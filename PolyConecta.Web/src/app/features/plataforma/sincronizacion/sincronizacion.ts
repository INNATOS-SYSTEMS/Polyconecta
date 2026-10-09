import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { OdooBreadcrumb } from '../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { SesionState } from '../../../core/sesion/sesion-state';
import { CatalogosService, EstadoCatalogoDto } from '../../catalogos/catalogos.service';

@Component({
  selector: 'pc-sincronizacion',
  imports: [OdooBreadcrumb],
  templateUrl: './sincronizacion.html',
  styles: `
    :host { display: block; }
    .o_form_view {
      padding: 1.5rem 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }
    .o_form_sheet {
      background: white;
      border: 1px solid var(--border-color, #e2e8f0);
      border-radius: 8px;
      padding: 2rem;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }
  `,
})
export class Sincronizacion implements OnInit {
  private readonly catalogos = inject(CatalogosService);
  private readonly sesion = inject(SesionState);

  protected readonly cargando = signal(true);
  protected readonly sincronizando = signal<string | null>(null); // 'todo' o nombre de catálogo
  protected readonly error = signal<string | null>(null);
  protected readonly exito = signal<string | null>(null);

  protected readonly estados = signal<EstadoCatalogoDto[]>([]);

  protected readonly puedeEjecutar = computed(() =>
    this.sesion.tienePermiso('plataforma.sincronizacion.ejecutar')
  );

  async ngOnInit(): Promise<void> {
    await this.cargarEstados();
  }

  protected async cargarEstados(): Promise<void> {
    this.cargando.set(true);
    this.error.set(null);
    try {
      const res = await this.catalogos.obtenerEstadoSincronizacion();
      this.estados.set(res);
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al obtener estado de sincronización.');
    } finally {
      this.cargando.set(false);
    }
  }

  protected async sincronizarTodo(): Promise<void> {
    this.sincronizando.set('todo');
    this.error.set(null);
    this.exito.set(null);
    try {
      const res = await this.catalogos.sincronizarTodo();
      this.estados.set(res);
      this.exito.set('Sincronización completa de todos los catálogos finalizada.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al ejecutar sincronización completa.');
    } finally {
      this.sincronizando.set(null);
    }
  }

  protected async sincronizarCatalogo(catalogo: string): Promise<void> {
    this.sincronizando.set(catalogo);
    this.error.set(null);
    this.exito.set(null);
    try {
      const res = await this.catalogos.sincronizarCatalogo(catalogo);
      this.estados.update(lista => {
        const idx = lista.findIndex(e => e.catalogo.toLowerCase() === catalogo.toLowerCase());
        if (idx >= 0) {
          const nueva = [...lista];
          nueva[idx] = res;
          return nueva;
        }
        return [...lista, res];
      });
      this.exito.set(`Sincronización del catálogo "${this.nombreCatalogo(catalogo)}" completada.`);
    } catch (e: unknown) {
      this.error.set((e as Error).message || `Error al sincronizar catálogo "${catalogo}".`);
    } finally {
      this.sincronizando.set(null);
    }
  }

  protected nombreCatalogo(cat: string): string {
    const mapa: Record<string, string> = {
      productos: 'Productos',
      clientes: 'Clientes',
      agentes: 'Agentes',
      almacenes: 'Almacenes',
    };
    return mapa[cat.toLowerCase()] ?? cat;
  }

  protected formatearFecha(f: string | null): string {
    if (!f) return 'Nunca';
    try {
      const d = new Date(f);
      return isNaN(d.getTime()) ? f : d.toLocaleString();
    } catch {
      return f;
    }
  }
}
