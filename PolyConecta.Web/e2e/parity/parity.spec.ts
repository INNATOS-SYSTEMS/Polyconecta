import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { expect, test } from '@playwright/test';
import pixelmatch from 'pixelmatch';
import { PNG } from 'pngjs';
import { ANGULAR, BLAZOR, abrir } from '../soporte/apps';
import { rutasSeleccionadas } from './rutas';

export const DIRECTORIO = join(__dirname, '..', '..', 'parity-report');
const UMBRAL = 0.01;

for (const ruta of rutasSeleccionadas()) {
  test(`paridad ${ruta}`, async ({ page }) => {
    const nombre = ruta === '/' ? 'inicio' : ruta.slice(1).replace(/\//g, '_');
    const capturar = async (base: string) => {
      await abrir(page, base, ruta);
      // "Nuevo" es la única diferencia permitida (D-59): se enmascara en los dos lados.
      return PNG.sync.read(await page.screenshot({ mask: [page.getByRole('button', { name: 'Nuevo' })], maskColor: '#ff00ff' }));
    };
    const blazor = await capturar(BLAZOR);
    const angular = await capturar(ANGULAR);

    const { width, height } = blazor;
    const diferencia = new PNG({ width, height });
    const distintos = pixelmatch(blazor.data, angular.data, diferencia.data, width, height, { threshold: 0.1 });
    const proporcion = distintos / (width * height);

    mkdirSync(DIRECTORIO, { recursive: true });
    writeFileSync(join(DIRECTORIO, `${nombre}.blazor.png`), PNG.sync.write(blazor));
    writeFileSync(join(DIRECTORIO, `${nombre}.angular.png`), PNG.sync.write(angular));
    writeFileSync(join(DIRECTORIO, `${nombre}.diff.png`), PNG.sync.write(diferencia));
    writeFileSync(join(DIRECTORIO, `${nombre}.json`), JSON.stringify({ ruta, nombre, proporcion, distintos, pasa: proporcion <= UMBRAL }));

    expect(proporcion, `${ruta}: ${(proporcion * 100).toFixed(2)} % de píxeles distintos (máximo ${UMBRAL * 100} %)`).toBeLessThanOrEqual(UMBRAL);
  });
}
