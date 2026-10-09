import { Browser, expect, Page, test } from '@playwright/test';
import { ANGULAR } from '../soporte/apps';
import { entrarComo } from '../soporte/sesion';

/**
 * Chatter guardado del pedido con dos navegadores (spec 003, quickstart §5 paso 3, US4; L2-T032).
 * Contra la API real, sin simular: SignalR no se puede interceptar con `page.route`. Necesita la API
 * levantada con `./run.sh` y los usuarios de R1 (DatosR1, e2e/soporte/sesion.ts).
 */
async function entrar(browser: Browser, usuario: string): Promise<Page> {
  const contexto = await browser.newContext();
  await entrarComo(contexto, usuario);
  const page = await contexto.newPage();
  await page.goto(`${ANGULAR}/`);
  return page;
}

async function escribir(page: Page, clase: 'Mensaje' | 'Nota', texto: string): Promise<void> {
  const panel = page.locator('pc-odoo-chatter-drawer');
  await panel.locator(`[data-chatter-clase-opcion="${clase}"]`).click();
  const campo = panel.locator('input[placeholder="Escribir nota..."]');
  await campo.fill(texto);
  await campo.press('Enter');
}

test.describe('Chatter guardado del pedido (F1 / US4, contra la API)', () => {
  test('un mensaje llega en vivo al otro navegador, sigue al recargar y las transiciones dejan su Cambio', async ({ browser }) => {
    const ac = await entrar(browser, 'ac1');
    await ac.goto(`${ANGULAR}/ventas/pedidos/nuevo`);
    await ac.selectOption('#campo-cliente', { index: 1 });
    await ac.selectOption('#campo-linea-producto', { index: 1 });
    await ac.fill('#campo-linea-cantidad', '5');
    await ac.fill('#campo-linea-precio', '3');
    await ac.click('#btn-agregar-linea');
    await ac.click('#btn-guardar-nuevo-pedido');
    await expect(ac).toHaveURL(/\/ventas\/pedidos\/\d+$/);
    const url = ac.url();
    const panelAc = ac.locator('pc-odoo-chatter-drawer');
    await expect(panelAc.locator('[data-chatter-clase="Cambio"]').first()).toContainText('Nuevo → Borrador');

    const nombreAc = (await ac.locator('.o_user_name').textContent())!.trim();
    const comercial = await entrar(browser, 'comercial1');
    const nombreCom = (await comercial.locator('.o_user_name').textContent())!.trim();
    await comercial.goto(url);
    const panelCom = comercial.locator('pc-odoo-chatter-drawer');
    await expect(panelCom).toContainText('Nuevo → Borrador');
    await comercial.waitForTimeout(500); // que el hub los tenga a los dos en el grupo del documento

    const texto = `¿Se puede entregar el lunes? ${Date.now()}`;
    await escribir(ac, 'Mensaje', texto);
    await expect(panelCom).toContainText(texto, { timeout: 2000 });
    await expect(panelCom.locator('[data-chatter-clase="Mensaje"]', { hasText: texto })).toContainText(nombreAc);
    await expect(panelAc.locator('[data-chatter-clase="Mensaje"]', { hasText: texto })).toHaveCount(1);

    const nota = `Nota interna ${Date.now()}`;
    await escribir(comercial, 'Nota', nota);
    await expect(panelAc.locator('[data-chatter-clase="Nota"]', { hasText: nota })).toContainText(nombreCom);

    // La confirmación escribe su Cambio sin que nadie lo teclee, y llega en vivo al otro navegador.
    await ac.click('#btn-confirmar-pedido');
    await expect(panelCom.locator('[data-chatter-clase="Cambio"]', { hasText: 'Borrador → Confirmado' })).toBeVisible();

    await comercial.reload();
    await expect(panelCom).toContainText(texto);
    await expect(panelCom).toContainText(nota);
    await expect(panelCom.locator('[data-chatter-clase="Cambio"]', { hasText: 'Borrador → Confirmado' })).toContainText('Atención a Clientes');
  });
});
