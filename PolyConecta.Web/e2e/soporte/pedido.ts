import { expect, Page } from '@playwright/test';

/**
 * Acciones sobre la hoja del pedido (spec 011 conectada a la API, D-157): campos many2one, captura de
 * líneas arriba de la tabla, pestañas, engranaje y avisos. Las pruebas de Pedidos las usan en lugar de
 * ids sueltos.
 */
export const etapa = (page: Page) => page.locator('.o_statusbar_pipeline .arrow-step.active');
export const lineas = (page: Page) => page.locator('[data-lineas-pedido] tbody tr');
export const aviso = (page: Page, tipo: 'exito' | 'aviso' | 'error' = 'exito') => page.locator(`[data-aviso="${tipo}"]`).last();
export const botonBarra = (page: Page, texto: string | RegExp) => page.locator('.o_statusbar button', { hasText: texto });

/** Elige en un many2one del maestro (07 §1.6): escribe y toma la opción `indice` de lo que aparece. */
export async function elegir(page: Page, campo: string, texto = '', indice = 0): Promise<void> {
  const input = page.locator(`[data-many2one="${campo}"] input`);
  await input.click();
  await input.fill(texto);
  const opcion = page.locator('.o_many2one_opcion').nth(indice);
  await expect(opcion).toBeVisible();
  await opcion.click();
}

/** Clave del producto `indice` del catálogo de la captura. */
export async function claveProducto(page: Page, indice: number): Promise<string> {
  return (await page.locator('.o_line_capture ~ datalist option, datalist option').nth(indice).getAttribute('value')) ?? '';
}

/** Captura una línea: [Producto] [Cantidad] [Unidad] [Precio unitario] [Agregar] (D-74, D-161). */
export async function capturarLinea(page: Page, clave: string, cantidad: string, precio: string): Promise<void> {
  const captura = page.locator('.o_line_capture');
  const campos = captura.locator('input');
  await campos.nth(0).fill(clave);
  await campos.nth(0).press('Tab');
  await expect(campos.nth(2), 'la unidad es la base del producto y no se edita (D-127)').toHaveAttribute('readonly', '');
  await campos.nth(1).fill(cantidad);
  await campos.nth(1).press('Tab');
  await campos.nth(3).fill(precio);
  await campos.nth(3).press('Tab');
  await captura.getByRole('button', { name: /Agregar|Guardar/ }).click();
}

/** Catálogos de la hoja (clientes, agentes, productos) para las pruebas con la API simulada. */
export async function simularCatalogosPedido(page: Page, catalogos: { clientes?: unknown[]; agentes?: unknown[]; productos?: unknown[] } = {}): Promise<void> {
  const json = (body: unknown) => ({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
  await page.route(/\/api\/v1\/ventas\/clientes\/buscar/, r => r.fulfill(json(catalogos.clientes ?? [])));
  await page.route(/\/api\/v1\/ventas\/agentes/, r => r.fulfill(json(catalogos.agentes ?? [])));
  await page.route(/\/api\/v1\/inventario\/productos\/buscar/, r => r.fulfill(json(catalogos.productos ?? [])));
}

/** Edita la línea `indice` desde la captura (botón de ícono de la fila) y la vuelve a guardar en la tabla. */
export async function editarLinea(page: Page, indice: number, cambios: { cantidad?: string; precio?: string }): Promise<void> {
  await lineas(page).nth(indice).locator('button[title="Editar"]').click();
  const campos = page.locator('.o_line_capture input');
  // La línea sube a la captura antes de cambiarla (07 §1.5).
  await expect(campos.nth(0)).not.toHaveValue('');
  if (cambios.cantidad !== undefined) { await campos.nth(1).fill(cambios.cantidad); await campos.nth(1).press('Tab'); }
  if (cambios.precio !== undefined) { await campos.nth(3).fill(cambios.precio); await campos.nth(3).press('Tab'); }
  await page.locator('.o_line_capture').getByRole('button', { name: 'Guardar' }).click();
}

export async function pestana(page: Page, id: string): Promise<void> {
  await page.click(`[data-pestana="${id}"]`);
}

/** Abre el engranaje del documento y devuelve la acción (07 §1.3). */
export async function accionEngranaje(page: Page, nombre: string) {
  await page.click('[data-acciones]');
  return page.locator(`[data-accion="${nombre}"]`);
}

/** Revocar o cancelar desde el engranaje: el diálogo exige el motivo (FR-025, FR-026). */
export async function conMotivo(page: Page, accion: string, motivo: string): Promise<void> {
  await (await accionEngranaje(page, accion)).click();
  const dialogo = page.locator('#dialogo-motivo');
  await expect(dialogo.locator('.btn-primary').first()).toBeDisabled();
  await dialogo.locator('#campo-motivo').fill(motivo);
  await dialogo.locator('.btn-primary').first().click();
}
