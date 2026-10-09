import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { OrigenEnMemoria } from '../../../../core/lista/origen-en-memoria';
import { HojaRegistro } from '../../../../shared/hoja-registro/hoja-registro';
import { AvisosService } from '../../../../shared/odoo-dialog/avisos';
import { OdooMaestro } from '../../../../shared/odoo-maestro/odoo-maestro';
import { OdooMany2one } from '../../../../shared/odoo-many2one/odoo-many2one';
import { OdooNumber } from '../../../../shared/odoo-number/odoo-number';
import { OdooTabs, PcPestana } from '../../../../shared/odoo-tabs/odoo-tabs';
import { OrigenHttp } from '../../../../core/lista/origen-http';
import { FilaProducto } from '../productos-list/productos-list';
import { CatalogosService, ClasificacionDto, DatosPt, DatosRollo, ProductoDetalleDto } from '../../catalogos.service';

interface CampoFicha<T> { campo: keyof T; etiqueta: string; tipo: 'texto' | 'numero'; unidad?: string; decimales?: number }

const CAMPOS_PT: CampoFicha<DatosPt>[] = [
  { campo: 'customerPartNumber', etiqueta: 'No. parte cliente', tipo: 'texto' },
  { campo: 'finalSize', etiqueta: 'Medida final', tipo: 'texto' },
  { campo: 'inks', etiqueta: 'Tintas', tipo: 'texto' },
  { campo: 'pantones', etiqueta: 'Pantones', tipo: 'texto' },
  { campo: 'dieCut', etiqueta: 'Suaje', tipo: 'texto' },
  { campo: 'packaging', etiqueta: 'Empaque', tipo: 'texto' },
  { campo: 'sealType', etiqueta: 'Tipo de sello', tipo: 'texto' },
  { campo: 'kgPerThousand', etiqueta: 'Kilos por millar', tipo: 'numero', unidad: 'kg', decimales: 2 },
];

const CAMPOS_ROLLO: CampoFicha<DatosRollo>[] = [
  { campo: 'materialType', etiqueta: 'Tipo de material', tipo: 'texto' },
  { campo: 'rollTypeSize', etiqueta: 'Medida rollo', tipo: 'texto' },
  { campo: 'gaugeMicrons', etiqueta: 'Calibre (micrones)', tipo: 'numero', decimales: 0 },
  { campo: 'kgPerRoll', etiqueta: 'Kilos por rollo', tipo: 'numero', unidad: 'kg', decimales: 2 },
  { campo: 'treatmentDynes', etiqueta: 'Tratamiento (dynas)', tipo: 'numero', decimales: 0 },
  { campo: 'pigment', etiqueta: 'Pigmento', tipo: 'texto' },
  { campo: 'additive', etiqueta: 'Aditivo', tipo: 'texto' },
  { campo: 'perforation', etiqueta: 'Perforación', tipo: 'texto' },
  { campo: 'preliminaryPrint', etiqueta: 'Impresión preliminar', tipo: 'texto' },
];

const mitad = <T>(l: T[]): [T[], T[]] => [l.slice(0, Math.ceil(l.length / 2)), l.slice(Math.ceil(l.length / 2))];

/**
 * Producto sincronizado de CONTPAQi (FR-015 a FR-018, P-10 de la propuesta aprobada): lo de CONTPAQi es
 * texto de solo lectura; la clasificación y la ficha técnica son de PolyConecta y se editan en su lugar
 * (D-164). Sin "Nuevo": los productos se dan de alta en CONTPAQi.
 */
