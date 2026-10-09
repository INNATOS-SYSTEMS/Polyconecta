import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { BrowserContext, expect } from '@playwright/test';
import { ANGULAR } from './apps';

/**
 * Contraseña de los usuarios de R1 (DatosR1, L2-T033): `R1_PASSWORD` o `LOCAL_R1_PASSWORD` de `.env.local`,
 * que genera `./run.sh`. Las pruebas que van contra la API real entran con ellos.
 */
export function contrasenaR1(): string {
  if (process.env['R1_PASSWORD']) return process.env['R1_PASSWORD'];
  const env = readFileSync(resolve(__dirname, '../../../.env.local'), 'utf8');
  const linea = env.split('\n').find(l => l.startsWith('LOCAL_R1_PASSWORD='));
  if (!linea) throw new Error('Falta LOCAL_R1_PASSWORD en .env.local: levanta la API con ./run.sh.');
  return linea.slice('LOCAL_R1_PASSWORD='.length);
}

/** Entra con un usuario de R1 en el contexto: la cookie queda para todas sus páginas. */
export async function entrarComo(contexto: BrowserContext, usuario: string): Promise<void> {
  const page = await contexto.newPage();
  await page.goto(`${ANGULAR}/login`);
  await page.locator('[data-login-usuario]').fill(usuario);
  await page.locator('[data-login-contrasena]').fill(contrasenaR1());
  await page.locator('[data-login-submit]').click();
  await expect(page).not.toHaveURL(/\/login/);
  await page.close();
}
