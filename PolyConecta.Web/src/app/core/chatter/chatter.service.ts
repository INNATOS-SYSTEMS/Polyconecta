import { Injectable, InjectionToken, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { horaCorta } from '../format/numero';
import { pedirApi } from '../sesion/api';

/** URL del hub del chatter en PolyConecta.Api (FR-015): por el proxy, en el mismo origen, para llevar la cookie de sesión. */
export const CHATTER_URL = new InjectionToken<string>('CHATTER_URL', {
  providedIn: 'root',
  factory: () => '/hubs/chatter',
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

/** Documento con chatter guardado (R-04): `ventas.pedido` y su id. */
export interface DocumentoChatter {
  tipo: string;
  id: number;
}

export type ClaseMensaje = 'Mensaje' | 'Nota' | 'Cambio';

/** `MensajeChatter` de contracts/api-f1.md: el historial y lo que transmite el hub. */
export interface MensajeGuardado {
  id: number;
  tipo: string;
  documentoId: number;
  clase: ClaseMensaje;
  texto: string;
  autor: string;
  grupo: string | null;
  fecha: string;
}

const llave = (d: DocumentoChatter) => `${d.tipo}:${d.id}`;

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
  private readonly oyentesGuardados = new Map<string, Set<(m: MensajeGuardado) => void>>();
  /** Documentos a cuyo grupo está unida esta conexión; se vuelven a unir al reconectar. */
  private readonly unidos = new Map<string, DocumentoChatter>();

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

  // --- Documentos con chatter guardado (R-04, L2-T032) ---

  /** Historial, del más antiguo al más reciente para pintarlo; la API lo da al revés, en páginas de 50. */
  async historial(d: DocumentoChatter): Promise<MensajeGuardado[]> {
    const pagina = await pedirApi<MensajeGuardado[]>(`/api/v1/plataforma/chatter/${encodeURIComponent(d.tipo)}/${d.id}`);
    return [...pagina].reverse();
  }

  /** Recibe lo que llega al grupo del documento y se une a él. Devuelve la función para salir. */
  seguir(d: DocumentoChatter, oyente: (m: MensajeGuardado) => void): () => void {
    const k = llave(d);
    const set = this.oyentesGuardados.get(k) ?? new Set();
    set.add(oyente);
    this.oyentesGuardados.set(k, set);
    this.unidos.set(k, d);
    void this.conectar().then(() => this.unirse(d));
    return () => {
      set.delete(oyente);
      if (set.size > 0) return;
      this.unidos.delete(k);
      if (this.enVivo) void this.conexion!.invoke('SalirDeDocumento', d.tipo, d.id).catch(() => undefined);
    };
  }

  /** Guarda por el hub con el autor de la sesión. Rechaza si no hay conexión o la API no lo acepta. */
  async publicar(d: DocumentoChatter, clase: 'Mensaje' | 'Nota', texto: string): Promise<MensajeGuardado> {
    await this.conectar();
    if (!this.enVivo) throw new Error('Sin conexión en vivo: el mensaje no se envió.');
    return this.conexion!.invoke<MensajeGuardado>('EnviarMensaje', d.tipo, d.id, clase, texto);
  }

  private async unirse(d: DocumentoChatter): Promise<void> {
    if (!this.enVivo || !this.unidos.has(llave(d))) return;
    await this.conexion!.invoke('UnirseADocumento', d.tipo, d.id).catch(() => undefined);
  }

  private prepararConexion(): HubConnection {
    const c = this.crear(this.url);
    c.on('MensajeChatter', (m: MensajeGuardado) => {
      this.oyentesGuardados.get(`${m.tipo}:${m.documentoId}`)?.forEach(o => o(m));
    });
    c.on('ReceiveChatterMessage', (documentId: string, author: string, text: string) => {
      const mensaje: MensajeChatter = { documentId, author, text, timestamp: horaCorta(new Date()) };
      this.oyentes.get(documentId)?.forEach(o => o(mensaje));
    });
    c.onreconnecting(() => this.estado.set('conectando'));
    c.onreconnected(() => {
      this.estado.set('conectado');
      // Una conexión nueva no conserva sus grupos.
      for (const d of this.unidos.values()) void this.unirse(d);
    });
    c.onclose(() => this.estado.set('desconectado'));
    return c;
  }
}
