import { Page, test } from '@playwright/test';
import { writeFileSync, mkdirSync } from 'node:fs';
import { join } from 'node:path';
import { abrir, ANGULAR, BLAZOR } from '../soporte/apps';
import { rutasSeleccionadas } from '../parity/rutas';

/**
 * Auditoría de comportamiento de primer nivel: en cada ruta, para cada botón o enlace visible,
 * carga limpia en las dos aplicaciones, lo pulsa y compara URL y texto de la página.
 * Escribe auditoria-report/<ruta>.json con las diferencias.
 */
const CLICKABLES = 'main button:visible, main a:visible, main [role=button]:visible, main tr[style*="cursor"]:visible, main .o_kanban_record:visible, .o_header_menu a:visible';

const norm = (t: string) => t.replace(/\s+/g, ' ').trim();

async function estado(page: Page): Promise<{ url: string; texto: string }> {
  const url = new URL(page.url());
  return { url: decodeURIComponent(url.pathname), texto: norm(await page.locator('body').innerText()) };
}

async function pulsar(page: Page, base: string, ruta: string, i: number): Promise<{ etiqueta: string; url: string; texto: string } | null> {
  await abrir(page, base, ruta);
  const todos = page.locator(CLICKABLES);
  const n = await todos.count();
  if (i >= n) return null;
  const el = todos.nth(i);
  const etiqueta = norm((await el.innerText().catch(() => '')) || (await el.getAttribute('title')) || (await el.evaluate(e => `[${e.tagName.toLowerCase()} ${e.querySelector('i')?.className ?? e.className}]`)));
  // "Nuevo" es la única diferencia permitida con el prototipo (D-59): se compara su etiqueta, no se pulsa.
  if (etiqueta === 'Nuevo') return { etiqueta, url: '', texto: '' };
  const deshabilitado = await el.isDisabled().catch(() => false);
  if (!deshabilitado) {
    await el.click({ timeout: 3000 }).catch(() => undefined);
    await page.waitForTimeout(400);
  }
  return { etiqueta: (deshabilitado ? '[deshabilitado] ' : '') + etiqueta, ...(await estado(page)) };
}

function difTexto(a: string, b: string): string {
  let i = 0;
  while (i < a.length && a[i] === b[i]) i++;
  return `@${i}\n  blazor : …${a.slice(Math.max(0, i - 60), i + 140)}\n  angular: …${b.slice(Math.max(0, i - 60), i + 140)}`;
}

for (const ruta of rutasSeleccionadas()) {
  test(`auditoría ${ruta}`, async ({ browser }) => {
    test.setTimeout(600_000);
    const pb = await browser.newPage();
    const pa = await browser.newPage();
    await abrir(pb, BLAZOR, ruta);
    await abrir(pa, ANGULAR, ruta);
    const nb = await pb.locator(CLICKABLES).count();
    const na = await pa.locator(CLICKABLES).count();
    const difs: string[] = [];
    if (nb !== na) difs.push(`número de elementos pulsables: blazor ${nb}, angular ${na}`);
    for (let i = 0; i < Math.min(nb, na); i++) {
      const b = await pulsar(pb, BLAZOR, ruta, i);
      const a = await pulsar(pa, ANGULAR, ruta, i);
      if (!a || !b) continue;
      if (a.etiqueta !== b.etiqueta) difs.push(`#${i} etiqueta: blazor "${b.etiqueta}" / angular "${a.etiqueta}"`);
      if (a.url !== b.url) difs.push(`#${i} "${b.etiqueta}" url: blazor ${b.url} / angular ${a.url}`);
      else if (a.texto !== b.texto) difs.push(`#${i} "${b.etiqueta}" texto ${difTexto(b.texto, a.texto)}`);
    }
    const dir = join(__dirname, '..', '..', 'auditoria-report');
    mkdirSync(dir, { recursive: true });
    writeFileSync(join(dir, (ruta === '/' ? 'inicio' : ruta.slice(1).replace(/\//g, '_')) + '.txt'), `${ruta}: ${Math.min(nb, na)} pulsables\n` + difs.join('\n') + '\n');
    await pb.close();
    await pa.close();
  });
}
