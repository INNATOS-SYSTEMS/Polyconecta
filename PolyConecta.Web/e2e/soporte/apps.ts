import { Page } from '@playwright/test';
import { rutaAngular, rutaPrototipo } from './rutas';

export const BLAZOR = 'http://localhost:9010';
export const ANGULAR = 'http://localhost:9000';

const SIN_ANIMACIONES = '*, *::before, *::after { transition: none !important; animation: none !important; caret-color: transparent !important; }';

/**
 * Abre una ruta y espera a que esté lista para capturar: fuentes cargadas (Inter y Bootstrap Icons
 * del CDN) y, en Blazor, el circuito interactivo conectado. Si las fuentes no cargan, falla con ese
 * motivo explícito y no por diferencia de píxeles (caso borde de la spec).
 */
export async function abrir(page: Page, base: string, ruta: string): Promise<void> {
  // La réplica lleva el prefijo de su módulo (D-155): una ruta del prototipo se traduce al abrir Angular.
  if (base === ANGULAR) ruta = rutaAngular(ruta);
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

/** Ruta de la página en la forma del prototipo: en Angular quita el prefijo de módulo (D-155) para comparar. */
export function rutaComparable(page: Page): string {
  const url = new URL(page.url());
  const ruta = decodeURIComponent(url.pathname);
  return url.origin === ANGULAR ? rutaPrototipo(ruta) : ruta;
}
