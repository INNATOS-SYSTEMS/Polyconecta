import { readFileSync } from 'node:fs';
import { expect, Locator, Page, test } from '@playwright/test';
import { ANGULAR, abrir } from '../soporte/apps';

/** Comportamientos de los componentes de la spec 011 en la galería (contracts/componentes.md). */
const lista = (page: Page) => page.locator('[data-catalogo="lista"]');
const filas = (page: Page) => lista(page).locator('table[data-lista="catalogo.pedidos"] tbody tr:not(.o_group_row)');
const rango = (page: Page) => lista(page).locator('.o_pager').first();
const tabla = (page: Page) => lista(page).locator('table[data-lista="catalogo.pedidos"]');
const totales = async (page: Page) => (await filas(page).locator('td:nth-child(5)').allInnerTexts()).map(t => Number(t.replace(/[$,]/g, '')));

async function arrastrar(page: Page, origen: Locator, destino: Locator): Promise<void> {
  await origen.scrollIntoViewIfNeeded();
  const a = (await origen.boundingBox())!;
  const b = (await destino.boundingBox())!;
  await page.mouse.move(a.x + 20, a.y + 10);
  await page.mouse.down();
  await page.mouse.move(a.x + 40, a.y + 30, { steps: 5 });
  await page.mouse.move(b.x + b.width / 2, b.y + 60, { steps: 15 });
  await page.mouse.up();
}

test.beforeEach(async ({ page }) => {
  // Limpia los favoritos solo al abrir la primera vez: una recarga dentro de la prueba los conserva.
  await page.addInitScript(() => {
    if (!sessionStorage.getItem('catalogo-limpio')) {
      localStorage.clear();
      sessionStorage.setItem('catalogo-limpio', '1');
    }
  });
  await abrir(page, ANGULAR, '/catalogo');
  await expect(filas(page)).toHaveCount(20);
});

