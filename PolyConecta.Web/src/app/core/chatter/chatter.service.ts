import { Injectable, InjectionToken, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { horaCorta } from '../format/numero';

/** URL del hub del chatter en PolyConecta.Api (FR-015). */
export const CHATTER_URL = new InjectionToken<string>('CHATTER_URL', {
  providedIn: 'root',
  factory: () => 'http://localhost:9020/hubs/chatter',
});

/** Fábrica de la conexión; las pruebas la sustituyen por un hub falso. */
export const CHATTER_CONEXION = new InjectionToken<(url: string) => HubConnection>('CHATTER_CONEXION', {
  providedIn: 'root',
  factory: () => (url: string) =>
    new HubConnectionBuilder().withUrl(url, { withCredentials: true }).withAutomaticReconnect().configureLogging(LogLevel.None).build(),
});

export type EstadoChatter = 'sin-iniciar' | 'conectando' | 'conectado' | 'desconectado';

export interface MensajeChatter {
  documentId: string;
  author: string;
  text: string;
  /** Hora local de recepción en formato h:mm tt (no se usa la del servidor, research R-07). */
  timestamp: string;
}

/**
 * Chatter en vivo por SignalR (US-4, FR-016, contracts/chatter-hub.md). Si la API no está, la
 * aplicación sigue funcionando y el panel agrega las notas en local, como el prototipo.
 */
@Injectable({ providedIn: 'root' })
export class ChatterService {
  private readonly url = inject(CHATTER_URL);
  private readonly crear = inject(CHATTER_CONEXION);
  private conexion?: HubConnection;
  private inicio?: Promise<void>;
  private readonly oyentes = new Map<string, Set<(m: MensajeChatter) => void>>();

  readonly estado = signal<EstadoChatter>('sin-iniciar');

  /** Conecta si no lo está. Un fallo deja el estado en desconectado; no lanza. */
  conectar(): Promise<void> {
    if (this.inicio && this.estado() !== 'desconectado') return this.inicio;
    this.conexion ??= this.prepararConexion();
    this.estado.set('conectando');
    this.inicio = this.conexion
      .start()
      .then(() => this.estado.set('conectado'))
      .catch(() => this.estado.set('desconectado'));
    return this.inicio;
  }

  get enVivo(): boolean {
    return this.estado() === 'conectado' && this.conexion?.state === HubConnectionState.Connected;
  }

  /** Recibe los mensajes de un documento. Devuelve la función para dejar de escuchar. */
  escuchar(documentId: string, oyente: (m: MensajeChatter) => void): () => void {
    const set = this.oyentes.get(documentId) ?? new Set();
    set.add(oyente);
    this.oyentes.set(documentId, set);
    return () => set.delete(oyente);
  }

  /** Envía por el hub. Devuelve false si no hay conexión en vivo (el panel la agrega en local). */
  async enviar(documentId: string, author: string, text: string): Promise<boolean> {
    if (!this.enVivo) return false;
    try {
      await this.conexion!.invoke('SendMessage', documentId, author, text);
      return true;
    } catch {
      this.estado.set('desconectado');
      return false;
    }
  }

  private prepararConexion(): HubConnection {
    const c = this.crear(this.url);
    c.on('ReceiveChatterMessage', (documentId: string, author: string, text: string) => {
      const mensaje: MensajeChatter = { documentId, author, text, timestamp: horaCorta(new Date()) };
      this.oyentes.get(documentId)?.forEach(o => o(mensaje));
    });
    c.onreconnecting(() => this.estado.set('conectando'));
    c.onreconnected(() => this.estado.set('conectado'));
    c.onclose(() => this.estado.set('desconectado'));
    return c;
  }
}
