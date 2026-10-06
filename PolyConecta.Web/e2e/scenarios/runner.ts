import { Page, expect, test } from '@playwright/test';
import { ANGULAR, BLAZOR, abrir } from '../soporte/apps';

/**
 * Guiones de escenario (spec 001, FR-018): la misma lista de pasos se ejecuta contra Blazor y contra
 * Angular, y se comparan los textos visibles de cada punto de control. Un guion `soloAngular` (modo
 * libre, que Blazor no tiene) solo verifica sus puntos de control contra lo esperado.
 */
export type Paso =
  | { ir: string }
  | { pulsar: string; texto?: string }
  | { capturar: string; valor: string; enter?: boolean }
  | { elegirLote: string; lote: string }
  | { control: string; en: string; esperado?: string | RegExp }
  | { recargar: true };

export interface Guion {
  nombre: string;
  pasos: Paso[];
  soloAngular?: boolean;
}

const normalizar = (texto: string): string => texto.replace(/\s+/g, ' ').trim();

async function ejecutar(page: Page, base: string, pasos: Paso[]): Promise<Record<string, string>> {
  const controles: Record<string, string> = {};
  for (const paso of pasos) {
    if ('ir' in paso) {
      await abrir(page, base, paso.ir);
    } else if ('pulsar' in paso) {
      const objetivo = paso.texto ? page.locator(paso.pulsar, { hasText: paso.texto }).first() : page.locator(paso.pulsar).first();
      await objetivo.click();
      await page.waitForTimeout(250);
    } else if ('capturar' in paso) {
      const campo = page.locator(paso.capturar).first();
      await campo.fill(paso.valor);
      await campo.dispatchEvent('change');
      if (paso.enter) await campo.press('Enter');
      await page.waitForTimeout(150);
    } else if ('elegirLote' in paso) {
      const campo = page.locator(paso.elegirLote).first();
      await campo.fill(paso.lote);
      await campo.press('Enter');
      await page.waitForTimeout(150);
    } else if ('recargar' in paso) {
      await page.reload({ waitUntil: 'networkidle' });
      await page.waitForTimeout(500);
    } else {
      const texto = normalizar(await page.locator(paso.en).first().innerText());
      controles[paso.control] = texto;
      if (paso.esperado !== undefined) expect(texto, paso.control).toMatch(paso.esperado);
    }
  }
  return controles;
}

/** Registra el guion como prueba de Playwright. */
export function guion(g: Guion): void {
  test(`escenario: ${g.nombre}`, async ({ page }) => {
    const angular = await ejecutar(page, ANGULAR, g.pasos);
    if (g.soloAngular) return;
    const blazor = await ejecutar(page, BLAZOR, g.pasos);
    for (const control of Object.keys(blazor)) {
      expect(angular[control], `punto de control "${control}"`).toBe(blazor[control]);
    }
  });
}