test.describe('lista', () => {
  test('pagina y cambia las filas por página', async ({ page }) => {
    await expect(rango(page)).toContainText('1-20 / 57');
    await rango(page).locator('[data-pager="siguiente"]').click();
    await expect(rango(page)).toContainText('21-40 / 57');
    await expect(filas(page).first()).toContainText('PV-2026-0021');
    await rango(page).locator('[data-pager="rango"]').click();
    await rango(page).locator('[data-tamano="40"]').click();
    await expect(filas(page)).toHaveCount(40);
    await expect(rango(page)).toContainText('1-40 / 57');
  });

  test('ordena las 57 en el origen, de menor a mayor en el primer clic', async ({ page }) => {
    await tabla(page).locator('th[data-columna="total"]').click();
    await expect(tabla(page).locator('th[data-columna="total"]')).toHaveAttribute('aria-sort', 'ascending');
    const asc = await totales(page);
    expect(asc).toEqual([...asc].sort((a, b) => a - b));
    expect(asc[0]).toBe(1000);
    await tabla(page).locator('th[data-columna="total"]').click();
    await expect(tabla(page).locator('th[data-columna="total"]')).toHaveAttribute('aria-sort', 'descending');
    expect((await totales(page))[0]).toBe(50127);
  });

  test('filtra con la búsqueda y con los filtros con nombre', async ({ page }) => {
    await lista(page).locator('.o_search_bar input').fill('norte');
    await expect(rango(page)).toContainText('/ 12');
    await lista(page).locator('.o_search_bar input').fill('');
    await lista(page).locator('button[title="Filtros del modelo"]').click();
    await lista(page).locator('.o_search_menu_item', { hasText: 'Borrador' }).click();
    await expect(rango(page)).toContainText('/ 12');
    for (const e of await filas(page).locator('td:nth-child(6)').allInnerTexts()) expect(e).toBe('Borrador');
  });

  test('agrupa en el origen, con conteo y total, y abre un grupo bajo pedido', async ({ page }) => {
    await lista(page).locator('button[title="Filtros del modelo"]').click();
    await lista(page).locator('.o_search_menu_item', { hasText: 'Cliente' }).last().click();
    // Se cierra el menú: con la búsqueda al centro del panel, queda encima de la tabla.
    await lista(page).locator('button[title="Filtros del modelo"]').click();
    const grupos = lista(page).locator('table[data-lista="catalogo.pedidos"] .o_group_row');
    await expect(grupos).toHaveCount(5);
    await expect(filas(page)).toHaveCount(0);
    const norte = grupos.filter({ hasText: 'BOLSAS DEL NORTE' });
    await expect(norte).toContainText('(12)');
    await norte.click();
    await expect(filas(page)).toHaveCount(12);
    await norte.click();
    await expect(filas(page)).toHaveCount(0);
  });

  test('oculta y reordena columnas', async ({ page }) => {
    await lista(page).locator('[data-lista="columnas"]').first().click();
    // El menú de columnas va en un popover sobre la página, fuera del contenedor de la lista.
    await page.locator('[data-columna-visible="fecha"]').first().uncheck();
    await expect(lista(page).locator('table[data-lista="catalogo.pedidos"] th[data-columna="fecha"]')).toHaveCount(0);
    await page.locator('[data-columna-subir="total"]').first().click();
    await expect.poll(() => tabla(page).locator('thead th[data-columna]').evaluateAll(ths => ths.map(t => t.getAttribute('data-columna'))))
      .toEqual(['folio', 'total', 'cliente', 'estado']);
  });

  test('selecciona y exporta las seleccionadas a .xlsx desde "Acciones", junto a la búsqueda', async ({ page }) => {
    await filas(page).nth(0).locator('input').click();
    await filas(page).nth(2).locator('input').click();
    // La selección va en el panel de control, no encima de la tabla (07 §1.1, D-167).
    await expect(lista(page).locator('.o_control_panel [data-lista="seleccion"]')).toContainText('2 seleccionados');
    await expect(lista(page).locator('pc-odoo-list [data-lista="seleccion"]')).toHaveCount(0);
    await lista(page).locator('[data-lista="acciones"]').click();
    const [descarga] = await Promise.all([page.waitForEvent('download'), page.locator('[data-lista="exportar"]').click()]);
    expect(descarga.suggestedFilename()).toMatch(/^catalogo\.pedidos-\d{4}-\d{2}-\d{2}\.xlsx$/);
    const bytes = readFileSync((await descarga.path())!);
    expect([bytes[0], bytes[1]]).toEqual([0x50, 0x4b]); // un .xlsx es un zip
  });

  test('"Eliminar" va al final, se deshabilita con su razón y pide confirmación', async ({ page }) => {
    const borrador = filas(page).filter({ hasText: 'Borrador' }).first();
    const folio = (await borrador.locator('td').nth(1).textContent())!.trim();
    await borrador.locator('input').click();
    await lista(page).locator('[data-lista="acciones"]').click();
    const eliminar = page.locator('[data-accion-masiva="Eliminar"]');
    await expect(eliminar).toHaveClass(/text-danger/);
    await eliminar.click();
    await page.locator('[data-dialogo="cancelar"]').click();
    await expect(tabla(page)).toContainText(folio);

    await lista(page).locator('[data-lista="acciones"]').click();
    await page.locator('[data-accion-masiva="Eliminar"]').click();
    await page.locator('[data-dialogo="confirmar"]').click();
    await expect(tabla(page)).not.toContainText(folio);
    await expect(lista(page).locator('[data-lista="seleccion"]')).toHaveCount(0);

    // Un pedido fuera de Borrador: la acción no aplica y dice por qué.
    await filas(page).filter({ hasText: 'Autorizado' }).first().locator('input').click();
    await lista(page).locator('[data-lista="acciones"]').click();
    await expect(page.locator('[data-accion-masiva="Eliminar"]')).toHaveAttribute('aria-disabled', 'true');
    await expect(page.locator('[data-accion-masiva="Eliminar"]')).toHaveAttribute('title', 'Solo se eliminan pedidos en Borrador.');
  });

  test('muestra el total del filtro y el mensaje de lista vacía', async ({ page }) => {
    await expect(lista(page).locator('table[data-lista="catalogo.pedidos"] tfoot')).toContainText('$1,388,284.00');
    await expect(page.locator('table[data-lista="catalogo.vacia"]')).toContainText('No hay registros que mostrar.');
  });

  test('guarda un favorito y lo aplica por omisión al volver', async ({ page }) => {
    await lista(page).locator('button[title="Filtros del modelo"]').click();
    await lista(page).locator('.o_search_menu_item', { hasText: 'Estado' }).last().click();
    await lista(page).locator('[data-favorito-nombre]').fill('Por estado');
    await lista(page).locator('.o_search_menu label', { hasText: 'Usar por omisión' }).locator('input').check();
    await lista(page).locator('[data-favorito-guardar]').click();
    await expect(page.locator('[data-aviso="exito"]')).toContainText('Favorito "Por estado" guardado.');
    await page.evaluate(() => location.reload());
    await expect(lista(page).locator('table[data-lista="catalogo.pedidos"] .o_group_row')).toHaveCount(5);
    await expect(lista(page).locator('.o_search_facet')).toContainText('Estado');
  });
});

