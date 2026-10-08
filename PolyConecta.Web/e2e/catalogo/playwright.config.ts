import { defineConfig } from '@playwright/test';

/** Galería de componentes (spec 011): solo Angular en :9000, a 1600×900. */
export default defineConfig({
  testDir: '.',
  testMatch: ["**/*.spec.ts"],
  timeout: 60_000,
  fullyParallel: false,
  workers: 1,
  reporter: [['list']],
  use: {
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
