import { defineConfig } from '@playwright/test';
import base from '../playwright.config';

/**
 * Prueba del chatter en vivo (L2-T066, SC-005): además de las dos aplicaciones levanta
 * PolyConecta.Api, que hospeda el hub. El hub no usa la base: la cadena de conexión solo satisface
 * la validación de arranque y no apunta a ningún servidor.
 */
export default defineConfig({
  ...base,
  testDir: '.',
  testMatch: ['chatter.spec.ts'],
  globalTeardown: undefined,
  webServer: [
    ...(Array.isArray(base.webServer) ? base.webServer : []).map(w => ({ ...w, cwd: '../..' })),
    {
      command: 'dotnet run --project ../PolyConecta.Api --no-launch-profile --urls http://localhost:9020',
      url: 'http://localhost:9020/swagger/index.html',
      cwd: '../..',
      reuseExistingServer: true,
      timeout: 180_000,
      env: { ConnectionStrings__PolyConecta: 'Server=localhost,1;Database=sin-base;User Id=chatter;Password=chatter;TrustServerCertificate=true' },
    },
  ],
});
