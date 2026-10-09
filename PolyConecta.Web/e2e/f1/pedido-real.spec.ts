import { Browser, expect, Page, test } from '@playwright/test';
import { ANGULAR } from '../soporte/apps';
import { entrarComo } from '../soporte/sesion';

/**
 * Pedido con dos firmas contra la API real (spec 003, quickstart §4; research R-10: las pruebas de Pedidos
 * van contra la API, con SQL Server y el bridge simulado). Necesita `./run.sh --with-bridge`, los catálogos
 * sincronizados y los usuarios de R1. Las pruebas de `pedido.spec.ts` cubren la pantalla con la API simulada.
 */
const estado = (page: Page) => page.locator('.o_statusbar_pipeline .arrow-step.active');
const chatter = (page: Page) => page.locator('pc-odoo-chatter-drawer');

async function como(browser: Browser, usuario: string, url: string): Promise<Page> {
  const contexto = await browser.newContext();
  await entrarComo(contexto, usuario);
  const page = await contexto.newPage();
  await page.goto(url);
  return page;
}

async function conMotivo(page: Page, boton: string, motivo: string): Promise<void> {
  await page.click(boton);
  const dialogo = page.locator('#dialogo-motivo');
  await expect(dialogo.locator('[data-dialogo="confirmar"], .btn-primary').first()).toBeDisabled();
  await dialogo.locator('#campo-motivo').fill(motivo);
  await dialogo.locator('.btn-primary').first().click();
}

test.describe('Pedido con dos firmas (F1 / US1, contra la API)', () => {
  test('AC captura y confirma, Comercial y Cobranza suplente firman, Comercial revoca y AC cancela con motivo', async ({ browser }) => {
    // 1. ac1 captura con "Nuevo": dos líneas con la unidad base del producto, fija.
    const ac = await como(browser, 'ac1', `${ANGULAR}/ventas/pedidos/nuevo`);
    await ac.selectOption('#campo-cliente', { index: 1 });
    for (const [producto, cantidad] of [[1, '120'], [2, '80']] as const) {
      await ac.selectOption('#campo-linea-producto', { index: producto });
      await expect(ac.locator('#campo-linea-unidad')).toBeDisabled();
      await ac.fill('#campo-linea-cantidad', cantidad);
      await ac.fill('#campo-linea-precio', '4.25');
      await ac.click('#btn-agregar-linea');
    }
    await ac.click('#btn-guardar-nuevo-pedido');
    await expect(ac).toHaveURL(/\/ventas\/pedidos\/\d+$/);
    const url = ac.url();
    await expect(ac.locator('#pedido-folio')).toHaveText(/^PV-\d{4}-\d{4}$/);
    await expect(estado(ac)).toHaveText('Borrador');

    // 2. Confirma.
    await ac.click('#btn-confirmar-pedido');
    await expect(estado(ac)).toHaveText('Confirmado');

    // 3. Primera firma: Comercial. Sigue Confirmado con 1 de 2.
    const comercial = await como(browser, 'comercial1', url);
    await comercial.click('#btn-autorizar-pedido');
    await expect(comercial.locator('#badge-firmas-pedido')).toHaveText('1/2 firmas');
    await expect(estado(comercial)).toHaveText('Confirmado');

    // Planner solo lee: no tiene ninguna acción del pedido.
    const planner = await como(browser, 'planner-pim', url);
    await expect(planner.locator('#pedido-folio')).toBeVisible();
    for (const boton of ['#btn-editar-pedido', '#btn-revocar-pedido', '#btn-cancelar-pedido']) {
      await expect(planner.locator(boton)).toHaveCount(0);
    }
    await expect(planner.locator('#btn-autorizar-pedido')).toBeDisabled();

    // 4. Segunda firma: Cobranza como suplente (D-38). Queda Autorizado.
    const suplente = await como(browser, 'cobranza-suplente', url);
    await suplente.click('#btn-autorizar-pedido');
    await expect(estado(suplente)).toHaveText('Autorizado');
    await expect(suplente.getByText('Suplente', { exact: true })).toBeVisible();

    // 5. Comercial revoca con motivo: regresa a Confirmado, sin firmas, y el motivo queda en el chatter (D-33).
    await comercial.reload();
    await conMotivo(comercial, '#btn-revocar-pedido', 'Cambió la fecha de entrega');
    await expect(estado(comercial)).toHaveText('Confirmado');
    await expect(comercial.locator('#badge-firmas-pedido')).toHaveText('0/2 firmas');
    await expect(chatter(comercial)).toContainText('Cambió la fecha de entrega');

    // 6. AC cancela con motivo y el pedido ya no se edita.
    await ac.reload();
    await conMotivo(ac, '#btn-cancelar-pedido', 'El cliente retiró la orden');
    await expect(estado(ac)).toHaveText('Cancelado');
    await expect(ac.locator('#btn-editar-pedido')).toHaveCount(0);
    await expect(chatter(ac)).toContainText('El cliente retiró la orden');
  });
});
