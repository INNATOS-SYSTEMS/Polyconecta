import { Component, computed, effect, inject, input, model, signal } from '@angular/core';
import { OdooIcon } from '../odoo-icon/odoo-icon';
import { ChatterService } from '../../core/chatter/chatter.service';
import { horaCorta } from '../../core/format/numero';

export interface ChatterEntry {
  author: string;
  timestamp: string;
  text: string;
}

/**
 * Réplica de Components/Chatter/OdooChatterDrawer.razor. Con `documentId` y la API disponible, las
 * notas viajan por el hub y aparecen en las demás pestañas del documento (US-4). Sin API, se agregan
 * en local como en el prototipo; el encabezado lo indica en su title (no cambia los píxeles) y, si
 * se envía sin conexión, aparece el aviso "Sin conexión en vivo".
 */
@Component({
  selector: 'pc-odoo-chatter-drawer',
  imports: [OdooIcon],
  templateUrl: './odoo-chatter-drawer.html',
  styles: ':host { display: contents; }',
})
export class OdooChatterDrawer {
  private readonly chatter = inject(ChatterService);

  readonly messages = model<ChatterEntry[]>([]);
  /** Folio del documento. Sin él, el panel es solo local (como el prototipo). */
  readonly documentId = input<string | undefined>(undefined);
  /** Documento que todavía no se guarda ("Nuevo", D-136): el chatter se ve, pero se activa al guardar. */
  readonly inactivo = input(false);

  protected readonly newMsgText = signal('');
  protected readonly avisoSinConexion = signal(false);
  protected readonly tituloConexion = computed(() =>
    !this.documentId() ? '' : this.chatter.estado() === 'conectado' ? 'Conexión en vivo' : 'Sin conexión en vivo',
  );

  constructor() {
    effect(onCleanup => {
      const id = this.documentId();
      if (!id) return;
      void this.chatter.conectar();
      const dejar = this.chatter.escuchar(id, m =>
        this.messages.update(lista => [...lista, { author: m.author, timestamp: m.timestamp, text: m.text }]),
      );
      onCleanup(dejar);
    });
  }

  protected async send(): Promise<void> {
    const texto = this.newMsgText();
    if (texto.trim() === '') return;
    this.newMsgText.set('');
    const id = this.documentId();
    // Con conexión, el mensaje propio llega por ReceiveChatterMessage: no se agrega dos veces.
    if (id && (await this.chatter.enviar(id, 'Administrator', texto))) return;
    if (id) this.avisoSinConexion.set(true);
    this.messages.update(m => [...m, { author: 'Administrator', timestamp: horaCorta(new Date()), text: texto }]);
  }

  protected inicial(author: string): string {
    return author ? author.substring(0, 1) : 'S';
  }
}

export { horaCorta } from '../../core/format/numero';
