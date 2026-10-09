import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HojaRegistro } from '../../../../shared/hoja-registro/hoja-registro';
import { OdooMaestro } from '../../../../shared/odoo-maestro/odoo-maestro';
import { OdooTabs, PcPestana } from '../../../../shared/odoo-tabs/odoo-tabs';
import { CatalogosService, ClienteDetalleDto } from '../../catalogos.service';

/**
 * Cliente sincronizado de CONTPAQi (D-149, D-150, P-12 de la propuesta aprobada): todo de solo lectura,
 * con sus domicilios en una pestaña. Sin "Nuevo" ni barra de acciones: se da de alta y se cambia en CONTPAQi.
 */
@Component({
  selector: 'pc-cliente-form',
  imports: [HojaRegistro, OdooMaestro, OdooTabs, PcPestana],
  template: `
    @if (!cargando()) {
      <pc-hoja-registro lista="Clientes" ruta="/ventas/clientes" titulo="Cliente" tipo="ventas.cliente" [conNuevo]="false"
                        [nombre]="cliente()?.razonSocial ?? ''" [error]="error()" [id]="cliente()?.id ?? null">
        @if (cliente(); as c) {
          <pc-odoo-maestro>
            <div izquierda>
              <div class="o_form_label_row"><span class="o_form_label">Clave</span><span class="o_form_value fw-semibold">{{ c.clave ?? c.codigo }}</span></div>
              <div class="o_form_label_row"><span class="o_form_label">RFC</span><span class="o_form_value">{{ c.rfc || '—' }}</span></div>
            </div>
            <div derecha>
              <div class="o_form_label_row"><span class="o_form_label">Moneda</span><span class="o_form_value">{{ c.moneda || '—' }}</span></div>
              <div class="o_form_label_row"><span class="o_form_label">Estado</span><span class="o_form_value">{{ c.activo ? 'Activo' : 'Archivado' }}</span></div>
            </div>
          </pc-odoo-maestro>
          <pc-odoo-tabs [pestanas]="pestanas">
            <ng-template pcPestana="domicilios">
              <div class="mb-4">
                <table class="table align-middle mb-0" data-domicilios>
                  <thead>
                    <tr class="text-muted small"><th style="width: 110px;">Tipo</th><th style="width: 220px;">Referencia</th><th>Dirección</th></tr>
                  </thead>
                  <tbody>
                    @for (d of c.domicilios ?? []; track d.id) {
                      <tr>
                        <td>{{ d.tipo === 'Envio' ? 'Envío' : d.tipo }}</td>
                        <td>{{ d.sucursal || '—' }}</td>
                        <td class="small">{{ d.texto }}</td>
                      </tr>
                    } @empty {
                      <tr><td colspan="3" class="small text-muted">Sin domicilios en CONTPAQi.</td></tr>
                    }
                  </tbody>
                </table>
              </div>
            </ng-template>
          </pc-odoo-tabs>
        }
      </pc-hoja-registro>
    }
  `,
  styles: ':host { display: block; }',
})
export class ClienteForm implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly catalogos = inject(CatalogosService);

  protected readonly cargando = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly cliente = signal<ClienteDetalleDto | null>(null);
  protected readonly pestanas = [{ id: 'domicilios', titulo: 'Domicilios' }];

  async ngOnInit(): Promise<void> {
    try {
      this.cliente.set(await this.catalogos.obtenerCliente(this.route.snapshot.paramMap.get('id') ?? ''));
    } catch (e: unknown) {
      this.error.set((e as Error).message || 'No se pudo cargar el cliente.');
    } finally {
      this.cargando.set(false);
    }
  }
}
