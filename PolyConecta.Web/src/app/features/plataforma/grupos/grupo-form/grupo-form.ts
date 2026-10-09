import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { OdooBreadcrumb } from '../../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooDualList } from '../../../../shared/odoo-dual-list/odoo-dual-list';
import { ModuloPermisoItem } from '../../../../shared/odoo-dual-list/odoo-dual-list.types';
import { GrupoDetalleDto, SeguridadService } from '../../seguridad.service';

@Component({
  selector: 'pc-grupo-form',
  imports: [FormsModule, OdooBreadcrumb, OdooDualList],
  templateUrl: './grupo-form.html',
  styles: `
    :host { display: block; }
    .o_form_view {
      padding: 1.5rem 2rem;
      max-width: 1080px;
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
export class GrupoForm implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly seguridad = inject(SeguridadService);

  protected readonly id = signal<string>(this.route.snapshot.paramMap.get('id') ?? 'nuevo');
  protected readonly esNuevo = computed(() => this.id() === 'nuevo');

  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly exito = signal<string | null>(null);

  // Campos del grupo
  protected readonly codigo = signal('');
  protected readonly nombre = signal('');
  protected readonly descripcion = signal('');
  protected readonly activo = signal(true);
  protected readonly rowVersion = signal('');
  protected readonly miembros = signal(0);
  protected readonly permisosAsignados = signal<string[]>([]);

  // Para modo nuevo
  protected readonly copiarDe = signal<number | null>(null);
  protected readonly gruposDisponibles = signal<{ id: number; codigo: string; nombre: string }[]>([]);

  protected actualizarCopiarDe(valor: unknown): void {
    this.copiarDe.set(valor ? Number(valor) : null);
  }

  // Catálogo completo de permisos
  protected readonly arbolPermisos = signal<ModuloPermisoItem[]>([]);

  protected readonly tituloMiga = computed(() => {
    if (this.esNuevo()) return 'Nuevo grupo';
    return this.nombre() || this.codigo() || 'Grupo';
  });

  async ngOnInit(): Promise<void> {
    this.cargando.set(true);
    this.error.set(null);
    try {
      const [permisos, catalogoGrupos] = await Promise.all([
        this.seguridad.obtenerPermisos(),
        this.seguridad.listarGruposCatalogo(),
      ]);
      this.arbolPermisos.set(permisos);
      this.gruposDisponibles.set(catalogoGrupos);

      if (!this.esNuevo()) {
        const g = await this.seguridad.obtenerGrupo(this.id());
        this.cargarDatosGrupo(g);
      }
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al cargar grupo');
    } finally {
      this.cargando.set(false);
    }
  }

  private cargarDatosGrupo(g: GrupoDetalleDto): void {
    this.codigo.set(g.codigo);
    this.nombre.set(g.nombre);
    this.descripcion.set(g.descripcion ?? '');
    this.activo.set(g.activo);
    this.rowVersion.set(g.rowVersion);
    this.miembros.set(g.miembros);
    this.permisosAsignados.set([...g.permisos]);
  }

  protected async guardar(): Promise<void> {
    this.error.set(null);
    this.exito.set(null);

    if (!this.codigo().trim() || !this.nombre().trim()) {
      this.error.set('El código y el nombre del grupo son obligatorios.');
      return;
    }

    this.guardando.set(true);
    try {
      if (this.esNuevo()) {
        const creado = await this.seguridad.crearGrupo({
          codigo: this.codigo().trim().toUpperCase(),
          nombre: this.nombre().trim(),
          descripcion: this.descripcion().trim() || null,
          copiarDe: this.copiarDe() ? Number(this.copiarDe()) : null,
        });
        void this.router.navigateByUrl(`/plataforma/grupos/${creado.id}`);
      } else {
        const actualizado = await this.seguridad.editarGrupo(this.id(), {
          rowVersion: this.rowVersion(),
          nombre: this.nombre().trim(),
          descripcion: this.descripcion().trim() || null,
          permisos: this.permisosAsignados(),
        });
        this.cargarDatosGrupo(actualizado);
        this.exito.set('Grupo guardado exitosamente.');
      }
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al guardar el grupo.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected async cambiarEstado(archivar: boolean): Promise<void> {
    this.guardando.set(true);
    this.error.set(null);
    try {
      const g = archivar
        ? await this.seguridad.archivarGrupo(this.id())
        : await this.seguridad.restaurarGrupo(this.id());
      this.cargarDatosGrupo(g);
      this.exito.set(archivar ? 'Grupo archivado.' : 'Grupo restaurado.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al cambiar estado.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected descartar(): void {
    void this.router.navigateByUrl('/plataforma/grupos');
  }
}
