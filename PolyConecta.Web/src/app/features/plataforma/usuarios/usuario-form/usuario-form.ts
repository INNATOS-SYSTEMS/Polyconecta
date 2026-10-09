import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { OrigenEnMemoria } from '../../../../core/lista/origen-en-memoria';
import { HojaRegistro } from '../../../../shared/hoja-registro/hoja-registro';
import { AccionMenu } from '../../../../shared/odoo-action-menu/odoo-action-menu';
import { OdooDialog } from '../../../../shared/odoo-dialog/odoo-dialog';
import { AvisosService } from '../../../../shared/odoo-dialog/avisos';
import { OdooIcon } from '../../../../shared/odoo-icon/odoo-icon';
import { OdooMaestro } from '../../../../shared/odoo-maestro/odoo-maestro';
import { OdooMany2one } from '../../../../shared/odoo-many2one/odoo-many2one';
import { OdooTabs, PcPestana } from '../../../../shared/odoo-tabs/odoo-tabs';
import { AgenteItem, PlantaItem, SeguridadService, UsuarioDetalleDto } from '../../seguridad.service';

interface GrupoItem { id: number; codigo: string; nombre: string }
interface Tipo { suplente: boolean; nombre: string }
interface Asignacion { grupoId: number; plantaId: number; suplente: boolean }

const TIPOS: Tipo[] = [{ suplente: false, nombre: 'Titular' }, { suplente: true, nombre: 'Suplente' }];

/**
 * Usuario (FR-013, P-04 y P-05 de la propuesta aprobada): el formulario de registro con sus grupos por
 * planta como líneas de una pestaña, capturadas arriba de la tabla. Se edita en su lugar (D-164).
 */
@Component({
  selector: 'pc-usuario-form',
  imports: [FormsModule, HojaRegistro, OdooMaestro, OdooMany2one, OdooTabs, PcPestana, OdooDialog, OdooIcon],
  templateUrl: './usuario-form.html',
  styles: ':host { display: block; }',
})
export class UsuarioForm implements OnInit {
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
  protected readonly guardado = signal<UsuarioDetalleDto | null>(null);

  protected readonly usuario = signal('');
  protected readonly nombre = signal('');
  protected readonly email = signal('');
  protected readonly contrasena = signal('');
  protected readonly agente = signal<AgenteItem | null>(null);
  protected readonly asignaciones = signal<Asignacion[]>([]);

  protected readonly plantas = signal<PlantaItem[]>([]);
  protected readonly grupos = signal<GrupoItem[]>([]);
  protected readonly agentes = signal<AgenteItem[]>([]);

  // Captura de una asignación: [Grupo] [Planta] [Tipo] [Agregar]
  protected readonly capGrupo = signal<GrupoItem | null>(null);
  protected readonly capPlanta = signal<PlantaItem | null>(null);
  protected readonly capTipo = signal<Tipo>(TIPOS[0]);
  protected readonly capValida = computed(() => !!this.capGrupo() && !!this.capPlanta());

  protected readonly dialogoContrasena = signal(false);
  protected readonly nuevaContrasena = signal('');

  protected readonly pestanas = [{ id: 'grupos', titulo: 'Grupos y plantas' }];
  protected readonly origenGrupos = computed(() => { const l = this.grupos(); return new OrigenEnMemoria<GrupoItem>({ datos: () => l, id: g => String(g.id), buscables: ['codigo', 'nombre'] }); });
  protected readonly origenPlantas = computed(() => { const l = this.plantas(); return new OrigenEnMemoria<PlantaItem>({ datos: () => l, id: p => String(p.id), buscables: ['codigo', 'nombre'] }); });
  protected readonly origenAgentes = computed(() => { const l = this.agentes(); return new OrigenEnMemoria<AgenteItem>({ datos: () => l, id: a => String(a.id), buscables: ['codigo', 'nombre'] }); });
  protected readonly origenTipos = new OrigenEnMemoria<Tipo>({ datos: () => TIPOS, id: t => t.nombre, buscables: ['nombre'] });
  protected readonly textoGrupo = (g: GrupoItem) => g.nombre;
  protected readonly textoPlanta = (p: PlantaItem) => p.codigo;
  protected readonly textoAgente = (a: AgenteItem) => `(${a.codigo}) ${a.nombre}`;
  protected readonly textoTipo = (t: Tipo) => t.nombre;
  protected readonly idPorId = (r: { id: number }) => String(r.id);
  protected readonly idTipo = (t: Tipo) => t.nombre;

  protected readonly acciones = computed<AccionMenu[]>(() => {
    const u = this.guardado();
    if (!u) return [];
    return [
      { nombre: u.activo ? 'Archivar' : 'Restaurar', icono: 'archivar', ejecutar: () => void this.cambiarEstado(u.activo) },
      { nombre: 'Restablecer contraseña', icono: 'reintentar', ejecutar: () => this.pedirContrasena() },
    ];
  });

