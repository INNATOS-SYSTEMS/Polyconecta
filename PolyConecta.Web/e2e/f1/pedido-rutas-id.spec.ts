import { expect, test } from '@playwright/test';
import { ANGULAR, abrir } from '../soporte/apps';
import { simularListas } from '../soporte/listas';
import { aviso, pestana, simularCatalogosPedido } from '../soporte/pedido';

/**
 * Escenarios de rutas por id y acciones del pedido de venta (F1 / US1, L2-T024, D-154, FR-031a):
 * - Rutas por id (/ventas/pedidos/:id)
 * - Filas y tarjetas abren por id; el folio queda en migas y título
 * - Id inexistente o sin permiso muestra "Página no encontrada"
 * - Acciones disponibles (confirmar, autorizar, revocar) habilitadas según contrato
 */
test.describe('Pedidos de venta: rutas por id y acciones (F1 / US1 / L2-T024)', () => {
  const sesionComercial = {
    autenticado: true,
    usuarioId: 20,
    nombreUsuario: 'comercial1',
    nombreVisible: 'Comercial 1',
    permisos: [
      'ventas.pedido.leer',
      'ventas.pedido.crear',
      'ventas.pedido.editar',
      'ventas.pedido.confirmar',
      'ventas.pedido.firmar_comercial',
    ],
  };

  const pedidoMock = {
    id: 15,
    folio: 'PV-2026-0015',
    estado: 'Borrador',
    rowVersion: 'AAAAAAAAB9I=',
    cliente: { id: 3, clave: 'EMM-001', nombre: 'EMPRESA MEXICANA DE MANUFACTURA' },
    ordenCompraCliente: 'OC-4471',
    agente: { id: 4, clave: 'AGE-01', nombre: 'Carlos Vendedor' },
    fechaPedido: '2026-10-13',
    fechaPromesa: '2026-10-30',
    moneda: 'USD',
    tipoCambio: 18.5,
    monedaBase: 'MXN',
    domicilioEntrega: { id: 11, texto: 'Av. Industrial 120, Apodaca, N.L.' },
    lineas: [
      {
        id: 31,
        productoId: 42,
        clave: 'PT1113 C567',
        producto: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]',
        cantidad: 1200,
        unidad: 'PZA',
        precioUnitario: 0.85,
        subtotal: 1020,
        metaProduccionKg: null,
        toleranciaPorcentaje: null,
      },
    ],
    firmas: [],
    firmasPendientes: ['Comercial', 'Cobranza'],
    rolesPorFirmar: ['Comercial'],
    sincronizacion: { estado: 'NoAplica' },
    acciones: [
      { accion: 'confirmar', disponible: true, razon: null, aviso: null },
      { accion: 'autorizar', disponible: false, razon: 'El pedido debe estar confirmado', aviso: null },
      { accion: 'editar', disponible: true, razon: null, aviso: null },
      { accion: 'cancelar', disponible: true, razon: null, aviso: null },
    ],
  };

  const pedidosLista = [
    {
      id: 15,
      folio: 'PV-2026-0015',
      cliente: 'EMPRESA MEXICANA DE MANUFACTURA',
      fechaPromesa: '2026-10-30',
      estado: 'Borrador',
      _filtros: ['Borrador'],
    },
  ];

  test('La lista de pedidos abre por id y muestra el folio en el título y migas', async ({ page }) => {
    await page.route(/\/api\/v1\/plataforma\/sesion/, async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(sesionComercial) });
    });

    await page.route(/\/api\/v1\/ventas\/pedidos/, async route => {
      const url = route.request().url();
      if (url.includes('/conjunto')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ completo: true, total: 1, filas: pedidosLista }),
        });
      } else if (url.includes('/consulta')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ filas: pedidosLista, grupos: null, total: 1, totales: {} }),
        });
      } else if (url.match(/\/pedidos\/15$/)) {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(pedidoMock) });
      } else {
        await route.fallback();
      }
    });

    await simularListas(page);
    await simularCatalogosPedido(page);

    await abrir(page, ANGULAR, '/ventas/pedidos');
    await expect(page.getByText('PV-2026-0015')).toBeVisible();

    // Clic en la fila
    await page.getByText('PV-2026-0015').click();

    // URL debe contener el id numérico /ventas/pedidos/15 (D-154)
    await expect(page).toHaveURL(/.*\/ventas\/pedidos\/15$/);

    // Miga y encabezado muestran el folio, no el id
    await expect(page.locator('#pedido-folio')).toHaveText('PV-2026-0015');
    await expect(page.locator('.o_breadcrumb')).toContainText('PV-2026-0015');
    await expect(page.locator('.o_statusbar_pipeline .arrow-step.active')).toHaveText('Borrador');
  });

  test('Id inexistente muestra Página no encontrada', async ({ page }) => {
    await page.route(/\/api\/v1\/plataforma\/sesion/, async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(sesionComercial) });
    });

    await page.route(/\/api\/v1\/ventas\/pedidos\/999999/, async route => {
      await route.fulfill({ status: 404, contentType: 'application/json', body: JSON.stringify({ error: 'No existe el pedido' }) });
    });

    await simularListas(page);
    await simularCatalogosPedido(page);

    await abrir(page, ANGULAR, '/ventas/pedidos/999999');

    // Debe mostrar la vista de Página no encontrada
    await expect(page.locator('h4', { hasText: 'Página no encontrada' })).toBeVisible();
    await expect(page.getByText('La ruta solicitada no existe en PolyConecta.')).toBeVisible();
  });

  test('Confirmar y autorizar pedido habilitado según acciones y contrato', async ({ page }) => {
    let pedidoActual = { ...pedidoMock };

    await page.route(/\/api\/v1\/plataforma\/sesion/, async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(sesionComercial) });
    });

    await page.route(/\/api\/v1\/ventas\/pedidos/, async route => {
      const url = route.request().url();
      const method = route.request().method();

      if (url.match(/\/pedidos\/15$/)) {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(pedidoActual) });
      } else if (url.endsWith('/confirmar') && method === 'POST') {
        pedidoActual = {
          ...pedidoActual,
          estado: 'Confirmado',
          rowVersion: 'AAAAAAAAB9K=',
          acciones: [
            { accion: 'confirmar', disponible: false, razon: 'Ya está confirmado', aviso: null },
            { accion: 'autorizar', disponible: true, razon: null, aviso: null },
            { accion: 'editar', disponible: true, razon: null, aviso: 'Modificar revoca autorizaciones' },
            { accion: 'cancelar', disponible: true, razon: null, aviso: null },
          ],
        };
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(pedidoActual) });
      } else if (url.endsWith('/autorizar') && method === 'POST') {
        pedidoActual = {
          ...pedidoActual,
          estado: 'Confirmado',
          rowVersion: 'AAAAAAAAB9M=',
          firmas: [
            {
              rol: 'Comercial',
              usuario: 'Comercial 1',
              usuarioId: 20,
              grupo: 'Comercial',
              suplente: false,
              fecha: '2026-10-13T12:00:00Z',
            },
          ],
          firmasPendientes: ['Cobranza'],
          rolesPorFirmar: [],
          acciones: [
            { accion: 'confirmar', disponible: false, razon: 'Ya está confirmado', aviso: null },
            { accion: 'autorizar', disponible: false, razon: 'Ya firmaste como Comercial', aviso: null },
            { accion: 'revocar', disponible: true, razon: null, aviso: null },
            { accion: 'editar', disponible: true, razon: null, aviso: 'Modificar revoca autorizaciones' },
            { accion: 'cancelar', disponible: true, razon: null, aviso: null },
          ],
        };
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(pedidoActual) });
      } else {
        await route.fallback();
      }
    });

    await simularListas(page);
    await simularCatalogosPedido(page);

    await abrir(page, ANGULAR, '/ventas/pedidos/15');

    // 1. Confirmar pedido en Borrador
    const btnConfirmar = page.locator('#btn-confirmar-pedido');
    await expect(btnConfirmar).toBeVisible();
    await expect(btnConfirmar).toBeEnabled();
    await btnConfirmar.click();

    await expect(aviso(page)).toContainText('Pedido confirmado');
    await expect(page.locator('.o_statusbar_pipeline .arrow-step.active')).toHaveText('Confirmado');

    // 2. Autorizar pedido confirmado
    const btnAutorizar = page.locator('#btn-autorizar-pedido');
    await expect(btnAutorizar).toBeVisible();
    await expect(btnAutorizar).toBeEnabled();
    await btnAutorizar.click();

    await expect(aviso(page)).toContainText('Firma registrada');
    await expect(page.locator('#badge-firmas-pedido')).toHaveText('1/2');
    await pestana(page, 'firmas');
    await expect(page.locator('[data-firmas-pedido]')).toContainText('Comercial 1');
    await expect(page.locator('[data-firmas-pendientes]')).toContainText('Cobranza');
  });
});
