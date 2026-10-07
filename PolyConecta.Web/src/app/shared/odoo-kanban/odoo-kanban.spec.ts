import { TestBed } from '@angular/core/testing';
import { Component, TemplateRef, viewChild } from '@angular/core';
import { describe, expect, it } from 'vitest';
import { TransicionKanban } from '../../core/kanban/kanban';
import { OrigenEnMemoria } from '../../core/lista/origen-en-memoria';
import { OdooKanban } from './odoo-kanban';

interface Doc { id: string; estado: string }

@Component({ template: '<ng-template #t let-d>{{ d.id }}</ng-template>' })
class Anfitrion {
  readonly t = viewChild.required<TemplateRef<{ $implicit: Doc }>>('t');
}

async function montar(docs: Doc[], transiciones: TransicionKanban<Doc>[]) {
  const anfitrion = TestBed.createComponent(Anfitrion);
  anfitrion.detectChanges();
  const f = TestBed.createComponent(OdooKanban<Doc>);
  f.componentRef.setInput('origen', new OrigenEnMemoria<Doc>({ datos: () => docs, id: d => d.id }));
  f.componentRef.setInput('campoEtapa', 'estado');
  f.componentRef.setInput('etapas', [{ valor: 'A', titulo: 'A' }, { valor: 'B', titulo: 'B' }, { valor: 'C', titulo: 'C', plegada: true }]);
  f.componentRef.setInput('transiciones', transiciones);
  f.componentRef.setInput('tarjeta', anfitrion.componentInstance.t());
  f.componentRef.setInput('idDe', (d: Doc) => d.id);
  f.componentRef.setInput('etapaDe', (d: Doc) => d.estado);
  f.detectChanges();
  await f.componentInstance.cargar();
  f.detectChanges();
  return f;
}

describe('OdooKanban', () => {
  it('pinta una columna por etapa con su cuenta y empieza plegadas las terminales', async () => {
    const f = await montar([{ id: '1', estado: 'A' }, { id: '2', estado: 'A' }, { id: '3', estado: 'B' }], []);
    expect(f.nativeElement.querySelector('[data-cuenta="A"]').textContent).toBe('2');
    expect(f.nativeElement.querySelector('[data-etapa="C"]').classList).toContain('o_kanban_plegada');
  });

  it('ejecuta la transición declarada y devuelve el motivo si no hay', async () => {
    const ejecutadas: string[] = [];
    const f = await montar([{ id: '1', estado: 'A' }], [{ desde: 'A', hacia: 'B', nombre: 'Avanzar', ejecutar: d => { ejecutadas.push(d.id); return undefined; } }]);
    const k = f.componentInstance;
    expect(await k.mover({ id: '1', estado: 'A' }, 'A', 'B')).toBeUndefined();
    expect(ejecutadas).toEqual(['1']);
    expect(await k.mover({ id: '1', estado: 'A' }, 'A', 'C')).toBe('No se puede pasar de A a C');
  });

  it('devuelve el motivo que da la transición cuando no procede', async () => {
    const f = await montar([], [{ desde: 'A', hacia: 'B', nombre: 'Avanzar', ejecutar: () => 'Falta la firma de Cobranza.' }]);
    expect(await f.componentInstance.mover({ id: '1', estado: 'A' }, 'A', 'B')).toBe('Falta la firma de Cobranza.');
  });
});
