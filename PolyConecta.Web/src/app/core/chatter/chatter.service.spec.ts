import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { OdooChatterDrawer } from '../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { CHATTER_CONEXION, ChatterService, MensajeGuardado } from './chatter.service';

/** Hub falso: reenvía SendMessage a todos los clientes como ChatterHub (Clients.All). */
class HubFalso {
  state = HubConnectionState.Disconnected;
  private readonly handlers = new Map<string, (...args: string[]) => void>();
  constructor(private readonly falla = false) {}
  on(evento: string, h: (...args: string[]) => void) {
    this.handlers.set(evento, h);
  }
  onreconnecting() {}
  onreconnected() {}
  onclose() {}
  async start() {
    if (this.falla) throw new Error('sin API');
    this.state = HubConnectionState.Connected;
  }
  async invoke(_metodo: string, documentId: string, author: string, text: string) {
    this.handlers.get('ReceiveChatterMessage')?.(documentId, author, text, '10/6/2026 1:00 AM');
  }
}

const configurar = (hub: HubFalso) =>
  TestBed.configureTestingModule({ providers: [{ provide: CHATTER_CONEXION, useValue: () => hub as unknown as HubConnection }] });

describe('ChatterService', () => {
  it('con conexión envía por el hub y entrega solo a los oyentes del documento', async () => {
    configurar(new HubFalso());
    const chatter = TestBed.inject(ChatterService);
    await chatter.conectar();
    const recibidos: string[] = [];
    chatter.escuchar('BOL-2026-0001', m => recibidos.push(`${m.author}: ${m.text}`));
    chatter.escuchar('OTRO', () => recibidos.push('no debía llegar'));

    expect(await chatter.enviar('BOL-2026-0001', 'Administrator', 'hola')).toBe(true);

    expect(chatter.estado()).toBe('conectado');
    expect(recibidos).toEqual(['Administrator: hola']);
  });

  it('sin API queda desconectado y enviar devuelve false', async () => {
    configurar(new HubFalso(true));
    const chatter = TestBed.inject(ChatterService);
    await chatter.conectar();
    expect(chatter.estado()).toBe('desconectado');
    expect(await chatter.enviar('X', 'Administrator', 'hola')).toBe(false);
  });

  it('el panel con documento no duplica el mensaje propio cuando hay conexión', async () => {
    configurar(new HubFalso());
    const fixture = TestBed.createComponent(OdooChatterDrawer);
    fixture.componentRef.setInput('documentId', 'BOL-2026-0001');
    fixture.detectChanges();
    await TestBed.inject(ChatterService).conectar();

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = 'en vivo';
    input.dispatchEvent(new Event('change'));
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    await fixture.whenStable();

    expect(fixture.componentInstance.messages().map(m => m.text)).toEqual(['en vivo']);
  });

  it('el panel sin API agrega en local y avisa que no hay conexión en vivo', async () => {
    configurar(new HubFalso(true));
    const fixture = TestBed.createComponent(OdooChatterDrawer);
    fixture.componentRef.setInput('documentId', 'BOL-2026-0001');
    fixture.detectChanges();
    await TestBed.inject(ChatterService).conectar();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[title]')?.getAttribute('title')).toBe('Sin conexión en vivo');

    const input = el.querySelector('input') as HTMLInputElement;
    input.value = 'local';
    input.dispatchEvent(new Event('change'));
    (el.querySelector('button') as HTMLButtonElement).click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.messages().map(m => m.author)).toEqual(['Administrator']);
    expect(el.textContent).toContain('Sin conexión en vivo');
  });
});

/** Hub falso del chatter guardado: une al grupo, guarda con id y transmite `MensajeChatter` al grupo. */
class HubGuardado {
  state = HubConnectionState.Disconnected;
  readonly grupos = new Set<string>();
  readonly invocados: string[] = [];
  private readonly handlers = new Map<string, (...args: unknown[]) => void>();
  private siguienteId = 100;
  constructor(private readonly falla = false) {}
  on(evento: string, h: (...args: unknown[]) => void) {
    this.handlers.set(evento, h);
  }
  onreconnecting() {}
  onreconnected() {}
  onclose() {}
  async start() {
    if (this.falla) throw new Error('sin API');
    this.state = HubConnectionState.Connected;
  }
  async invoke(metodo: string, tipo: string, id: number, clase?: string, texto?: string) {
    this.invocados.push(metodo);
    if (metodo === 'UnirseADocumento') this.grupos.add(`${tipo}:${id}`);
    if (metodo !== 'EnviarMensaje') return undefined;
    const m: MensajeGuardado = {
      id: this.siguienteId++, tipo, documentoId: id, clase: clase as 'Nota', texto: texto!.trim(),
      autor: 'Celia Villarreal', grupo: 'Atención a Clientes', fecha: '2026-10-09T12:05:00Z',
    };
    if (this.grupos.has(`${tipo}:${id}`)) this.handlers.get('MensajeChatter')?.(m);
    return m;
  }
}

