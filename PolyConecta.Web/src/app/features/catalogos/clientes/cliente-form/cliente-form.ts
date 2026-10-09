import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { OdooBreadcrumb } from '../../../../shared/odoo-breadcrumb/odoo-breadcrumb';
import {
  CatalogosService,
  ClienteDetalleDto,
  DomicilioDto,
} from '../../catalogos.service';

@Component({
  selector: 'pc-cliente-form',
  imports: [OdooBreadcrumb],
  templateUrl: './cliente-form.html',
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
export class ClienteForm implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly catalogos = inject(CatalogosService);

  protected readonly id = signal<string>(this.route.snapshot.paramMap.get('id') ?? '1');

  protected readonly cargando = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly codigo = signal('');
  protected readonly razonSocial = signal('');
  protected readonly rfc = signal('');
  protected readonly moneda = signal('');
  protected readonly activo = signal(true);
  protected readonly domicilioFiscal = signal<DomicilioDto | null>(null);
  protected readonly domiciliosEnvio = signal<DomicilioDto[]>([]);

  async ngOnInit(): Promise<void> {
    this.cargando.set(true);
    this.error.set(null);
    try {
      const cli = await this.catalogos.obtenerCliente(this.id());
      this.cargarCliente(cli);
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'Error al cargar el cliente.');
    } finally {
      this.cargando.set(false);
    }
  }

  private cargarCliente(c: ClienteDetalleDto): void {
    this.codigo.set(c.clave ?? c.codigo ?? '');
    this.razonSocial.set(c.razonSocial);
    this.rfc.set(c.rfc ?? '');
    this.moneda.set(c.moneda ?? '');
    this.activo.set(c.activo);

    if (c.domicilioFiscal) {
      this.domicilioFiscal.set(c.domicilioFiscal);
    } else if (c.domicilios) {
      const fiscal = c.domicilios.find(d => d.tipo?.toLowerCase() === 'fiscal');
      this.domicilioFiscal.set(fiscal ?? null);
    }

    if (c.domiciliosEnvio && c.domiciliosEnvio.length > 0) {
      this.domiciliosEnvio.set(c.domiciliosEnvio);
    } else if (c.domicilios) {
      const envios = c.domicilios.filter(d => d.tipo?.toLowerCase() === 'envio');
      this.domiciliosEnvio.set(envios);
    }
  }

  protected volver(): void {
    void this.router.navigateByUrl('/ventas/clientes');
  }

  protected formatearDomicilio(d: DomicilioDto): string {
    if (d.texto) return d.texto;
    const partes = [
      d.calle,
      d.numeroExterior ? `No. ${d.numeroExterior}` : '',
      d.numeroInterior ? `Int. ${d.numeroInterior}` : '',
      d.colonia ? `Col. ${d.colonia}` : '',
      d.codigoPostal ? `C.P. ${d.codigoPostal}` : '',
      d.ciudad || d.municipio,
      d.estado,
      d.pais,
    ].filter(Boolean);
    return partes.join(', ');
  }
}