test.describe('kanban', () => {
  const kanban = (page: Page) => page.locator('[data-catalogo="kanban"]');
  const columna = (page: Page, etapa: string) => kanban(page).locator(`[data-etapa="${etapa}"]`);

  test('una transición válida mueve la tarjeta; una sin transición la regresa con el motivo', async ({ page }) => {
    await expect(columna(page, 'Hecho')).toHaveClass(/o_kanban_plegada/);
    await arrastrar(page, columna(page, 'Borrador').locator('[data-tarjeta="p1"]'), columna(page, 'Confirmado'));
    await expect(columna(page, 'Confirmado').locator('[data-tarjeta="p1"]')).toHaveCount(1);
    await arrastrar(page, columna(page, 'Borrador').locator('[data-tarjeta="p6"]'), columna(page, 'En progreso'));
    await expect(columna(page, 'Borrador').locator('[data-motivo]')).toHaveText('No se puede pasar de Borrador a En progreso');
    await expect(columna(page, 'Borrador').locator('[data-tarjeta="p6"]')).toHaveCount(1);
  });

  test('una transición que pide firma abre su diálogo; cancelar regresa la tarjeta', async ({ page }) => {
    await arrastrar(page, columna(page, 'Confirmado').locator('[data-tarjeta="p4"]'), columna(page, 'Autorizado'));
    await expect(page.locator('.o_dialog')).toContainText('Autorizar pedido');
    await page.locator('.o_dialog [data-dialogo="cancelar"]').click();
    await expect(columna(page, 'Confirmado').locator('[data-tarjeta="p4"]')).toHaveCount(1);
    await arrastrar(page, columna(page, 'Confirmado').locator('[data-tarjeta="p4"]'), columna(page, 'Autorizado'));
    await page.locator('.o_dialog [data-firmante]').fill('Comercial');
    await page.locator('.o_dialog [data-dialogo="confirmar"]').click();
    await expect(columna(page, 'Autorizado').locator('[data-tarjeta="p4"]')).toHaveCount(1);
  });
});

