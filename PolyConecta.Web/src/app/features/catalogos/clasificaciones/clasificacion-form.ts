import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HojaRegistro } from '../../../shared/hoja-registro/hoja-registro';
import { AvisosService } from '../../../shared/odoo-dialog/avisos';
import { OdooMaestro } from '../../../shared/odoo-maestro/odoo-maestro';
import { CatalogosService, ClasificacionDto } from '../catalogos.service';

/** Clasificación de productos (FR-017, P-13): código y nombre, con su bitácora. Se edita en su lugar (D-164). */
@Component({
  selector: 'pc-clasificacion-form',
  imports: [HojaRegistro, OdooMaestro],
  template: `
    @if (!cargando()) {
      <pc-hoja-registro lista="Clasificaciones" ruta="/plataforma/clasificaciones" titulo="Clasificación" tipo="inventario.clasificacion"
                        [nombre]="guardada()?.nombre ?? ''" [nuevo]="nuevo" [sucio]="sucio()" [guardando]="guardando()" [error]="error()"
                        [id]="guardada()?.id ?? null" (guardar)="guardar()" (descartar)="descartar()">
        <pc-odoo-maestro>
          <div izquierda>
            <div class="o_form_label_row">
              <span class="o_form_label">Código</span>
              <input class="form-control form-control-sm o_inline_input" aria-label="Código" data-campo="codigo"
                     [value]="codigo()" (input)="codigo.set($any($event.target).value); sucio.set(true)" />
            </div>
            <div class="o_form_label_row">
              <span class="o_form_label">Nombre</span>
              <input class="form-control form-control-sm o_inline_input" aria-label="Nombre" data-campo="nombre"
                     [value]="nombre()" (input)="nombre.set($any($event.target).value); sucio.set(true)" />
            </div>
          </div>
          <div derecha>
            <div class="o_form_label_row">
              <span class="o_form_label">Valor en CONTPAQi</span>
              <span class="o_form_value">{{ guardada()?.valorErp ?? '—' }}</span>
            </div>
          </div>
        </pc-odoo-maestro>
      </pc-hoja-registro>
    }
  `,
  styles: ':host { display: block; }',
})
export class ClasificacionForm implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly catalogos = inject(CatalogosService);
  private readonly avisos = inject(AvisosService);

  protected readonly idRuta = this.route.snapshot.paramMap.get('id') ?? 'nuevo';
  protected readonly nuevo = this.idRuta === 'nuevo';
  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly sucio = signal(false);
  protected readonly guardada = signal<ClasificacionDto | null>(null);
  protected readonly codigo = signal('');
  protected readonly nombre = signal('');

  async ngOnInit(): Promise<void> {
    try {
      if (!this.nuevo) {
        const c = (await this.catalogos.listarClasificaciones()).find(x => String(x.id) === this.idRuta);
        if (!c) throw new Error('La clasificación no existe.');
        this.cargar(c);
      }
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo cargar la clasificación.');
    } finally {
      this.cargando.set(false);
    }
  }

  private cargar(c: ClasificacionDto): void {
    this.guardada.set(c);
    this.codigo.set(c.codigo);
    this.nombre.set(c.nombre);
    this.sucio.set(false);
  }

  protected async guardar(): Promise<void> {
    this.error.set(null);
    if (!this.codigo().trim() || !this.nombre().trim()) {
      this.error.set('La clasificación lleva código y nombre.');
      return;
    }
    this.guardando.set(true);
    try {
      const c = await this.catalogos.guardarClasificacion(this.guardada()?.id ?? null, { codigo: this.codigo().trim(), nombre: this.nombre().trim() });
      if (this.nuevo) {
        void this.router.navigateByUrl(`/plataforma/clasificaciones/${c.id}`);
        return;
      }
      this.cargar(c);
      this.avisos.exito('Clasificación guardada.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo guardar la clasificación.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected descartar(): void {
    this.error.set(null);
    const c = this.guardada();
    if (c) this.cargar(c);
    else void this.router.navigateByUrl('/plataforma/clasificaciones');
  }
}
