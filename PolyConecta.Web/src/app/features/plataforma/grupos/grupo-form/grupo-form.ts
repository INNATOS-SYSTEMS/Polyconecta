import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { OrigenEnMemoria } from '../../../../core/lista/origen-en-memoria';
import { HojaRegistro } from '../../../../shared/hoja-registro/hoja-registro';
import { AccionMenu } from '../../../../shared/odoo-action-menu/odoo-action-menu';
import { AvisosService } from '../../../../shared/odoo-dialog/avisos';
import { OdooDualList } from '../../../../shared/odoo-dual-list/odoo-dual-list';
import { ModuloPermisoItem } from '../../../../shared/odoo-dual-list/odoo-dual-list.types';
import { OdooMaestro } from '../../../../shared/odoo-maestro/odoo-maestro';
import { OdooMany2one } from '../../../../shared/odoo-many2one/odoo-many2one';
import { SmartButtonModel } from '../../../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { OdooTabs, PcPestana } from '../../../../shared/odoo-tabs/odoo-tabs';
import { GrupoDetalleDto, SeguridadService } from '../../seguridad.service';

interface GrupoItem { id: number; codigo: string; nombre: string }

/**
 * Grupo y sus permisos en dos paneles (D-148, P-07 y P-08 de la propuesta aprobada): el selector dual vive
 * en la pestaña "Permisos". En el alta, "Copiar permisos de" llena el panel de asignados. Se edita en su
 * lugar (D-164).
 */
@Component({
  selector: 'pc-grupo-form',
  imports: [FormsModule, HojaRegistro, OdooMaestro, OdooMany2one, OdooTabs, PcPestana, OdooDualList],
  templateUrl: './grupo-form.html',
  styles: ':host { display: block; }',
})
export class GrupoForm implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly seguridad = inject(SeguridadService);
  private readonly avisos = inject(AvisosService);

  protected readonly idRuta = this.route.snapshot.paramMap.get('id') ?? 'nuevo';
  protected readonly nuevo = this.idRuta === 'nuevo';

  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly sucio = signal(false);
  protected readonly guardado = signal<GrupoDetalleDto | null>(null);

  protected readonly codigo = signal('');
  protected readonly nombre = signal('');
  protected readonly descripcion = signal('');
  protected readonly permisos = signal<string[]>([]);
  protected readonly copiarDe = signal<GrupoItem | null>(null);

  protected readonly arbol = signal<ModuloPermisoItem[]>([]);
  protected readonly grupos = signal<GrupoItem[]>([]);

  protected readonly pestanas = [{ id: 'permisos', titulo: 'Permisos' }];
  protected readonly origenGrupos = computed(() => { const l = this.grupos(); return new OrigenEnMemoria<GrupoItem>({ datos: () => l, id: g => String(g.id), buscables: ['codigo', 'nombre'] }); });
  protected readonly textoGrupo = (g: GrupoItem) => g.nombre;
  protected readonly idGrupo = (g: GrupoItem) => String(g.id);

  protected readonly acciones = computed<AccionMenu[]>(() => {
    const g = this.guardado();
    return g ? [{ nombre: g.activo ? 'Archivar' : 'Restaurar', icono: 'archivar', ejecutar: () => void this.cambiarEstado(g.activo) }] : [];
  });

  protected readonly botones = computed<SmartButtonModel[]>(() => {
    const g = this.guardado();
    return g ? [{ label: g.miembros === 1 ? 'Usuario' : 'Usuarios', countBadge: g.miembros, iconClass: 'usuario', targetRoute: '' }] : [];
  });

  async ngOnInit(): Promise<void> {
    try {
      const [arbol, grupos] = await Promise.all([this.seguridad.obtenerPermisos(), this.seguridad.listarGruposCatalogo()]);
      this.arbol.set(arbol);
      this.grupos.set(grupos);
      if (!this.nuevo) this.cargar(await this.seguridad.obtenerGrupo(this.idRuta));
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo cargar el grupo.');
    } finally {
      this.cargando.set(false);
    }
  }

  private cargar(g: GrupoDetalleDto): void {
    this.guardado.set(g);
    this.codigo.set(g.codigo);
    this.nombre.set(g.nombre);
    this.descripcion.set(g.descripcion ?? '');
    this.permisos.set([...g.permisos]);
    this.sucio.set(false);
  }

  protected cambiar<T>(campo: { set(v: T): void }, valor: T): void {
    campo.set(valor);
    this.sucio.set(true);
  }

  /** En el alta, copiar los permisos de otro grupo llena el panel de asignados (US2, escenario 7). */
  protected async copiar(g: GrupoItem | null): Promise<void> {
    this.copiarDe.set(g);
    if (!g) return;
    try {
      this.permisos.set([...((await this.seguridad.obtenerGrupo(g.id)).permisos ?? [])]);
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudieron copiar los permisos.');
    }
  }

  protected async guardar(): Promise<void> {
    this.error.set(null);
    if (!this.codigo().trim() || !this.nombre().trim()) {
      this.error.set('El código y el nombre del grupo son obligatorios.');
      return;
    }
    this.guardando.set(true);
    try {
      if (this.nuevo) {
        const creado = await this.seguridad.crearGrupo({
          codigo: this.codigo().trim().toUpperCase(), nombre: this.nombre().trim(),
          descripcion: this.descripcion().trim() || null, copiarDe: this.copiarDe()?.id ?? null,
        });
        const copiados = creado.permisos ?? [];
        const iguales = copiados.length === this.permisos().length && copiados.every(p => this.permisos().includes(p));
        if (!iguales) {
          await this.seguridad.editarGrupo(creado.id, {
            rowVersion: creado.rowVersion, nombre: creado.nombre, descripcion: creado.descripcion, permisos: this.permisos(),
          });
        }
        void this.router.navigateByUrl(`/plataforma/grupos/${creado.id}`);
        return;
      }
      const g = this.guardado()!;
      this.cargar(await this.seguridad.editarGrupo(g.id, {
        rowVersion: g.rowVersion, nombre: this.nombre().trim(), descripcion: this.descripcion().trim() || null, permisos: this.permisos(),
      }));
      this.avisos.exito('Grupo guardado.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo guardar el grupo.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected descartar(): void {
    this.error.set(null);
    const g = this.guardado();
    if (g) this.cargar(g);
    else void this.router.navigateByUrl('/plataforma/grupos');
  }

  private async cambiarEstado(archivar: boolean): Promise<void> {
    const g = this.guardado();
    if (!g) return;
    try {
      this.cargar(archivar ? await this.seguridad.archivarGrupo(g.id) : await this.seguridad.restaurarGrupo(g.id));
      this.avisos.exito(archivar ? 'Grupo archivado.' : 'Grupo restaurado.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo cambiar el estado.');
    }
  }
}
