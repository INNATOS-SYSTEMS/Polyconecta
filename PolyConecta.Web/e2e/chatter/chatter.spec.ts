import { expect, Page, test } from '@playwright/test';
import { abrir, ANGULAR } from '../soporte/apps';

/** Chatter en vivo entre pestañas por el hub de PolyConecta.Api (US-4, SC-005). */
const DOCUMENTO = '/produccion/fabricacion/BOL-2026-0001';
const PANEL = 'pc-odoo-chatter-drawer';

async function escribir(page: Page, texto: string): Promise<void> {
  const campo = page.locator(`${PANEL} input[placeholder="Escribir nota..."]`);
  await campo.fill(texto);
  await campo.press('Enter');
}

test('con la API: el mensaje llega a la otra pestaña en menos de 1 s', async ({ browser }) => {
  const contexto = await browser.newContext();
  const a = await contexto.newPage();
  const b = await contexto.newPage();
  for (const p of [a, b]) {
    await abrir(p, ANGULAR, DOCUMENTO);
    // Conectado: el aviso de sin conexión no aparece.
    await expect(p.locator(PANEL)).not.toContainText('Sin conexión en vivo');
  }
  // Las dos pestañas esperan a que el hub las tenga conectadas antes de medir.
  await a.waitForTimeout(500);

  const texto = `Nota en vivo ${Date.now()}`;
  const inicio = Date.now();
  await escribir(a, texto);
  await expect(b.locator(PANEL)).toContainText(texto, { timeout: 1000 });
  const ms = Date.now() - inicio;
  expect(ms, `llegó en ${ms} ms`).toBeLessThan(1000);

  // En la pestaña que lo envió aparece una sola vez (no se duplica el propio).
  await expect(a.locator(`${PANEL} .text-dark`, { hasText: texto })).toHaveCount(1);
  await expect(b.locator(`${PANEL} .text-dark`, { hasText: texto })).toHaveCount(1);
  // Otro documento no lo recibe.
  const c = await contexto.newPage();
  await abrir(c, ANGULAR, '/produccion/fabricacion/IMP-2026-0001');
  await escribir(a, `${texto} (2)`);
  await expect(b.locator(PANEL)).toContainText(`${texto} (2)`);
  await expect(c.locator(PANEL)).not.toContainText(texto);
  await contexto.close();
});

test('sin la API: "Sin conexión en vivo" y la nota se agrega solo en local', async ({ browser }) => {
  const contexto = await browser.newContext();
  // La API apagada se simula cortando el hub en el navegador; la API real sigue arriba para la otra prueba.
  await contexto.route('**/hubs/chatter/**', ruta => ruta.abort());
  const a = await contexto.newPage();
  const b = await contexto.newPage();
  await abrir(a, ANGULAR, DOCUMENTO);
  await abrir(b, ANGULAR, DOCUMENTO);

  const texto = `Nota local ${Date.now()}`;
  await escribir(a, texto);
  await expect(a.locator(PANEL)).toContainText(texto);
  await expect(a.locator(PANEL)).toContainText('Administrator');
  await expect(a.locator(PANEL)).toContainText('Sin conexión en vivo');
  await b.waitForTimeout(1000);
  await expect(b.locator(PANEL)).not.toContainText(texto);
  await contexto.close();
});