test.describe('campos', () => {
  const campos = (page: Page) => page.locator('[data-catalogo="campos"]');

  test('selección de registro: busca, elige con teclado, avisa sin resultados y "Buscar más…"', async ({ page }) => {
    const entrada = campos(page).locator('[data-many2one="Producto"] input');
    await entrada.click();
    await entrada.pressSequentially('resina');
    // Cada letra pide opciones nuevas; se espera a que todas sean del texto completo antes de usar el teclado.
    await expect.poll(() => page.locator('[data-opcion]').allInnerTexts()).toEqual(expect.arrayContaining([expect.stringContaining('RESINA')]));
    await expect.poll(async () => (await page.locator('[data-opcion]').allInnerTexts()).every(t => t.includes('RESINA'))).toBe(true);
    await expect(page.locator('[data-opcion]')).toHaveCount(8);
    await expect(page.locator('[data-many2one-mas]')).toBeVisible();
    // El foco sigue en el campo aunque aparezca "Buscar más…" (popover sin autoFocus).
    await expect(entrada).toBeFocused();
    await page.keyboard.press('ArrowDown');
    await expect(page.locator('[data-opcion][data-highlighted]')).toHaveCount(1);
    await page.keyboard.press('Enter');
    await expect(campos(page).locator('[data-catalogo="producto"]')).toHaveText(/^MP\d{4}$/);
    await entrada.click();
    await entrada.fill('');
    await entrada.pressSequentially('zzz');
    await expect(page.locator('.o_many2one_vacio')).toBeVisible();
    await entrada.fill('');
    await entrada.pressSequentially('pigmento');
    await expect(page.locator('.o_many2one_vacio')).toBeHidden();
    await expect(page.locator('[data-opcion]')).toHaveCount(2);
  });

  test('"Buscar más…" abre la lista completa y elegir una fila la devuelve', async ({ page }) => {
    const entrada = campos(page).locator('[data-many2one="Producto"] input');
    await entrada.click();
    await page.locator('[data-many2one-mas]').click();
    await page.locator('.o_dialog [data-buscar-mas]').fill('tinta roja');
    await page.locator('.o_dialog tbody tr', { hasText: 'TINTA ROJA' }).click();
    await expect(campos(page).locator('[data-catalogo="producto"]')).toHaveText('TN0002');
  });

  test('fecha: calendario en español, dentro de su rango', async ({ page }) => {
    await campos(page).locator('[data-fecha="Fecha de entrega"] button').first().click();
    await expect(page.locator('[data-fecha-mes]')).toHaveText('octubre 2026');
    await expect(page.locator('.o_calendar th').first()).toHaveText('lu');
    await expect(page.getByRole('button', { name: 'Mes siguiente' })).toBeVisible();
    await page.locator('[data-dia="2026-10-15"]').click();
    await expect(campos(page).locator('[data-catalogo="fecha"]')).toHaveText('2026-10-15');
    await expect(campos(page).locator('[data-fecha="Fecha de entrega"] [data-fecha-texto]')).toHaveText('15 oct 2026');
  });

  test('número: formato es-MX, edición sin separadores y mínimo', async ({ page }) => {
    const cantidad = campos(page).locator('[data-numero="Cantidad"] input');
    await expect(cantidad).toHaveValue('5,500.0');
    await cantidad.click();
    await expect(cantidad).toHaveValue('5500');
    await cantidad.fill('1234.56');
    await cantidad.blur();
    await expect(cantidad).toHaveValue('1,234.6');
    await expect(campos(page).locator('[data-catalogo="cantidad"]')).toHaveText('1234.56');
    await cantidad.click();
    await cantidad.fill('-3');
    await cantidad.blur();
    await expect(campos(page).locator('[data-numero="Cantidad"] + [role="alert"], [role="alert"]').filter({ hasText: 'El valor mínimo' })).toBeVisible();
    await expect(campos(page).locator('[data-numero="Cantidad fija"] input')).toHaveAttribute('readonly', '');
    await expect(campos(page).locator('[data-numero-unidad]').first()).toHaveText('PZA');
  });
});

