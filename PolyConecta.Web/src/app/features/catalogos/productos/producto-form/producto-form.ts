import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { OdooBreadcrumb } from '../../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import { SesionState } from '../../../../core/sesion/sesion-state';
import {
  CatalogosService,
  ClasificacionDto,
  DatosPt,
  DatosRollo,
  ProductoDetalleDto,
} from '../../catalogos.service';

@Component({
  selector: 'pc-producto-form',
  imports: [FormsModule, OdooBreadcrumb],
  templateUrl: './producto-form.html',
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
    .nav-tabs .nav-link {
      color: var(--text-muted, #64748b);
      cursor: pointer;
    }
    .nav-tabs .nav-link.active {
      color: var(--brand-primary, #714B67);
      font-weight: 600;
      border-bottom: 2px solid var(--brand-primary, #714B67);
    }
  `,
})
export class ProductoForm implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly catalogos = inject(CatalogosService);
  private readonly sesion = inject(SesionState);

  protected readonly id = signal<string>(this.route.snapshot.paramMap.get('id') ?? '1');

  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly exito = signal<string | null>(null);

  protected readonly pestanaActiva = signal<'general' | 'ficha'>('general');

  // Datos de solo lectura de CONTPAQi
  protected readonly codigo = signal('');
  protected readonly nombre = signal('');
  protected readonly unidadBase = signal('');
  protected readonly controlaLote = signal(false);
  protected readonly activo = signal(true);
  protected readonly rowVersion = signal('');

  // Clasificación
  protected readonly clasificaciones = signal<ClasificacionDto[]>([]);
  protected readonly clasificacionId = signal<number | null>(null);

  // Ficha técnica - Rollo
  protected readonly materialType = signal('');
  protected readonly rollTypeSize = signal('');
  protected readonly gaugeMicrons = signal<number | null>(null);
  protected readonly kgPerRoll = signal<number | null>(null);
  protected readonly treatmentDynes = signal<number | null>(null);
  protected readonly pigment = signal('');
  protected readonly additive = signal('');
  protected readonly perforation = signal('');
  protected readonly preliminaryPrint = signal('');

  // Ficha técnica - PT
  protected readonly customerPartNumber = signal('');
  protected readonly finalSize = signal('');
  protected readonly inks = signal('');
  protected readonly pantones = signal('');
  protected readonly dieCut = signal('');
  protected readonly packaging = signal('');
  protected readonly sealType = signal('');
  protected readonly kgPerThousand = signal<number | null>(null);

  // Permisos
  protected readonly puedeClasificar = computed(() =>
    this.sesion.tienePermiso('inventario.producto.clasificar')
  );
  protected readonly puedeEditarFicha = computed(() =>
    this.sesion.tienePermiso('inventario.ficha.editar')
  );

  async ngOnInit(): Promise<void> {
    this.cargando.set(true);
    this.error.set(null);
    try {
      const [clasifs, prod] = await Promise.all([
        this.catalogos.listarClasificaciones().catch(() => []),
        this.catalogos.obtenerProducto(this.id()),
      ]);
      this.clasificaciones.set(clasifs);
      this.cargarProducto(prod);
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al cargar el producto.');
    } finally {
      this.cargando.set(false);
    }
  }

  private cargarProducto(p: ProductoDetalleDto): void {
    this.codigo.set(p.codigo);
    this.nombre.set(p.nombre);
    this.unidadBase.set(p.unidadBase);
    this.controlaLote.set(p.controlaLote);
    this.activo.set(p.activo);
    this.rowVersion.set(p.rowVersion);
    this.clasificacionId.set(p.clasificacionId);

    if (p.rollo) {
      this.materialType.set(p.rollo.materialType ?? '');
      this.rollTypeSize.set(p.rollo.rollTypeSize ?? '');
      this.gaugeMicrons.set(p.rollo.gaugeMicrons ?? null);
      this.kgPerRoll.set(p.rollo.kgPerRoll ?? null);
      this.treatmentDynes.set(p.rollo.treatmentDynes ?? null);
      this.pigment.set(p.rollo.pigment ?? '');
      this.additive.set(p.rollo.additive ?? '');
      this.perforation.set(p.rollo.perforation ?? '');
      this.preliminaryPrint.set(p.rollo.preliminaryPrint ?? '');
    }

    if (p.pt) {
      this.customerPartNumber.set(p.pt.customerPartNumber ?? '');
      this.finalSize.set(p.pt.finalSize ?? '');
      this.inks.set(p.pt.inks ?? '');
      this.pantones.set(p.pt.pantones ?? '');
      this.dieCut.set(p.pt.dieCut ?? '');
      this.packaging.set(p.pt.packaging ?? '');
      this.sealType.set(p.pt.sealType ?? '');
      this.kgPerThousand.set(p.pt.kgPerThousand ?? null);
    }
  }

  protected async guardarClasificacion(): Promise<void> {
    this.guardando.set(true);
    this.error.set(null);
    this.exito.set(null);
    try {
      const prod = await this.catalogos.clasificarProducto(this.id(), this.clasificacionId());
      this.cargarProducto(prod);
      this.exito.set('Clasificación guardada exitosamente.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al guardar la clasificación.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected async guardarFicha(): Promise<void> {
    this.guardando.set(true);
    this.error.set(null);
    this.exito.set(null);

    if (!this.materialType().trim() || !this.rollTypeSize().trim()) {
      this.error.set('El bloque Rollo requiere Tipo de material y Medida de rollo.');
      this.guardando.set(false);
      return;
    }

    const rollo: DatosRollo = {
      materialType: this.materialType().trim(),
      rollTypeSize: this.rollTypeSize().trim(),
      gaugeMicrons: this.gaugeMicrons(),
      kgPerRoll: this.kgPerRoll(),
      treatmentDynes: this.treatmentDynes(),
      pigment: this.pigment().trim() || null,
      additive: this.additive().trim() || null,
      perforation: this.perforation().trim() || null,
      preliminaryPrint: this.preliminaryPrint().trim() || null,
    };

    const pt: DatosPt = {
      customerPartNumber: this.customerPartNumber().trim() || null,
      finalSize: this.finalSize().trim() || null,
      inks: this.inks().trim() || null,
      pantones: this.pantones().trim() || null,
      dieCut: this.dieCut().trim() || null,
      packaging: this.packaging().trim() || null,
      sealType: this.sealType().trim() || null,
      kgPerThousand: this.kgPerThousand(),
    };

    try {
      const prod = await this.catalogos.guardarFichaTecnica(this.id(), { rollo, pt });
      this.cargarProducto(prod);
      this.exito.set('Ficha técnica guardada exitosamente.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al guardar la ficha técnica.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected volver(): void {
    void this.router.navigateByUrl('/inventario/productos');
  }

  protected actualizarClasificacion(val: unknown): void {
    this.clasificacionId.set(val ? Number(val) : null);
  }
}