  protected grupoDe = (id: number) => this.grupos().find(g => g.id === id)?.nombre ?? `Grupo ${id}`;
  protected plantaDe = (id: number) => this.plantas().find(p => p.id === id)?.codigo ?? `Planta ${id}`;

  async ngOnInit(): Promise<void> {
    try {
      const [plantas, grupos, agentes] = await Promise.all([
        this.seguridad.obtenerPlantas(),
        this.seguridad.listarGruposCatalogo(),
        this.seguridad.obtenerAgentes().catch(() => []),
      ]);
      this.plantas.set(plantas);
      this.grupos.set(grupos);
      this.agentes.set(agentes);
      if (!this.nuevo) this.cargar(await this.seguridad.obtenerUsuario(this.idRuta));
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo cargar el usuario.');
    } finally {
      this.cargando.set(false);
    }
  }

  private cargar(u: UsuarioDetalleDto): void {
    this.guardado.set(u);
    this.usuario.set(u.usuario);
    this.nombre.set(u.nombre);
    this.email.set(u.email ?? '');
    this.agente.set(this.agentes().find(a => a.id === u.agenteId) ?? null);
    this.asignaciones.set(u.asignaciones.map(a => ({ grupoId: a.grupoId, plantaId: a.plantaId, suplente: a.suplente })));
    this.sucio.set(false);
  }

  protected cambiar<T>(campo: { set(v: T): void }, valor: T): void {
    campo.set(valor);
    this.sucio.set(true);
  }

  protected agregarAsignacion(): void {
    const g = this.capGrupo(), p = this.capPlanta();
    if (!g || !p) return;
    if (this.asignaciones().some(a => a.grupoId === g.id && a.plantaId === p.id)) {
      this.error.set(`${g.nombre} · ${p.codigo} ya está asignado.`);
      return;
    }
    this.error.set(null);
    this.asignaciones.update(l => [...l, { grupoId: g.id, plantaId: p.id, suplente: this.capTipo().suplente }]);
    this.capGrupo.set(null);
    this.capPlanta.set(null);
    this.capTipo.set(TIPOS[0]);
    this.sucio.set(true);
  }

  protected quitarAsignacion(i: number): void {
    this.asignaciones.update(l => l.filter((_, k) => k !== i));
    this.sucio.set(true);
  }

  protected async guardar(): Promise<void> {
    this.error.set(null);
    if (this.asignaciones().length === 0) {
      this.error.set('Un usuario activo necesita al menos un grupo.');
      return;
    }
    this.guardando.set(true);
    try {
      if (this.nuevo) {
        if (!this.usuario().trim() || !this.nombre().trim() || !this.contrasena().trim()) {
          this.error.set('Usuario, nombre y contraseña inicial son obligatorios.');
          return;
        }
        const creado = await this.seguridad.crearUsuario({
          usuario: this.usuario().trim(), nombre: this.nombre().trim(), email: this.email().trim() || null,
          contrasena: this.contrasena().trim(), asignaciones: this.asignaciones(),
        });
        if (this.agente()) await this.seguridad.ligarAgente(creado.id, this.agente()!.id);
        void this.router.navigateByUrl(`/plataforma/usuarios/${creado.id}`);
        return;
      }
      const u = this.guardado()!;
      let actualizado = await this.seguridad.editarUsuario(u.id, {
        rowVersion: u.rowVersion, nombre: this.nombre().trim(), email: this.email().trim() || null, asignaciones: this.asignaciones(),
      });
      const agenteId = this.agente()?.id ?? null;
      if (agenteId !== actualizado.agenteId) actualizado = await this.seguridad.ligarAgente(u.id, agenteId);
      this.cargar(actualizado);
      this.avisos.exito('Usuario guardado.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo guardar el usuario.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected descartar(): void {
    this.error.set(null);
    const u = this.guardado();
    if (u) this.cargar(u);
    else void this.router.navigateByUrl('/plataforma/usuarios');
  }

  private async cambiarEstado(archivar: boolean): Promise<void> {
    const u = this.guardado();
    if (!u) return;
    try {
      this.cargar(archivar ? await this.seguridad.archivarUsuario(u.id) : await this.seguridad.restaurarUsuario(u.id));
      this.avisos.exito(archivar ? 'Usuario archivado.' : 'Usuario restaurado.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo cambiar el estado.');
    }
  }

  private pedirContrasena(): void {
    this.nuevaContrasena.set('');
    this.dialogoContrasena.set(true);
  }

  protected async restablecer(): Promise<void> {
    const u = this.guardado(), c = this.nuevaContrasena().trim();
    if (!u || !c) return;
    this.dialogoContrasena.set(false);
    try {
      await this.seguridad.restablecerContrasena(u.id, c);
      this.avisos.exito('Contraseña restablecida.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo restablecer la contraseña.');
    }
  }
}