@Component({
  selector: 'pc-producto-form',
  imports: [FormsModule, NgTemplateOutlet, HojaRegistro, OdooMaestro, OdooMany2one, OdooNumber, OdooTabs, PcPestana],
  templateUrl: './producto-form.html',
  styles: ':host { display: block; }',
})
export class ProductoForm implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly catalogos = inject(CatalogosService);
  private readonly avisos = inject(AvisosService);

  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly producto = signal<ProductoDetalleDto | null>(null);

  protected readonly clasificacion = signal<ClasificacionDto | null>(null);
  protected readonly pt = signal<DatosPt>({});
  protected readonly rollo = signal<DatosRollo>({ materialType: '', rollTypeSize: '' });
  protected readonly rolloLigado = signal<FilaProducto | null>(null);
  protected readonly cambioClasificacion = signal(false);
  protected readonly cambioFicha = signal(false);
  protected readonly sucio = computed(() => this.cambioClasificacion() || this.cambioFicha());

  protected readonly clasificaciones = signal<ClasificacionDto[]>([]);

  protected readonly camposPt = mitad(CAMPOS_PT);
  protected readonly camposRollo = mitad(CAMPOS_ROLLO);
  protected readonly pestanas = [{ id: 'pt', titulo: 'Ficha técnica · PT' }, { id: 'rollo', titulo: 'Ficha técnica · Rollo' }];

  protected readonly puedeClasificar = computed(() => !!this.producto()?.acciones.find(a => a.accion === 'clasificar')?.disponible);
  protected readonly puedeEditarFicha = computed(() => !!this.producto()?.acciones.find(a => a.accion === 'editar_ficha')?.disponible);

  protected readonly origenClasificaciones = computed(() => { const l = this.clasificaciones(); return new OrigenEnMemoria<ClasificacionDto>({ datos: () => l, id: c => String(c.id), buscables: ['codigo', 'nombre'] }); });
  /** El rollo ligado se busca en todo el catálogo, en el servidor (lista de productos, permiso de lectura). */
  protected readonly origenProductos = new OrigenHttp<FilaProducto>({ modulo: 'inventario', lista: 'productos', id: f => String(f.id) });
  protected readonly textoClasificacion = (c: ClasificacionDto) => c.nombre;
  protected readonly textoProducto = (p: FilaProducto) => (p.codigo ? `${p.codigo} - ${p.nombre}` : p.nombre);
  protected readonly idPorId = (r: { id: number }) => String(r.id);

  async ngOnInit(): Promise<void> {
    try {
      const id = this.route.snapshot.paramMap.get('id') ?? '';
      const [p, clas] = await Promise.all([
        this.catalogos.obtenerProducto(id),
        this.catalogos.listarClasificaciones().catch(() => []),
      ]);
      this.clasificaciones.set(clas);
      this.cargar(p);
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo cargar el producto.');
    } finally {
      this.cargando.set(false);
    }
  }

  private cargar(p: ProductoDetalleDto): void {
    this.producto.set(p);
    this.clasificacion.set(this.clasificaciones().find(c => c.id === p.clasificacionId) ?? null);
    this.pt.set({ ...(p.ficha?.pt ?? {}) });
    this.rollo.set({ ...(p.ficha?.rollo ?? { materialType: '', rollTypeSize: '' }) });
    const ligadoId = p.ficha?.rolloLigadoProductoId ?? null;
    this.rolloLigado.set(ligadoId ? { id: ligadoId, codigo: '', nombre: p.ficha?.rolloLigadoProducto ?? '', activo: true } : null);
    this.cambioClasificacion.set(false);
    this.cambioFicha.set(false);
  }

  protected valorPt(c: keyof DatosPt): string | number | null { return (this.pt()[c] ?? null) as string | number | null; }
  protected valorRollo(c: keyof DatosRollo): string | number | null { return (this.rollo()[c] ?? null) as string | number | null; }

  protected cambiarPt(c: keyof DatosPt, v: unknown): void {
    this.pt.update(d => ({ ...d, [c]: v === '' ? null : v }));
    this.cambioFicha.set(true);
  }

  protected cambiarRollo(c: keyof DatosRollo, v: unknown): void {
    this.rollo.update(d => ({ ...d, [c]: v === '' ? null : v }));
    this.cambioFicha.set(true);
  }

  protected elegirClasificacion(c: ClasificacionDto | null): void {
    this.clasificacion.set(c);
    this.cambioClasificacion.set(true);
  }

  protected elegirRolloLigado(p: FilaProducto | null): void {
    this.rolloLigado.set(p);
    this.cambioFicha.set(true);
  }

  protected async guardar(): Promise<void> {
    const p = this.producto();
    if (!p) return;
    this.guardando.set(true);
    this.error.set(null);
    try {
      let actual = p;
      if (this.cambioClasificacion()) actual = await this.catalogos.clasificarProducto(p.id, this.clasificacion()?.id ?? null);
      if (this.cambioFicha()) {
        actual = await this.catalogos.guardarFichaTecnica(p.id, {
          rollo: this.rollo(), pt: this.pt(), rolloLigadoProductoId: this.rolloLigado()?.id ?? null,
        });
      }
      this.cargar(actual);
      this.avisos.exito('Producto guardado.');
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo guardar el producto.');
    } finally {
      this.guardando.set(false);
    }
  }

  protected descartar(): void {
    this.error.set(null);
    const p = this.producto();
    if (p) this.cargar(p);
  }
}
