import { expect, Page, test } from '@playwright/test';
import { ANGULAR, abrir } from '../soporte/apps';
import { botonBarra, capturarLinea, elegir, lineas } from '../soporte/pedido';

/**
 * Sesión vencida durante la captura (spec 003, caso límite; L2-T010). Sobre el formulario real
 * de "Nuevo pedido": el guardado responde `401`; con captura pendiente se abre el diálogo de inicio
 * de sesión sobre la página y, al entrar, se repite la petición con lo capturado. Sin captura, un
 * `401` lleva a `/login?volver=`. La API se simula: un `401` a mitad de la captura no se provoca
 * de otro modo.
 */
const sesion = {
  usuario: { id: 10, usuario: 'ac1', nombre: 'Atención Clientes 1' },
  asignaciones: [{ grupo: 'ATENCION_CLIENTES', nombreGrupo: 'Atención a Clientes', planta: 'PIM', suplente: false }],
  permisos: ['ventas.pedido.leer', 'ventas.pedido.crear', 'ventas.pedido.editar'],
};

const cliente = {
  id: 3,
  clave: 'EMM-001',
  nombre: 'EMPRESA MEXICANA DE MANUFACTURA',
  etiqueta: 'EMM-001 · EMPRESA MEXICANA DE MANUFACTURA',
  moneda: 'MXN',
  domiciliosEnvio: [{ id: 11, texto: 'Planta Monterrey: Av. Industrial 120, Apodaca, N.L.' }],
};

const producto = {
  id: 42,
  clave: 'PT1113 C567',
  nombre: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]',
  etiqueta: 'PT1113 C567 · BOLSA MEDIANA 44X84',
  unidad: 'MIL',
  unidadId: 1,
  llevaLote: true,
};

const json = (body: unknown) => ({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });

async function simularCatalogos(page: Page): Promise<{ entradas: unknown[] }> {
  const entradas: unknown[] = [];
  await page.route(/\/api\/v1\/plataforma\/sesion$/, async route => {
    if (route.request().method() === 'POST') {
      entradas.push(JSON.parse(route.request().postData() || '{}'));
    }
    await route.fulfill(json(sesion));
  });
  await page.route(/\/api\/v1\/ventas\/clientes\/buscar/, route => route.fulfill(json([cliente])));
  await page.route(/\/api\/v1\/inventario\/productos\/buscar/, route => route.fulfill(json([producto])));
  await page.route(/\/api\/v1\/ventas\/agentes/, route => route.fulfill(json([])));
  return { entradas };
}

async function capturarPedido(page: Page): Promise<void> {
  await elegir(page, 'Cliente', 'EMPRESA');
  await capturarLinea(page, 'PT1113 C567', '100', '7.5');
  await expect(lineas(page)).toContainText(['PT1113 C567']);
}

test.describe('Sesión vencida durante la captura (F1)', () => {
  test('con captura pendiente, entra en el diálogo y guarda sin perder lo capturado', async ({ page }) => {
    const { entradas } = await simularCatalogos(page);
    const guardados: Array<{ clienteId: number; lineas: unknown[] }> = [];
    await page.route(/\/api\/v1\/ventas\/pedidos$/, async route => {
      if (route.request().method() !== 'POST') return route.fallback();
      guardados.push(JSON.parse(route.request().postData() || '{}'));
      if (guardados.length === 1) {
        await route.fulfill({ status: 401, contentType: 'application/json', body: '{}' });
        return;
      }
      await route.fulfill(json({ id: 99, folio: 'PV-2026-0099', estado: 'Borrador' }));
    });
    await page.route(/\/api\/v1\/ventas\/pedidos\/99$/, route => route.fulfill({ status: 404, body: '{}' }));

    await abrir(page, ANGULAR, '/ventas/pedidos/nuevo');
    await capturarPedido(page);
    await botonBarra(page, 'Guardar').click();

    const dialogo = page.locator('pc-dialogo-login');
    await expect(dialogo).toBeVisible();
    await expect(page).toHaveURL(/\/ventas\/pedidos\/nuevo$/);
    await dialogo.locator('[data-login-usuario]').fill('ac1');
    await dialogo.locator('[data-login-contrasena]').fill('clave123');
    await dialogo.getByRole('button', { name: 'Reanudar sesión' }).click();

    await expect(page).toHaveURL(/\/ventas\/pedidos\/99$/);
    expect(entradas).toEqual([{ usuario: 'ac1', contrasena: 'clave123' }]);
    expect(guardados).toHaveLength(2);
    expect(guardados[1]).toEqual(guardados[0]);
    expect(guardados[1].clienteId).toBe(3);
    expect(guardados[1].lineas).toHaveLength(1);
  });

  test('sin captura pendiente, un 401 lleva a /login con la ruta de regreso', async ({ page }) => {
    await simularCatalogos(page);
    await page.route(/\/api\/v1\/ventas\/pedidos\/(conjunto|consulta)$/, route =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{}' })
    );

    await abrir(page, ANGULAR, '/ventas/pedidos');

    await expect(page).toHaveURL(/\/login\?volver=%2Fventas%2Fpedidos$/);
    await expect(page.locator('pc-dialogo-login')).toHaveCount(0);
  });
});