describe('Chatter guardado (L2-T032)', () => {
  const historial: MensajeGuardado[] = [
    { id: 2, tipo: 'ventas.pedido', documentoId: 15, clase: 'Cambio', texto: 'Borrador → Confirmado', autor: 'Celia Villarreal', grupo: 'Atención a Clientes', fecha: '2026-10-09T12:01:00Z' },
    { id: 1, tipo: 'ventas.pedido', documentoId: 15, clase: 'Cambio', texto: 'Nuevo → Borrador: Pedido creado con Nuevo', autor: 'Celia Villarreal', grupo: 'Atención a Clientes', fecha: '2026-10-09T12:00:00Z' },
  ];

  const abrirPanel = async (hub: HubGuardado) => {
    TestBed.configureTestingModule({ providers: [{ provide: CHATTER_CONEXION, useValue: () => hub as unknown as HubConnection }] });
    const fixture = TestBed.createComponent(OdooChatterDrawer);
    fixture.componentRef.setInput('documento', { tipo: 'ventas.pedido', id: 15 });
    fixture.detectChanges();
    await vi.waitFor(() => expect(fixture.componentInstance.messages().length).toBe(2));
    fixture.detectChanges();
    return fixture;
  };

  const escribir = async (fixture: ComponentFixture<OdooChatterDrawer>, texto: string) => {
    const el = fixture.nativeElement as HTMLElement;
    const input = el.querySelector('input') as HTMLInputElement;
    input.value = texto;
    input.dispatchEvent(new Event('change'));
    (el.querySelector('.input-group button') as HTMLButtonElement).click();
    await fixture.whenStable();
    fixture.detectChanges();
  };

  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(historial), { status: 200 })));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('carga el historial de la API del más antiguo al más reciente y se une al grupo del documento', async () => {
    const hub = new HubGuardado();
    const fixture = await abrirPanel(hub);

    expect(fetch).toHaveBeenCalledWith('/api/v1/plataforma/chatter/ventas.pedido/15', expect.anything());
    expect(fixture.componentInstance.messages().map(m => m.text)).toEqual(['Nuevo → Borrador: Pedido creado con Nuevo', 'Borrador → Confirmado']);
    await vi.waitFor(() => expect(hub.grupos.has('ventas.pedido:15')).toBe(true));
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('[data-chatter-clase="Cambio"]')).toHaveLength(2);
    expect(el.textContent).toContain('Atención a Clientes');
  });

  it('una nota interna se guarda por el hub y no se duplica aunque también llegue por el grupo', async () => {
    const hub = new HubGuardado();
    const fixture = await abrirPanel(hub);
    await vi.waitFor(() => expect(hub.grupos.size).toBe(1));

    (fixture.nativeElement.querySelector('[data-chatter-clase-opcion="Nota"]') as HTMLButtonElement).click();
    await escribir(fixture, '  Revisar el precio ');

    expect(hub.invocados).toContain('EnviarMensaje');
    const notas = fixture.componentInstance.messages().filter(m => m.clase === 'Nota');
    expect(notas.map(m => m.text)).toEqual(['Revisar el precio']);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Nota interna');
  });

  it('sin conexión no pierde lo escrito y avisa que no se envió', async () => {
    const hub = new HubGuardado(true);
    const fixture = await abrirPanel(hub);

    await escribir(fixture, 'No se va');

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[data-chatter-error]')?.textContent).toContain('no se envió');
    expect((el.querySelector('input') as HTMLInputElement).value).toBe('No se va');
    expect(fixture.componentInstance.messages()).toHaveLength(2);
  });
});
