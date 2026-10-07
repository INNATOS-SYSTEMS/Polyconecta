import { expect, Page, test } from '@playwright/test';
import { abrir, ANGULAR, BLAZOR } from '../soporte/apps';

/**
 * Navegación igual en las dos aplicaciones (L2-T036): enlace directo, folio inexistente y
 * atrás/adelante del navegador. Cada caso corre los mismos pasos en Blazor y en Angular y compara.
 */

/** Ruta relativa y título del documento, lo que debe coincidir entre las dos aplicaciones. */
async function donde(page: Page): Promise<{ ruta: string; titulo: string }> {
  const url = new URL(page.url());
  return { ruta: url.pathname, titulo: await page.title() };
}

/** Texto visible del área de contenido, sin la barra superior. */
async function contenido(page: Page): Promise<string> {
  return (await page.locator('main, .o_content, body').first().innerText()).replace(/\s+/g, ' ').trim();
}

for (const [nombre, base] of [['Blazor', BLAZOR], ['Angular', ANGULAR]] as const) {
  test.describe(`navegación ${nombre}`, () => {
    test('enlace directo abre el documento sin pasar por la lista', async ({ page }) => {
      await abrir(page, base, '/fabricacion/BOL-2026-0001');
      expect((await donde(page)).ruta).toBe('/fabricacion/BOL-2026-0001');
      await expect(page.getByText('BOL-2026-0001').first()).toBeVisible();
      await expect(page.getByText('Orden de Fabricación no encontrada.')).toHaveCount(0);
    });

    test('folio inexistente muestra el "no encontrado" del documento', async ({ page }) => {
      await abrir(page, base, '/fabricacion/NO-EXISTE-0000');
      await expect(page.getByText('Orden de Fabricación no encontrada.')).toBeVisible();
    });

  });
}

/**
 * Ruta inexistente: Blazor responde un 404 vacío del servidor (su <NotFound> de Routes.razor no llega
 * a mostrarse al cargar directo). Una SPA no puede dar ese 404, así que la réplica muestra el contenido
 * de ese <NotFound>. Diferencia legítima, registrada en "Exploración y cambios" de la spec.
 */
test('ruta inexistente: Blazor da 404 y Angular el <NotFound> de Routes.razor', async ({ page, request }) => {
  expect((await request.get(BLAZOR + '/no-existe')).status()).toBe(404);
  await abrir(page, ANGULAR, '/no-existe');
  await expect(page.getByRole('heading', { name: 'Página no encontrada' })).toBeVisible();
  await expect(page.getByText('La ruta solicitada no existe en PolyConecta.')).toBeVisible();
});

test('atrás y adelante dejan la misma URL y el mismo título en las dos aplicaciones', async ({ browser }) => {
  const recorrido = async (base: string) => {
    const page = await browser.newPage();
    const pasos: { ruta: string; titulo: string }[] = [];
    await abrir(page, base, '/pedidos');
    pasos.push(await donde(page));
    await page.getByText('IV310-26').first().click();
    await page.waitForURL(/\/pedidos\/IV310-26$/);
    pasos.push(await donde(page));
    await page.goBack();
    await page.waitForURL(/\/pedidos$/);
    pasos.push(await donde(page));
    await page.goForward();
    await page.waitForURL(/\/pedidos\/IV310-26$/);
    pasos.push(await donde(page));
    const texto = await contenido(page);
    await page.close();
    return { pasos, texto };
  };
  const blazor = await recorrido(BLAZOR);
  const angular = await recorrido(ANGULAR);
  expect(angular.pasos).toEqual(blazor.pasos);
  expect(angular.pasos.map(p => p.ruta)).toEqual(['/pedidos', '/pedidos/IV310-26', '/pedidos', '/pedidos/IV310-26']);
  // Tras volver adelante se ve el mismo documento (no una página en blanco ni la lista).
  expect(angular.texto).toContain('IV310-26');
  expect(blazor.texto).toContain('IV310-26');
});
