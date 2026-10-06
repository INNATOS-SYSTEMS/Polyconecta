import { defineConfig } from '@playwright/test';

/**
 * Paridad con el prototipo Blazor (spec 001, FR-017, FR-018, research R-08): Chromium a 1600×900,
 * sin animaciones, contra Blazor en :9000 y Angular en :4200. Reutiliza los servidores si ya responden.
 */
export default defineConfig({
  testDir: '.',
  testMatch: ['**/*.spec.ts', '**/*.scenario.ts'],
  timeout: 60_000,
  fullyParallel: false,
  workers: 1,
  reporter: [['list']],
  globalTeardown: './parity/informe.ts',
  use: {
    browserName: 'chromium',
    viewport: { width: 1600, height: 900 },
    reducedMotion: 'reduce',
    deviceScaleFactor: 1,
    locale: 'es-MX',
    timezoneId: 'America/Monterrey',
  },
  webServer: [
    {
      command: 'dotnet run --project ../PolyConecta.Presentation --urls http://localhost:9000',
      url: 'http://localhost:9000',
      reuseExistingServer: true,
      timeout: 180_000,
    },
    {
      command: 'npx ng serve --port 4200',
      url: 'http://localhost:4200',
      reuseExistingServer: true,
      timeout: 180_000,
    },
  ],
});
