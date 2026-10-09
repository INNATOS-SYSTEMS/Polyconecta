import { defineConfig } from '@playwright/test';
import base from '../playwright.config';

/**
 * Prueba del chatter en vivo de las pantallas en memoria (L2-T066, SC-005). Desde F1 el hub exige sesión:
 * necesita la API de `./run.sh` con su base y los usuarios de R1, igual que la sesión global de la base.
 */
export default defineConfig({
  ...base,
  testDir: '.',
  testMatch: ['chatter.spec.ts'],
  globalTeardown: undefined,
  webServer: (Array.isArray(base.webServer) ? base.webServer : []).map(w => ({ ...w, cwd: '../..' })),
});
