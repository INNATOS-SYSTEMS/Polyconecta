import { Component, model, signal } from '@angular/core';

export interface ChatterEntry {
  author: string;
  timestamp: string;
  text: string;
}

/**
 * Réplica de Components/Chatter/OdooChatterDrawer.razor. Como en el prototipo, el mensaje se
 * agrega solo en local; el chatter en vivo por SignalR es la fase 2B de la spec 001.
 */
@Component({
  selector: 'pc-odoo-chatter-drawer',
  templateUrl: './odoo-chatter-drawer.html',
  styles: ':host { display: contents; }',
})
export class OdooChatterDrawer {
  readonly messages = model<ChatterEntry[]>([]);

  protected readonly newMsgText = signal('');

  protected send(): void {
    const texto = this.newMsgText();
    if (texto.trim() === '') return;
    this.messages.update(m => [...m, { author: 'Administrator', timestamp: horaCorta(new Date()), text: texto }]);
    this.newMsgText.set('');
  }

  protected inicial(author: string): string {
    return author ? author.substring(0, 1) : 'S';
  }
}

/** Formato "h:mm tt" de .NET: 9:05 AM. */
export function horaCorta(fecha: Date): string {
  const horas = fecha.getHours();
  const minutos = fecha.getMinutes().toString().padStart(2, '0');
  return `${horas % 12 === 0 ? 12 : horas % 12}:${minutos} ${horas < 12 ? 'AM' : 'PM'}`;
}
