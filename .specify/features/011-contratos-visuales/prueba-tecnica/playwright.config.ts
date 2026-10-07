import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: '.',
  timeout: 60_000,
  workers: 1,
  reporter: [['list']],
  use: { browserName: 'chromium', viewport: { width: 1600, height: 1400 }, locale: 'es-MX', timezoneId: 'America/Monterrey' },
  webServer: { command: 'npx ng serve --port 9100', url: 'http://localhost:9100', cwd: '../..', reuseExistingServer: true, timeout: 180_000 },
});