test.describe('formulario, avisos y sincronización', () => {
  test('pestañas y menú de acciones', async ({ page }) => {
    const f = page.locator('[data-catalogo="formulario"]');
    await expect(f.locator('[data-catalogo="lineas"]')).toBeVisible();
    await f.locator('[data-pestana="otra"]').click();
    await expect(f.locator('[data-catalogo="pestana-otra"]')).toBeVisible();
    await expect(f.locator('[data-catalogo="lineas"]')).toHaveCount(0);
    await f.locator('[data-acciones]').click();
    await expect(page.locator('[data-accion="Cancelar documento"]')).toBeDisabled();
    await page.locator('[data-accion="Imprimir"]').click();
    await expect(page.locator('[data-aviso="exito"]')).toContainText('Imprimir');
  });

  test('confirmación: Esc cancela y Confirmar confirma; hard-stop', async ({ page }) => {
    const a = page.locator('[data-catalogo="avisos"]');
    await a.locator('[data-catalogo="abrir-confirmacion"]').click();
    await expect(page.locator('.o_dialog')).toContainText('¿Confirmar el pedido PV-2026-0001?');
    await page.keyboard.press('Escape');
    await expect(a.locator('[data-catalogo="respuesta"]')).toHaveText('Cancelado');
    await a.locator('[data-catalogo="abrir-confirmacion"]').click();
    await page.locator('.o_dialog [data-dialogo="confirmar"]').click();
    await expect(a.locator('[data-catalogo="respuesta"]')).toHaveText('Confirmado');
    await a.locator('[data-catalogo="abrir-hard-stop"]').click();
    await expect(page.locator('.o_dialog')).toContainText('hard-stop');
  });

  test('avisos: éxito se cierra solo; error se queda hasta cerrarlo', async ({ page }) => {
    const a = page.locator('[data-catalogo="avisos"]');
    await a.locator('[data-catalogo="aviso-exito"]').click();
    await a.locator('[data-catalogo="aviso-error"]').click();
    await expect(page.locator('[data-aviso="exito"]')).toBeVisible();
    await expect(page.locator('[data-aviso="exito"]')).toHaveCount(0, { timeout: 6000 });
    await expect(page.locator('[data-aviso="error"]')).toBeVisible();
    await page.locator('[data-aviso="error"] button').click();
    await expect(page.locator('[data-aviso="error"]')).toHaveCount(0);
  });

  test('estado de sincronización: cinco íconos con popover y reintento solo en error', async ({ page }) => {
    const s = page.locator('[data-catalogo="sincronizacion"]');
    for (const e of ['NoAplica', 'Pendiente', 'Enviado', 'Confirmado', 'Error']) await expect(s.locator(`[data-sync="${e}"]`)).toHaveCount(1);
    await expect(s.locator('[data-sync="Confirmado"]')).toHaveText(''); // solo ícono
    await s.locator('[data-sync="Confirmado"]').click();
    await expect(page.locator('[data-sync-popover]')).toContainText('Registrado en CONTPAQi');
    await expect(page.locator('[data-sync-folio]')).toContainText('F-26200');
    await page.keyboard.press('Escape');
    await s.locator('[data-sync="Error"]').click();
    await expect(page.locator('[data-sync-error]')).toContainText('EXISTENCIA_INSUFICIENTE');
    await page.locator('[data-sync-reintentar]').click();
    await expect(page.locator('[data-aviso="aviso"]')).toContainText('Reintento encolado.');
  });

  test('formulario: engranaje junto a las migas y sincronización en la barra de acciones', async ({ page }) => {
    const f = page.locator('[data-catalogo="formulario"]');
    await expect(f.locator('.o_control_panel [data-acciones]')).toHaveText('');
    await expect(f.locator('.o_statusbar [data-acciones]')).toHaveCount(0);
    await expect(f.locator('.o_statusbar [data-sync]')).toHaveCount(1);
    await expect(f.locator('.o_control_panel')).toHaveCSS('border-bottom-color', 'rgba(0, 0, 0, 0)');
  });

  test('kanban: etapas de 338 px', async ({ page }) => {
    const anchos = await page.locator('[data-catalogo="kanban"] .o_kanban_column:not(.o_kanban_plegada)').evaluateAll(cs => cs.map(c => c.getBoundingClientRect().width));
    expect(new Set(anchos)).toEqual(new Set([338]));
  });

  test('dual-list: dos paneles en árbol, filtrado y transferencia de permisos', async ({ page }) => {
    const d = page.locator('[data-catalogo="dual-list"]');
    await expect(d).toBeVisible();
    await expect(d.locator('[data-conteo-disponibles]')).toContainText('8');
    await expect(d.locator('[data-conteo-asignados]')).toContainText('2');

    // Filtrar en panel izquierdo
    await d.locator('[data-buscar-disponibles]').fill('clasificar');
    await expect(d.locator('[data-dual-panel="disponibles"]')).toContainText('Clasificar');

    // Limpiar filtro
    await d.locator('[data-buscar-disponibles]').fill('');

    // Marcar permiso en disponibles y transferir con >
    await d.locator('[data-check-permiso="ventas.pedido.editar"]').check();
    await d.locator('[data-btn-asignar]').click();

    await expect(d.locator('[data-conteo-disponibles]')).toContainText('7');
    await expect(d.locator('[data-conteo-asignados]')).toContainText('3');
    await expect(d.locator('[data-dual-panel="asignados"]')).toContainText('Editar');
  });
});
