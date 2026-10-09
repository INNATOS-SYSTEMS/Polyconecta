import { request } from '@playwright/test';
import { resolve } from 'node:path';
import { contrasenaR1 } from './sesion';

/** Donde queda la sesión que comparten las pruebas de un arnés (git la ignora). */
export const SESION_GUARDADA = resolve(__dirname, '../.auth/sesion.json');

/**
 * `globalSetup` de los arneses que recorren la réplica (escenarios, auditor, tablero y galería): desde F1 toda
 * ruta exige sesión (US2 escenario 2), así que entran una vez con un usuario de R1 contra la API y guardan la
 * cookie en `storageState`. Necesitan la API arriba (`./run.sh`). `E2E_USUARIO` cambia el usuario.
 */
export default async function iniciarSesion(): Promise<void> {
  const usuario = process.env['E2E_USUARIO'] ?? 'planner-pim';
  const api = await request.newContext({ baseURL: process.env['API_URL'] ?? 'http://localhost:9020' });
  const r = await api
    .post('/api/v1/plataforma/sesion', {
      headers: { 'X-Requested-With': 'PolyConecta' },
      data: { usuario, contrasena: contrasenaR1() },
    })
    .catch(e => {
      throw new Error(`Sin API en :9020 para entrar como ${usuario}: levántala con ./run.sh (${(e as Error).message}).`);
    });
  if (!r.ok()) throw new Error(`No se pudo entrar como ${usuario}: ${r.status()} ${await r.text()}`);
  await api.storageState({ path: SESION_GUARDADA });
  await api.dispose();
}
