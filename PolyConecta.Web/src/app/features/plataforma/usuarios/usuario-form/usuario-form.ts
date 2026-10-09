import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { OdooBreadcrumb } from '../../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { OdooIcon } from '../../../../shared/odoo-icon/odoo-icon';
import {
  AgenteItem,
  PlantaItem,
  SeguridadService,
  UsuarioDetalleDto,
} from '../../seguridad.service';

interface FilaAsignacion {
  grupoId: number;
  plantaId: number;
  suplente: boolean;
}

@Component({
  selector: 'pc-usuario-form',
  imports: [FormsModule, OdooBreadcrumb, OdooIcon],
  templateUrl: './usuario-form.html',
  styles: `
    :host { display: block; }
    .o_form_view {
      padding: 1.5rem 2rem;
      max-width: 980px;
      margin: 0 auto;
    }
    .o_form_sheet {
      background: white;
      border: 1px solid var(--border-color, #e2e8f0);
      border-radius: 8px;
      padding: 2rem;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }
    .table td, .table th {
      vertical-align: middle;
    }
  `,
})
export class UsuarioForm implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly seguridad = inject(SeguridadService);

  protected readonly id = signal<string>(this.route.snapshot.paramMap.get('id') ?? 'nuevo');
  protected readonly esNuevo = computed(() => this.id() === 'nuevo');

  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly exito = signal<string | null>(null);

  // Campos del usuario
  protected readonly usuario = signal('');
  protected readonly nombre = signal('');
  protected readonly email = signal('');
  protected readonly contrasena = signal('');
  protected readonly agenteId = signal<number | null>(null);
  protected readonly activo = signal(true);
  protected readonly rowVersion = signal('');
  protected readonly asignaciones = signal<FilaAsignacion[]>([]);

  // Catálogos auxiliares
  protected readonly plantas = signal<PlantaItem[]>([]);
  protected readonly grupos = signal<{ id: number; codigo: string; nombre: string }[]>([]);
  protected readonly agentes = signal<AgenteItem[]>([]);

  protected readonly tituloMiga = computed(() => {
    if (this.esNuevo()) return 'Nuevo usuario';
    return this.nombre() || this.usuario() || 'Usuario';
  });

  async ngOnInit(): Promise<void> {
    this.cargando.set(true);
    this.error.set(null);
    try {
      const [plantas, grupos, agentes] = await Promise.all([
        this.seguridad.obtenerPlantas(),
        this.seguridad.listarGruposCatalogo(),
        this.seguridad.obtenerAgentes().catch(() => []),
      ]);
      this.plantas.set(plantas);
      this.grupos.set(grupos);
      this.agentes.set(agentes);

      if (!this.esNuevo()) {
        const u = await this.seguridad.obtenerUsuario(this.id());
        this.cargarDatosUsuario(u);
      } else {
        // Asignación por omisión si hay grupos y plantas
        if (grupos.length > 0 && plantas.length > 0) {
          this.asignaciones.set([{ grupoId: grupos[0].id, plantaId: plantas[0].id, suplente: false }]);
        }
      }
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al cargar usuario');
    } finally {
      this.cargando.set(false);
    }
  }

  private cargarDatosUsuario(u: UsuarioDetalleDto): void {
    this.usuario.set(u.usuario);
    this.nombre.set(u.nombre);
    this.email.set(u.email ?? '');
    this.activo.set(u.activo);
    this.agenteId.set(u.agenteId);
    this.rowVersion.set(u.rowVersion);
    this.asignaciones.set(
      u.asignaciones.map(a => ({
        grupoId: a.grupoId,
        plantaId: a.plantaId,
        suplente: a.suplente,
      }))
    );
  }

  protected agregarAsignacion(): void {
    const primerGrupo = this.grupos()[0]?.id ?? 0;
    const primeraPlanta = this.plantas()[0]?.id ?? 0;
    this.asignaciones.update(lista => [
      ...lista,
      { grupoId: primerGrupo, plantaId: primeraPlanta, suplente: false },
    ]);
  }

  protected quitarAsignacion(index: number): void {
    this.asignaciones.update(lista => lista.filter((_, i) => i !== index));
  }

  protected actualizarAsignacionGrupo(index: number, grupoId: number): void {
    this.asignaciones.update(lista => {
      const nueva = [...lista];
      nueva[index] = { ...nueva[index], grupoId: Number(grupoId) };
      return nueva;
    });
  }

  protected actualizarAsignacionPlanta(index: number, plantaId: number): void {
    this.asignaciones.update(lista => {
      const nueva = [...lista];
      nueva[index] = { ...nueva[index], plantaId: Number(plantaId) };
      return nueva;
    });
  }

  protected actualizarAsignacionSuplente(index: number, suplente: boolean): void {
    this.asignaciones.update(lista => {
      const nueva = [...lista];
      nueva[index] = { ...nueva[index], suplente };
      return nueva;
    });
  }

  protected actualizarAgente(valor: unknown): void {
    this.agenteId.set(valor ? Number(valor) : null);
  }

  protected async guardar(): Promise<void> {
    this.error.set(null);
    this.exito.set(null);

    if (this.asignaciones().length === 0) {
      this.error.set('Un usuario activo debe tener al menos una asignación de grupo.');
      return;
    }

    this.guardando.set(true);
    try {
      if (this.esNuevo()) {
        if (!this.usuario().trim() || !this.nombre().trim() || !this.contrasena().trim()) {
          this.error.set('Usuario, nombre y contraseña son obligatorios.');
          this.guardando.set(false);
          return;
        }

        const creado = await this.seguridad.crearUsuario({
          usuario: this.usuario().trim(),
          nombre: this.nombre().trim(),
          email: this.email().trim() || null,
          contrasena: this.contrasena().trim(),
          asignaciones: this.asignaciones(),
        });

        if (this.agenteId() !== null) {
          await this.seguridad.ligarAgente(creado.id, this.agenteId());
        }

        void this.router.navigateByUrl(`/plataforma/usuarios/${creado.id}`);
      } else {
        const actualizado = await this.seguridad.editarUsuario(this.id(), {
          rowVersion: this.rowVersion(),
          nombre: this.nombre().trim(),
          email: this.email().trim() || null,
          asignaciones: this.asignaciones(),
        });

        // Actualizar agente de CONTPAQi si cambió
        if (this.agenteId() !== actualizado.agenteId) {
          const conAgente = await this.seguridad.ligarAgente(this.id(), this.agenteId());
          this.cargarDatosUsuario(conAgente);
        } else {
          this.cargarDatosUsuario(actualizado);
        }

        this.exito.set('Usuario guardado exitosamente.');
      }
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al guardar el usuario.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected async cambiarEstado(archivar: boolean): Promise<void> {
    this.guardando.set(true);
    this.error.set(null);
    try {
      const u = archivar
        ? await this.seguridad.archivarUsuario(this.id())
        : await this.seguridad.restaurarUsuario(this.id());
      this.cargarDatosUsuario(u);
      this.exito.set(archivar ? 'Usuario archivado.' : 'Usuario restaurado.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al cambiar estado.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected async restablecerContrasena(): Promise<void> {
    const nueva = prompt('Ingrese la nueva contraseña para el usuario:');
    if (!nueva) return;

    this.guardando.set(true);
    this.error.set(null);
    try {
      await this.seguridad.restablecerContrasena(this.id(), nueva);
      this.exito.set('Contraseña restablecida exitosamente.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al restablecer la contraseña.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected descartar(): void {
    void this.router.navigateByUrl('/plataforma/usuarios');
  }
}
