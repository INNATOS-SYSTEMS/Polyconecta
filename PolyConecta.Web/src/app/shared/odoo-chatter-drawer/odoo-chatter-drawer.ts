import { Component, computed, effect, inject, input, model, signal } from '@angular/core';
import { OdooIcon } from '../odoo-icon/odoo-icon';
import { ChatterService, DocumentoChatter, MensajeGuardado } from '../../core/chatter/chatter.service';
import { fechaCorta, horaCorta } from '../../core/format/numero';

export interface ChatterEntry {
  author: string;
  timestamp: string;
  text: string;
  /** Solo en documentos guardados: `Nota` es interna y `Cambio` lo escribe la transición (R-04). */
  clase?: 'Mensaje' | 'Nota' | 'Cambio';
  grupo?: string | null;
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
  styles: ':host { display: contents; } .o_chatter_nota { background: #fff8e6; }',
})
export class OdooChatterDrawer {
  private readonly chatter = inject(ChatterService);

  readonly messages = model<ChatterEntry[]>([]);
  /** Folio del documento. Sin él, el panel es solo local (como el prototipo). */
  readonly documentId = input<string | undefined>(undefined);
  /** Documento que todavía no se guarda ("Nuevo", D-136): el chatter se ve, pero se activa al guardar. */
  readonly inactivo = input(false);
  /**
   * Documento con chatter guardado (R-04, L2-T032): el historial sale de la API, los mensajes se guardan
   * con el autor de la sesión y llegan en vivo a quien tenga abierto el mismo documento.
   */
  readonly documento = input<DocumentoChatter | undefined>(undefined);
  protected readonly clase = signal<'Mensaje' | 'Nota'>('Mensaje');
  protected readonly errorEnvio = signal<string | null>(null);
  private readonly vistos = new Set<number>();

  protected readonly newMsgText = signal('');
  protected readonly avisoSinConexion = signal(false);
  protected readonly tituloConexion = computed(() =>
    !this.documentId() ? '' : this.chatter.estado() === 'conectado' ? 'Conexión en vivo' : 'Sin conexión en vivo',
  );

  constructor() {
    effect(onCleanup => {
      const d = this.documento();
      if (!d) return;
      this.vistos.clear();
      this.messages.set([]);
      const agregar = (m: MensajeGuardado) => {
        if (this.vistos.has(m.id)) return; // el propio llega también por el grupo
        this.vistos.add(m.id);
        this.messages.update(lista => [...lista, entrada(m)]);
      };
      const dejar = this.chatter.seguir(d, agregar);
      void this.chatter.historial(d).then(h => h.forEach(agregar)).catch(() => this.avisoSinConexion.set(true));
      onCleanup(dejar);
    });
    effect(onCleanup => {
      if (this.documento()) return;
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
    const d = this.documento();
    if (d) {
      this.errorEnvio.set(null);
      try {
        const m = await this.chatter.publicar(d, this.clase(), texto);
        this.newMsgText.set('');
        if (!this.vistos.has(m.id)) {
          this.vistos.add(m.id);
          this.messages.update(lista => [...lista, entrada(m)]);
        }
      } catch (e) {
        this.errorEnvio.set((e as Error).message || 'No se pudo enviar el mensaje.');
      }
      return;
    }
    this.newMsgText.set('');
    const id = this.documentId();
    // Con conexión, el mensaje propio llega por ReceiveChatterMessage: no se agrega dos veces.
    if (id && (await this.chatter.enviar(id, 'Administrator', texto))) return;
    if (id) this.avisoSinConexion.set(true);
    this.messages.update(m => [...m, { author: 'Administrator', timestamp: horaCorta(new Date()), text: texto }]);
  }

  protected inicial(author: string): string {
    if (!author) return 'S';
    return author.substring(0, 1);
  }
}

const entrada = (m: MensajeGuardado): ChatterEntry => ({
  author: m.autor,
  timestamp: fechaCorta(new Date(m.fecha)),
  text: m.texto,
  clase: m.clase,
  grupo: m.grupo,
});

export { horaCorta } from '../../core/format/numero';
