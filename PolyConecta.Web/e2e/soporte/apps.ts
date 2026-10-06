import { Page } from '@playwright/test';

export const BLAZOR = 'http://localhost:9000';
export const ANGULAR = 'http://localhost:4200';

const SIN_ANIMACIONES = '*, *::before, *::after { transition: none !important; animation: none !important; caret-color: transparent !important; }';

/**
 * Abre una ruta y espera a que esté lista para capturar: fuentes cargadas (Inter y Bootstrap Icons
 * del CDN) y, en Blazor, el circuito interactivo conectado. Si las fuentes no cargan, falla con ese
 * motivo explícito y no por diferencia de píxeles (caso borde de la spec).
 */
export async function abrir(page: Page, base: string, ruta: string): Promise<void> {
  await page.goto(base + ruta, { waitUntil: 'networkidle' });
  await page.addStyleTag({ content: SIN_ANIMACIONES });
  const fuentes = await page.evaluate(async () => {
    await document.fonts.ready;
    return {
      inter: document.fonts.check('16px Inter'),
      iconos: document.fonts.check('16px bootstrap-icons'),
    };
  });
  if (!fuentes.inter || !fuentes.iconos) {
    throw new Error(`No cargaron las fuentes del CDN en ${base}${ruta} (Inter: ${fuentes.inter}, iconos: ${fuentes.iconos}).`);
  }
  if (base === BLAZOR) {
    await page.waitForFunction(() => (window as unknown as { Blazor?: unknown }).Blazor !== undefined);
  }
  await page.waitForTimeout(300);
}
