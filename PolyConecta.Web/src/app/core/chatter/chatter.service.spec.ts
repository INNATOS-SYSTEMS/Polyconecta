import { TestBed } from '@angular/core/testing';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { describe, expect, it } from 'vitest';
import { OdooChatterDrawer } from '../../shared/odoo-chatter-drawer/odoo-chatter-drawer';
import { CHATTER_CONEXION, ChatterService } from './chatter.service';

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
