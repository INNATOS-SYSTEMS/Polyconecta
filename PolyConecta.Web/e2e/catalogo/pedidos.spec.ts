import { expect, Locator, Page, test } from '@playwright/test';
import { ANGULAR, abrir } from '../soporte/apps';

/** Pedidos migrado a los componentes de la spec 011 (US2): kanban con transiciones y lista agrupada. */
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
const tarjeta = (page: Page, etapa: string) => columna(page, etapa).locator('[data-tarjeta="IV310-26"]');

test('kanban de pedidos: confirmar y autorizar con las dos firmas arrastrando', async ({ page }) => {
  await abrir(page, ANGULAR, '/pedidos');
  await page.locator('.o_view_switcher button').nth(1).click();
  await expect(tarjeta(page, 'Borrador')).toHaveCount(1);
  await arrastrar(page, tarjeta(page, 'Borrador'), columna(page, 'Hecho'));
  await expect(columna(page, 'Borrador').locator('[data-motivo]')).toHaveText('No se puede pasar de Borrador a Hecho');

  await arrastrar(page, tarjeta(page, 'Borrador'), columna(page, 'Confirmado'));
  await expect(tarjeta(page, 'Confirmado')).toHaveCount(1);

  await arrastrar(page, tarjeta(page, 'Confirmado'), columna(page, 'Autorizado'));
  await expect(page.locator('[data-firma-pendiente]')).toHaveText('Comercial');
  await page.locator('.o_dialog [data-dialogo="confirmar"]').click();
  await expect(columna(page, 'Confirmado').locator('[data-motivo]')).toHaveText('Falta la firma de Cobranza.');
  await expect(tarjeta(page, 'Confirmado')).toHaveCount(1);

  await arrastrar(page, tarjeta(page, 'Confirmado'), columna(page, 'Autorizado'));
  await expect(page.locator('[data-firma-pendiente]')).toHaveText('Cobranza');
  await page.locator('.o_dialog [data-dialogo="confirmar"]').click();
  await expect(tarjeta(page, 'Autorizado')).toHaveCount(1);

  // La misma regla que el formulario: el pedido quedó autorizado con las dos firmas.
  await tarjeta(page, 'Autorizado').click();
  await expect(page.locator('main')).toContainText('Autorización de Cobranza firmada por Crédito y Cobranza');
});

test('lista de pedidos: agrupa por estado y abre el pedido', async ({ page }) => {
  await abrir(page, ANGULAR, '/pedidos');
  await page.locator('button[title="Filtros del modelo"]').click();
  await page.locator('.o_search_menu_item', { hasText: 'Estado' }).last().click();
  await page.locator('button[title="Filtros del modelo"]').click(); // el menú del prototipo se cierra con su botón
  const grupo = page.locator('table[data-lista="ventas.pedidos"] .o_group_row', { hasText: 'Borrador' });
  await expect(grupo).toContainText('(1)');
  await grupo.click();
  await page.locator('table[data-lista="ventas.pedidos"] tbody tr', { hasText: 'IV310-26' }).click();
  await expect(page).toHaveURL(/\/pedidos\/IV310-26$/);
});
