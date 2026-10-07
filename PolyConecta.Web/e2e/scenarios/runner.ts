import { Page, expect, test } from '@playwright/test';
import { ANGULAR, BLAZOR, abrir } from '../soporte/apps';

/**
 * Guiones de escenario (spec 001, FR-018): la misma lista de pasos se ejecuta contra Blazor y contra
 * Angular, y se comparan los textos visibles de cada punto de control. Un guion `soloAngular` (modo
 * libre, que Blazor no tiene) solo verifica sus puntos de control contra lo esperado. Desde la spec 011 ya
 * no se comparan píxeles (D-135): los componentes nuevos cambian el acabado, no los textos ni el flujo.
 */
export type Paso =
  | { ir: string }
  /** Cambia de ruta sin recargar, para conservar el estado (Blazor lo pierde al abrir otro circuito). */
  | { navegar: string }
  | { pulsar: string; texto?: string }
  | { capturar: string; valor: string; enter?: boolean }
  | { elegirLote: string; lote: string }
  /** Elige una opción de un <select> por su valor. */
  | { elegir: string; valor: string }
  | { control: string; en: string; esperado?: string | RegExp }
  /** Punto de control sobre si un botón está habilitado (el texto no lo dice). */
  | { control: string; habilitado: string; texto?: string; esperado?: boolean }
  /** Punto de control sobre el valor de un campo y si se puede editar (innerText no los incluye). */
  | { control: string; campo: string; esperado?: string | RegExp; editable?: boolean }
  | { recargar: true };

export interface Guion {
  nombre: string;
  pasos: Paso[];
  soloAngular?: boolean;
}

const normalizar = (texto: string): string => texto.replace(/\s+/g, ' ').trim();


interface Corrida {
  controles: Record<string, string>;
}

async function ejecutar(page: Page, base: string, pasos: Paso[]): Promise<Corrida> {
  const controles: Record<string, string> = {};
  for (const paso of pasos) {
    if ('ir' in paso) {
      await abrir(page, base, paso.ir);
    } else if ('navegar' in paso) {
      const ruta = paso.navegar;
      await page.evaluate(r => {
        const w = window as unknown as { Blazor?: { navigateTo(u: string): void }; __sinRecarga?: boolean };
        w.__sinRecarga = true;
        if (w.Blazor) w.Blazor.navigateTo(r);
        else {
          history.pushState({}, '', r);
          dispatchEvent(new PopStateEvent('popstate', { state: {} }));
        }
      }, ruta);
      await page.waitForURL(u => decodeURIComponent(u.pathname) === ruta);
      await page.waitForTimeout(400);
      const conservo = await page.evaluate(() => (window as unknown as { __sinRecarga?: boolean }).__sinRecarga === true);
      expect(conservo, `navegar a ${ruta} recargó la página y perdió el estado`).toBe(true);
    } else if ('pulsar' in paso) {
      const objetivo = paso.texto ? page.locator(paso.pulsar, { hasText: paso.texto }).first() : page.locator(paso.pulsar).first();
      await objetivo.click({ timeout: 5000 });
      await page.waitForTimeout(250);
    } else if ('capturar' in paso) {
      const campo = page.locator(paso.capturar).first();
      await campo.fill(paso.valor);
      await campo.dispatchEvent('change');
      if (paso.enter) await campo.press('Enter');
      await page.waitForTimeout(150);
    } else if ('elegir' in paso) {
      await page.locator(paso.elegir).first().selectOption(paso.valor);
      await page.waitForTimeout(150);
    } else if ('elegirLote' in paso) {
      const campo = page.locator(paso.elegirLote).first();
      await campo.fill(paso.lote);
      await campo.press('Enter');
      await page.waitForTimeout(150);
    } else if ('recargar' in paso) {
      await page.reload({ waitUntil: 'networkidle' });
      await page.waitForTimeout(500);
    } else if ('campo' in paso) {
      const campo = page.locator(paso.campo).first();
      const valor = await campo.inputValue();
      const editable = await campo.isEditable();
      controles[paso.control] = `${valor} (${editable ? 'editable' : 'no editable'})`;
      if (paso.esperado !== undefined) expect(valor, paso.control).toMatch(paso.esperado);
      if (paso.editable !== undefined) expect(editable, `${paso.control}: editable`).toBe(paso.editable);
    } else if ('habilitado' in paso) {
      const boton = paso.texto ? page.locator(paso.habilitado, { hasText: paso.texto }).first() : page.locator(paso.habilitado).first();
      const habilitado = await boton.isEnabled();
      controles[paso.control] = habilitado ? 'habilitado' : 'deshabilitado';
      if (paso.esperado !== undefined) expect(habilitado, paso.control).toBe(paso.esperado);
    } else {
      const texto = normalizar(await page.locator(paso.en).first().innerText());
      controles[paso.control] = texto;
      if (paso.esperado !== undefined) expect(texto, paso.control).toMatch(paso.esperado);
    }
  }
  return { controles };
}

/** Registra el guion como prueba de Playwright. */
export function guion(g: Guion): void {
  test(`escenario: ${g.nombre}`, async ({ page }) => {
    // Blazor primero: si el guion falla ahí, el error es del guion; si falla solo en Angular, es una diferencia.
    const blazor = g.soloAngular ? undefined : await ejecutar(page, BLAZOR, g.pasos).catch(e => { throw new Error(`[Blazor] ${e.message}`); });
    const angular = await ejecutar(page, ANGULAR, g.pasos).catch(e => { throw new Error(`[Angular] ${e.message}`); });
    if (!blazor) return;
    for (const control of Object.keys(blazor.controles)) {
      expect(angular.controles[control], `punto de control "${control}"`).toBe(blazor.controles[control]);
    }
  });
}
