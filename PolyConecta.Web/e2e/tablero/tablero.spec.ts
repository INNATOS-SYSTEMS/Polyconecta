import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { expect, test } from '@playwright/test';
import { ANGULAR, abrir } from '../soporte/apps';
import { FLUJOS, archivo } from './pantallas';

/** Captura cada pantalla del tablero de flujo (spec 011) en `tablero-report/`, con su índice. */
const DIRECTORIO = join(__dirname, '..', '..', 'tablero-report');

for (const flujo of FLUJOS) {
  for (const pantalla of flujo.pantallas) {
    test(`tablero ${pantalla.ruta}`, async ({ page }) => {
      await abrir(page, ANGULAR, pantalla.ruta);
      await expect(page.getByText('Página no encontrada')).toHaveCount(0);
      // El layout desplaza el contenido dentro de un contenedor, no la página: se agranda la ventana
      // a lo que mide lo desplazable para que la captura muestre la pantalla completa.
      const extra = await page.evaluate(() =>
        Math.max(0, ...Array.from(document.querySelectorAll<HTMLElement>('*')).map(e => e.scrollHeight - e.clientHeight)));
      if (extra > 0) {
        await page.setViewportSize({ width: 1600, height: 900 + extra });
        await page.waitForTimeout(300);
      }
      mkdirSync(DIRECTORIO, { recursive: true });
      writeFileSync(join(DIRECTORIO, archivo(pantalla.ruta)), await page.screenshot());
    });
  }
}

test.afterAll(() => {
  mkdirSync(DIRECTORIO, { recursive: true });
  writeFileSync(join(DIRECTORIO, 'pantallas.json'), JSON.stringify(FLUJOS.map(f => ({ ...f, pantallas: f.pantallas.map(p => ({ ...p, archivo: archivo(p.ruta) })) })), null, 2));
});
