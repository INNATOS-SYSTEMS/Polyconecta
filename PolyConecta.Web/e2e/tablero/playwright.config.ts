import { resolve } from 'node:path';
import { defineConfig } from '@playwright/test';
import { SESION_GUARDADA } from '../soporte/sesion-global';

/** Tablero de flujo (spec 011): solo Angular en :9000, a 1600×900 y sin animaciones, como la paridad. */
export default defineConfig({
  testDir: '.',
  testMatch: ['tablero.spec.ts'],
  timeout: 60_000,
  fullyParallel: false,
  workers: 1,
  reporter: [['list']],
  // Desde F1 toda ruta exige sesión: entra una vez con un usuario de R1 (necesita la API arriba).
  globalSetup: resolve(__dirname, '../soporte/sesion-global.ts'),
  use: {
    storageState: SESION_GUARDADA,
    browserName: 'chromium',
    viewport: { width: 1600, height: 900 },
    reducedMotion: 'reduce',
    deviceScaleFactor: 1,
    locale: 'es-MX',
    timezoneId: 'America/Monterrey',
  },
  webServer: {
    command: 'npx ng serve --port 9000',
    url: 'http://localhost:9000',
    cwd: '../..',
    reuseExistingServer: true,
    timeout: 180_000,
  },
});
