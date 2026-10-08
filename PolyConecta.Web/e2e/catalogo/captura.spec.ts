import { test } from '@playwright/test';
import { ANGULAR, abrir } from '../soporte/apps';

/** Capturas por sección para revisar la galería y para 07-contratos-visuales.md (no es una prueba de comportamiento). */
test('captura de la galería por sección', async ({ page }) => {
  await abrir(page, ANGULAR, '/catalogo');
  await page.setViewportSize({ width: 1600, height: 6000 });
  await page.waitForTimeout(500);
  for (const s of await page.locator('[data-catalogo].o_catalogo_seccion').all()) {
    await s.screenshot({ path: `test-results/catalogo-${await s.getAttribute('data-catalogo')}.png` });
  }
});
