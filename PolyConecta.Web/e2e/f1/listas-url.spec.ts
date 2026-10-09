import { expect, Page, test } from '@playwright/test';
import { ANGULAR, abrir } from '../soporte/apps';

/**
 * Estado de la lista en la URL y favoritos por usuario (spec 003, US4 escenarios 1 y 2; L2-T029).
 * La vista de búsqueda la declara el servidor (`GET …/vista`) y el conjunto cabe en el umbral (D-151).
 */
const sesion = {
  usuario: { id: 10, usuario: 'ac1', nombre: 'Atención Clientes 1' },
  asignaciones: [{ grupo: 'ATENCION_CLIENTES', nombreGrupo: 'Atención a Clientes', planta: 'PIM', suplente: false }],
  permisos: ['ventas.pedido.leer', 'ventas.pedido.crear'],
};

const vista = {
  lista: 'ventas.pedidos',
  campos: [{ campo: 'folio', etiqueta: 'Folio' }, { campo: 'cliente', etiqueta: 'Cliente' }],
  filtros: [
    { nombre: 'Borrador', campo: 'Estado' },
    { nombre: 'Confirmado', campo: 'Estado' },
    { nombre: 'Mis pedidos', campo: 'Responsable' },
  ],
  agrupaciones: [{ etiqueta: 'Estado', campo: 'estado' }, { etiqueta: 'Cliente', campo: 'cliente' }],
};

const pedido = (id: number, estado: string, cliente: string, propio: boolean) => ({
  id, folio: `PV-2026-000${id}`, cliente, estado, fechaPromesa: null,
  _filtros: [estado, ...(propio ? ['Mis pedidos'] : [])],
});

const filas = [
  pedido(1, 'Borrador', 'ACME', true),
  pedido(2, 'Confirmado', 'ACME', true),
  pedido(3, 'Confirmado', 'Norte', false),
  pedido(4, 'Autorizado', 'Norte', true),
];

const json = (body: unknown) => ({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });

async function simularApi(page: Page, favoritos: unknown[] = []): Promise<{ guardados: unknown[] }> {
  const guardados: unknown[] = [];
  await page.route(/\/api\/v1\/plataforma\/sesion$/, route => route.fulfill(json(sesion)));
  await page.route(/\/api\/v1\/ventas\/pedidos\/vista$/, route => route.fulfill(json(vista)));
  await page.route(/\/api\/v1\/ventas\/pedidos\/conjunto$/, route => route.fulfill(json({ completo: true, total: filas.length, filas })));
  await page.route(/\/api\/v1\/plataforma\/favoritos\/ventas\.pedidos(\/.*)?$/, async route => {
    if (route.request().method() === 'PUT') {
      guardados.push(JSON.parse(route.request().postData() || '{}'));
      await route.fulfill(json({}));
      return;
    }
    await route.fulfill(json(favoritos));
  });
  return { guardados };
}

const menu = (page: Page) => page.getByTitle('Filtros del modelo');
const opcion = (page: Page, texto: string) => page.locator('.o_search_menu_item', { hasText: texto });

test.describe('Listas: estado en la URL y favoritos (F1 / US4)', () => {
  test('filtrar y agrupar queda en la URL; al recargar se obtiene la misma vista', async ({ page }) => {
    await simularApi(page);
    await abrir(page, ANGULAR, '/ventas/pedidos');
    await expect(page.locator('pc-odoo-list')).toContainText('PV-2026-0004');

    await menu(page).click();
    await opcion(page, 'Confirmado').click();
    await opcion(page, 'Cliente').click();

    await expect(page).toHaveURL(/filtro=Confirmado/);
    await expect(page).toHaveURL(/agrupar=Cliente/);
    await expect(page.locator('pc-odoo-list')).not.toContainText('PV-2026-0004');

    await page.reload({ waitUntil: 'networkidle' });

    await expect(page.locator('.o_search_facet', { hasText: 'Confirmado' })).toBeVisible();
    await expect(page.locator('.o_search_facet', { hasText: 'Cliente' })).toBeVisible();
    await expect(page).toHaveURL(/filtro=Confirmado/);
    await expect(page.locator('pc-odoo-list')).toContainText('ACME');
    await expect(page.locator('pc-odoo-list')).toContainText('Norte');
    await expect(page.locator('pc-odoo-list')).not.toContainText('PV-2026-0001');
  });

  test('un enlace compartido abre la vista con su filtro, aunque haya un favorito por omisión', async ({ page }) => {
    await simularApi(page, [{ nombre: 'Borradores', porOmision: true, definicion: { nombrados: ['Borrador'] } }]);

    await abrir(page, ANGULAR, '/ventas/pedidos?filtro=Mis%20pedidos');

    await expect(page.locator('.o_search_facet', { hasText: 'Mis pedidos' })).toBeVisible();
    await expect(page.locator('pc-odoo-list')).toContainText('PV-2026-0004');
    await expect(page.locator('pc-odoo-list')).not.toContainText('PV-2026-0003');
  });

  test('el favorito por omisión se aplica al abrir, con su filtro con nombre, y se ve en la URL', async ({ page }) => {
    await simularApi(page, [{ nombre: 'Por autorizar', porOmision: true, definicion: { nombrados: ['Confirmado'], agruparPor: [] } }]);

    await abrir(page, ANGULAR, '/ventas/pedidos');

    await expect(page.locator('.o_search_facet', { hasText: 'Confirmado' })).toBeVisible();
    await expect(page).toHaveURL(/filtro=Confirmado/);
    await expect(page.locator('pc-odoo-list')).not.toContainText('PV-2026-0001');
  });

  test('guardar la búsqueda actual manda a la API sus filtros con nombre y su agrupación', async ({ page }) => {
    const { guardados } = await simularApi(page);
    await abrir(page, ANGULAR, '/ventas/pedidos?filtro=Confirmado&agrupar=Cliente');

    await menu(page).click();
    await page.locator('[data-favorito-nombre]').fill('Por autorizar');
    await page.locator('[data-favorito-guardar]').click();

    await expect.poll(() => guardados.length).toBe(1);
    expect(guardados[0]).toMatchObject({ definicion: { nombrados: ['Confirmado'], agruparPor: ['Cliente'] } });
  });
});
