import { expect, Locator, Page, test } from '@playwright/test';
import { ANGULAR, abrir } from '../soporte/apps';

/** Fabricación migrada a los componentes de la spec 011 (US3, aclaración P2). */
async function arrastrar(page: Page, origen: Locator, destino: Locator): Promise<void> {
  await origen.scrollIntoViewIfNeeded();
  const a = (await origen.boundingBox())!;
  const b = (await destino.boundingBox())!;
  await page.mouse.move(a.x + 20, a.y + 10);
  await page.mouse.down();
  await page.mouse.move(a.x + 40, a.y + 30, { steps: 5 });
  await page.mouse.move(b.x + b.width / 2, b.y + 60, { steps: 15 });
  await page.mouse.up();
}

const columna = (page: Page, etapa: string) => page.locator(`[data-etapa="${etapa}"]`);

test('kanban de OF: confirmar arrastrando; Planeado → En progreso no se arrastra; cerrar sin cumplir regresa con el motivo', async ({ page }) => {
  await abrir(page, ANGULAR, '/fabricacion');
  await page.locator('.o_view_switcher button').nth(1).click();
  const bol = (etapa: string) => columna(page, etapa).locator('[data-tarjeta="BOL-2026-0001"]');
  await arrastrar(page, bol('Borrador'), columna(page, 'Planeado'));
  await expect(bol('Planeado')).toHaveCount(1);

  await arrastrar(page, bol('Planeado'), columna(page, 'En progreso'));
  await expect(columna(page, 'Planeado').locator('[data-motivo]')).toHaveText('No se puede pasar de Planeado a En progreso');

  const enProgreso = columna(page, 'En progreso').locator('[data-tarjeta]').first();
  const folio = await enProgreso.getAttribute('data-tarjeta');
  await arrastrar(page, enProgreso, columna(page, 'Hecho'));
  await expect(columna(page, 'En progreso').locator('[data-motivo]')).not.toBeEmpty();
  await expect(columna(page, 'En progreso').locator(`[data-tarjeta="${folio}"]`)).toHaveCount(1);
});

test('lista de OF: conserva la cadena y la aplana al ordenar', async ({ page }) => {
  await abrir(page, ANGULAR, '/fabricacion');
  const tabla = page.locator('table[data-lista="produccion.ordenes"]');
  await expect(tabla.locator('tbody tr', { hasText: 'BOL-2026-0001' })).toContainText('maestra');
  await tabla.locator('th[data-columna="folio"]').click();
  await expect(tabla.locator('tbody tr').first()).toContainText('BOL-2026-0001');
  await expect(tabla.locator('tbody .badge', { hasText: 'maestra' })).toHaveCount(0);
});

test('incidencias: kanban por centro de trabajo, sin arrastre', async ({ page }) => {
  await abrir(page, ANGULAR, '/incidencias');
  await page.locator('.o_view_switcher button').nth(1).click();
  await expect(page.locator('[data-kanban="centroTrabajo"] .o_kanban_column').first()).toBeVisible();
  await expect(page.locator('[data-kanban="centroTrabajo"] .cdk-drag-disabled').first()).toBeVisible();
});
