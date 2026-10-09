import { Browser, expect, Page, test } from '@playwright/test';
import { ANGULAR } from '../soporte/apps';
import { botonBarra, capturarLinea, claveProducto, conMotivo, accionEngranaje, elegir, etapa, pestana } from '../soporte/pedido';
import { entrarComo } from '../soporte/sesion';

/**
 * Pedido con dos firmas contra la API real (spec 003, quickstart §4; research R-10: las pruebas de Pedidos
 * van contra la API, con SQL Server y el bridge simulado). Necesita `./run.sh --with-bridge`, los catálogos
 * sincronizados y los usuarios de R1. Las pruebas de `pedido.spec.ts` cubren la pantalla con la API simulada.
 */
const chatter = (page: Page) => page.locator('pc-odoo-chatter-drawer');

async function como(browser: Browser, usuario: string, url: string): Promise<Page> {
  const contexto = await browser.newContext();
  await entrarComo(contexto, usuario);
  const page = await contexto.newPage();
  await page.goto(url);
  return page;
}

test.describe('Pedido con dos firmas (F1 / US1, contra la API)', () => {
  test('AC captura y confirma, Comercial y Cobranza suplente firman, Comercial revoca y AC cancela con motivo', async ({ browser }) => {
    // 1. ac1 captura con "Nuevo": dos líneas con la unidad base del producto, fija.
    const ac = await como(browser, 'ac1', `${ANGULAR}/ventas/pedidos/nuevo`);
    await elegir(ac, 'Cliente');
    for (const [producto, cantidad] of [[1, '120'], [2, '80']] as const) {
      await capturarLinea(ac, await claveProducto(ac, producto), cantidad, '4.25');
    }
    await expect(ac.locator('[data-lineas-pedido] tbody tr')).toHaveCount(2);
    await botonBarra(ac, 'Guardar').click();
    await expect(ac).toHaveURL(/\/ventas\/pedidos\/\d+$/);
    const url = ac.url();
    await expect(ac.locator('#pedido-folio')).toHaveText(/^PV-\d{4}-\d{4}$/);
    await expect(etapa(ac)).toHaveText('Borrador');

    // 2. Confirma.
    await ac.click('#btn-confirmar-pedido');
    await expect(etapa(ac)).toHaveText('Confirmado');

    // 3. Primera firma: Comercial. Sigue Confirmado con 1 de 2.
    const comercial = await como(browser, 'comercial1', url);
    await comercial.click('#btn-autorizar-pedido');
    await expect(comercial.locator('#badge-firmas-pedido')).toHaveText('1/2');
    await expect(etapa(comercial)).toHaveText('Confirmado');

    // Planner solo lee: el maestro es texto y el engranaje no le ofrece nada.
    const planner = await como(browser, 'planner-pim', url);
    await expect(planner.locator('#pedido-folio')).toBeVisible();
    await expect(planner.locator('[data-many2one="Cliente"]')).toHaveCount(0);
    await expect(planner.locator('.o_line_capture')).toHaveCount(0);
    await expect(planner.locator('#btn-autorizar-pedido')).toBeDisabled();
    if (await planner.locator('[data-acciones]').count()) {
      await expect(await accionEngranaje(planner, 'Cancelar pedido')).toBeDisabled();
    }

    // 4. Segunda firma: Cobranza como suplente (D-38). Queda Autorizado.
    const suplente = await como(browser, 'cobranza-suplente', url);
    await suplente.click('#btn-autorizar-pedido');
    await expect(etapa(suplente)).toHaveText('Autorizado');
    await pestana(suplente, 'firmas');
    await expect(suplente.locator('[data-firmas-pedido]')).toContainText('Suplente');

    // 5. Comercial revoca con motivo: regresa a Confirmado, sin firmas, y el motivo queda en el chatter (D-33).
    await comercial.reload();
    await conMotivo(comercial, 'Revocar autorización', 'Cambió la fecha de entrega');
    await expect(etapa(comercial)).toHaveText('Confirmado');
    await expect(comercial.locator('#badge-firmas-pedido')).toHaveText('0/2');
    await expect(chatter(comercial)).toContainText('Cambió la fecha de entrega');

    // 6. AC cancela con motivo y el pedido ya no se edita.
    await ac.reload();
    await conMotivo(ac, 'Cancelar pedido', 'El cliente retiró la orden');
    await expect(etapa(ac)).toHaveText('Cancelado');
    await expect(ac.locator('[data-many2one="Cliente"]')).toHaveCount(0);
    await expect(chatter(ac)).toContainText('El cliente retiró la orden');
  });
});
