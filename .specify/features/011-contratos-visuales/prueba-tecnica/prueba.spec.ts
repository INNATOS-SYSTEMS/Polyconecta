import { expect, test, type Page } from '@playwright/test';

const URL = 'http://localhost:9100/prueba-tecnica';
const sel = (p: string) => `[data-prueba="${p}"]`;
const folios = (page: Page) => page.locator(`${sel('fila')} td:nth-child(2)`).allInnerTexts();

test.describe('prueba técnica spec 011', () => {
  test.beforeEach(async ({ page }) => { await page.goto(URL); await expect(page.locator(sel('fila'))).toHaveCount(10); });

  test('tabla: paginación y filas por página en el servidor', async ({ page }) => {
    await expect(page.locator(sel('total'))).toHaveText('57');
    await expect(page.locator(sel('pagina'))).toHaveText('1 / 6');
    await page.click(sel('siguiente'));
    await expect(page.locator(sel('pagina'))).toHaveText('2 / 6');
    expect((await folios(page))[0]).toBe('PV-2026-0011');
    await page.selectOption(sel('tamano'), '20');
    await expect(page.locator(sel('fila'))).toHaveCount(20);
    await expect(page.locator(sel('pagina'))).toHaveText('1 / 3');
  });

  test('tabla: ordenar pide al origen y ordena las 57, no solo la página', async ({ page }) => {
    const antes = Number(await page.locator(sel('consultas')).innerText());
    await page.click(sel('col-total')); // ascendente
    await expect(page.locator(sel('col-total'))).toContainText('▲');
    await page.click(sel('col-total')); // descendente
    await expect(page.locator(sel('col-total'))).toContainText('▼');
    expect(Number(await page.locator(sel('consultas')).innerText())).toBeGreaterThan(antes);
    const totales = await page.locator(`${sel('fila')} td:nth-child(5)`).allInnerTexts();
    const nums = totales.map(Number);
    expect(nums).toEqual([...nums].sort((a, b) => b - a));
    expect(nums[0]).toBe(Math.max(...Array.from({ length: 57 }, (_, i) => 1000 + ((i * 3779) % 50000))));
  });

  test('tabla: filtro en el servidor', async ({ page }) => {
    await page.fill(sel('filtro'), 'norte');
    await expect(page.locator(sel('total'))).toHaveText(/^1[12]$/);
    for (const c of await page.locator(`${sel('fila')} td:nth-child(3)`).allInnerTexts()) expect(c).toBe('BOLSAS DEL NORTE');
  });

  test('tabla: agrupar en el servidor, con conteo y total, y abrir un grupo', async ({ page }) => {
    await page.selectOption(sel('agrupar'), 'cliente');
    await expect(page.locator(sel('grupo'))).toHaveCount(5);
    await expect(page.locator(sel('fila'))).toHaveCount(0);
    const g = page.locator(sel('grupo')).filter({ hasText: 'EMPRESA MEXICANA' });
    await expect(g).toContainText('(12)');
    await g.click();
    await expect(page.locator(sel('fila'))).toHaveCount(12);
    await g.click();
    await expect(page.locator(sel('fila'))).toHaveCount(0);
  });

  test('tabla: ocultar y reordenar columnas', async ({ page }) => {
    await page.uncheck(sel('ver-estado'));
    await expect(page.locator(sel('col-estado'))).toHaveCount(0);
    await page.click(sel('mover-total'));
    await expect(page.locator('thead th').nth(1)).toHaveAttribute('data-prueba', 'col-total');
  });

  test('tabla: exportar a Excel las seleccionadas y, sin selección, todo el filtro', async ({ page }) => {
    await page.locator(`${sel('fila')} input`).nth(0).click();
    await page.locator(`${sel('fila')} input`).nth(2).click();
    await page.uncheck(sel('ver-estado'));
    await page.click(sel('exportar'));
    await page.waitForFunction(() => (window as any).__ultimaExportacion);
    const a = await page.evaluate(() => (window as any).__ultimaExportacion);
    expect(a).toMatchObject({ filas: 2, columnas: ['folio', 'cliente', 'total'], zip: true });
    await page.locator(`${sel('fila')} input`).nth(0).click();
    await page.locator(`${sel('fila')} input`).nth(2).click();
    await page.evaluate(() => { delete (window as any).__ultimaExportacion; });
    await page.click(sel('exportar'));
    await page.waitForFunction(() => (window as any).__ultimaExportacion);
    expect((await page.evaluate(() => (window as any).__ultimaExportacion)).filas).toBe(57);
  });

  test('kanban: transición válida, inválida y con diálogo', async ({ page }) => {
    const arrastrar = async (folio: string, etapa: string) => {
      const t = page.locator(sel(`tarjeta-${folio}`)).boundingBox();
      const e = page.locator(sel(`etapa-${etapa}`)).boundingBox();
      const [bt, be] = await Promise.all([t, e]);
      await page.mouse.move(bt!.x + 20, bt!.y + 10);
      await page.mouse.down();
      await page.mouse.move(bt!.x + 40, bt!.y + 30, { steps: 5 });
      await page.mouse.move(be!.x + 60, be!.y + 80, { steps: 15 });
      await page.mouse.up();
    };
    await arrastrar('PV-1', 'Confirmado');
    await expect(page.locator(`${sel('etapa-Confirmado')} ${sel('tarjeta-PV-1')}`)).toHaveCount(1);
    await arrastrar('PV-2', 'Hecho');
    await expect(page.locator(sel('motivo'))).toHaveText('No se puede pasar de Borrador a Hecho');
    await expect(page.locator(`${sel('etapa-Borrador')} ${sel('tarjeta-PV-2')}`)).toHaveCount(1);
    await arrastrar('PV-3', 'Autorizado');
    await expect(page.locator(sel('dialogo-firma'))).toBeVisible();
    await page.click(sel('cancelar-firma'));
    await expect(page.locator(`${sel('etapa-Confirmado')} ${sel('tarjeta-PV-3')}`)).toHaveCount(1);
    await arrastrar('PV-3', 'Autorizado');
    await page.click(sel('firmar'));
    await expect(page.locator(`${sel('etapa-Autorizado')} ${sel('tarjeta-PV-3')}`)).toHaveCount(1);
  });

  test('combobox: filtra al escribir y selecciona con teclado', async ({ page }) => {
    await page.click(sel('combo-input'));
    await page.keyboard.type('MP00');
    await expect(page.locator('[data-prueba^="op-"]:visible')).toHaveCount(2);
    await expect(page.locator(sel('combo-vacio'))).toBeHidden();
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press('Enter');
    await expect(page.locator(sel('combo-valor'))).toHaveText(/MP000[12]/);
    await page.click(sel('combo-input'));
    await page.keyboard.press('ControlOrMeta+A');
    await page.keyboard.type('zzz');
    await expect(page.locator(sel('combo-vacio'))).toBeVisible();
  });

  test('captura de la página', async ({ page }) => {
    await page.selectOption(sel('agrupar'), 'cliente');
    await page.locator(sel('grupo')).first().click();
    await page.screenshot({ path: 'prueba-tecnica.png', fullPage: true });
  });

  test('calendario: navega por mes en español y elige una fecha', async ({ page }) => {
    await expect(page.locator(sel('fecha-boton'))).toHaveText('7 oct 2026');
    await page.click(sel('fecha-boton'));
    await expect(page.locator(sel('cal-titulo'))).toHaveText('octubre 2026');
    await page.click('[aria-label="Mes siguiente"]');
    await expect(page.locator(sel('cal-titulo'))).toHaveText('noviembre 2026');
    await page.click(sel('dia-15-10'));
    await expect(page.locator(sel('fecha-boton'))).toHaveText('15 nov 2026');
  });
});
