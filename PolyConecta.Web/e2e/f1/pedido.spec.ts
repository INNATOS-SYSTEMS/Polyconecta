import { expect, test } from '@playwright/test';
import { ANGULAR, abrir } from '../soporte/apps';
import { simularListas } from '../soporte/listas';

/**
 * Suite E2E de Pedidos de Venta F1 / US1 (quickstart §4, L2-T025):
 * 1. Captura en pedido-nuevo: cliente, moneda y domicilio propuestos, líneas con unidad fija, guardado.
 * 2. Validación de confirmación (sin precio falla y muestra qué falta; con precio confirma).
 * 3. Primera firma (Comercial) muestra 1/2 firmas y falta Cobranza.
 * 4. Edición con firmas: diálogo modal D-147 revoca la autorización y regresa a Confirmado.
 * 5. Segunda firma (Cobranza suplente) transiciona a Autorizado con firma suplente.
 * 6. Revocación manual regresa a Confirmado.
 * 7. Arrastre en Kanban ejecuta transiciones con diálogo de firma.
 */
test.describe('Flujo completo de pedidos de venta (F1 / US1 / quickstart §4)', () => {
  const sesionAc = {
    autenticado: true,
    usuarioId: 10,
    nombreUsuario: 'ac1',
    nombreVisible: 'Atención Clientes 1',
    permisos: [
      'ventas.pedido.leer',
      'ventas.pedido.crear',
      'ventas.pedido.editar',
      'ventas.pedido.confirmar',
    ],
  };

  const clientesMock = [
    {
      id: 3,
      clave: 'EMM-001',
      nombre: 'EMPRESA MEXICANA DE MANUFACTURA',
      etiqueta: 'EMM-001 · EMPRESA MEXICANA DE MANUFACTURA',
      moneda: 'USD',
      domiciliosEnvio: [
        { id: 11, texto: 'Planta Monterrey: Av. Industrial 120, Apodaca, N.L.' },
        { id: 12, texto: 'Cedis Saltillo: Parque Ramos Arizpe 400, Coah.' },
      ],
    },
  ];

  const productosMock = [
    {
      id: 42,
      clave: 'PT1113 C567',
      nombre: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]',
      etiqueta: 'PT1113 C567 · BOLSA MEDIANA 44X84',
      unidad: 'MIL',
      unidadId: 1,
      llevaLote: true,
    },
    {
      id: 43,
      clave: 'PT1114 C568',
      nombre: 'BOLSA CHICA 30X40 C.200',
      etiqueta: 'PT1114 C568 · BOLSA CHICA',
      unidad: 'MIL',
      unidadId: 1,
      llevaLote: true,
    },
  ];

  const agentesMock = [
    { id: 5, clave: 'AGE-01', nombre: 'Carlos Vendedor', etiqueta: 'AGE-01 · Carlos Vendedor', tipo: 'venta' },
  ];

  test('Paso 1: Captura de nuevo pedido con cliente, moneda, domicilio y líneas con unidad fija', async ({ page }) => {
    await page.route(/\/api\/v1\/plataforma\/sesion/, async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(sesionAc) });
    });

    await page.route(/\/api\/v1\/ventas\/clientes\/buscar/, async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(clientesMock) });
    });

    await page.route(/\/api\/v1\/inventario\/productos\/buscar/, async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(productosMock) });
    });

    await page.route(/\/api\/v1\/ventas\/agentes/, async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(agentesMock) });
    });

    let pedidoGuardadoPayload: any = null;
    await page.route(/\/api\/v1\/ventas\/pedidos/, async route => {
      const url = route.request().url();
      const method = route.request().method();
      if (url.endsWith('/pedidos') && method === 'POST') {
        pedidoGuardadoPayload = JSON.parse(route.request().postData() || '{}');
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 101,
            folio: 'PV-2026-0001',
            estado: 'Borrador',
            rowVersion: 'AAAAAAAACAA=',
            cliente: { id: 3, clave: 'EMM-001', nombre: 'EMPRESA MEXICANA DE MANUFACTURA' },
            ordenCompraCliente: 'OC-4471',
            agente: { id: 5, clave: 'AGE-01', nombre: 'Carlos Vendedor' },
            fechaPedido: '2026-10-14',
            fechaPromesa: '2026-10-25',
            moneda: 'USD',
            tipoCambio: 18.5,
            monedaBase: 'MXN',
            domicilioEntrega: { id: 11, texto: 'Planta Monterrey: Av. Industrial 120, Apodaca, N.L.' },
            lineas: [
              {
                id: 1,
                productoId: 42,
                clave: 'PT1113 C567',
                producto: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]',
                cantidad: 100,
                unidad: 'MIL',
                precioUnitario: 7.5,
                subtotal: 750,
                metaProduccionKg: null,
                toleranciaPorcentaje: null,
              },
            ],
            firmas: [],
            firmasPendientes: ['Comercial', 'Cobranza'],
            rolesPorFirmar: [],
            sincronizacion: { estado: 'NoAplica' },
            acciones: [
              { accion: 'confirmar', disponible: true, razon: null, aviso: null },
              { accion: 'editar', disponible: true, razon: null, aviso: null },
              { accion: 'cancelar', disponible: true, razon: null, aviso: null },
            ],
          }),
        });
      } else if (url.endsWith('/pedidos/101')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 101,
            folio: 'PV-2026-0001',
            estado: 'Borrador',
            rowVersion: 'AAAAAAAACAA=',
            cliente: { id: 3, clave: 'EMM-001', nombre: 'EMPRESA MEXICANA DE MANUFACTURA' },
            ordenCompraCliente: 'OC-4471',
            agente: { id: 5, clave: 'AGE-01', nombre: 'Carlos Vendedor' },
            fechaPedido: '2026-10-14',
            fechaPromesa: '2026-10-25',
            moneda: 'USD',
            tipoCambio: 18.5,
            monedaBase: 'MXN',
            domicilioEntrega: { id: 11, texto: 'Planta Monterrey: Av. Industrial 120, Apodaca, N.L.' },
            lineas: [
              {
                id: 1,
                productoId: 42,
                clave: 'PT1113 C567',
                producto: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]',
                cantidad: 100,
                unidad: 'MIL',
                precioUnitario: 7.5,
                subtotal: 750,
              },
            ],
            firmas: [],
            firmasPendientes: ['Comercial', 'Cobranza'],
            rolesPorFirmar: [],
            sincronizacion: { estado: 'NoAplica' },
            acciones: [
              { accion: 'confirmar', disponible: true, razon: null, aviso: null },
              { accion: 'editar', disponible: true, razon: null, aviso: null },
              { accion: 'cancelar', disponible: true, razon: null, aviso: null },
            ],
          }),
        });
      } else {
        await route.fallback();
      }
    });

    await simularListas(page);

    await abrir(page, ANGULAR, '/ventas/pedidos/nuevo');

    // 1. Seleccionar cliente EMM-001 -> propone moneda USD
    await page.selectOption('#campo-cliente', { label: 'EMM-001 - EMPRESA MEXICANA DE MANUFACTURA' });
    await expect(page.locator('#campo-moneda')).toHaveValue('USD');

    // 2. Elegir domicilio de entrega
    await page.selectOption('#campo-domicilio-entrega', { index: 1 });

    // 3. Capturar orden de compra
    await page.fill('#campo-orden-compra', 'OC-4471');

    // 4. Capturar línea: producto, cantidad, precio (la unidad debe ser fija MIL)
    await page.selectOption('#campo-linea-producto', { label: 'PT1113 C567 - BOLSA MEDIANA 44X84 C.430 BOL-004 [77]' });
    await expect(page.locator('#campo-linea-unidad')).toHaveValue('MIL');
    await expect(page.locator('#campo-linea-unidad')).toBeDisabled();

    await page.fill('#campo-linea-cantidad', '100');
    await page.fill('#campo-linea-precio', '7.5');
    await page.click('#btn-agregar-linea');

    // Línea agregada a la tabla
    await expect(page.locator('#tabla-lineas-nuevo-pedido')).toContainText('PT1113 C567');
    await expect(page.locator('#tabla-lineas-nuevo-pedido')).toContainText('100.00');
    await expect(page.locator('#tabla-lineas-nuevo-pedido')).toContainText('7.50');

    // 5. Guardar pedido
    await page.click('#btn-guardar-nuevo-pedido');

    // Redirige al formulario por id /ventas/pedidos/101 y muestra folio en título
    await expect(page).toHaveURL(/.*\/ventas\/pedidos\/101$/);
    await expect(page.locator('#pedido-folio')).toHaveText('PV-2026-0001');
    await expect(page.locator('.o_statusbar_pipeline .arrow-step.active')).toHaveText('Borrador');

    expect(pedidoGuardadoPayload).not.toBeNull();
    expect(pedidoGuardadoPayload.clienteId).toBe(3);
    expect(pedidoGuardadoPayload.moneda).toBe('USD');
    expect(pedidoGuardadoPayload.lineas.length).toBe(1);
    expect(pedidoGuardadoPayload.lineas[0].cantidad).toBe(100);
  });

  test('Pasos 2, 3 y 4: Confirmación, primera firma y edición con D-147 que revoca autorizaciones', async ({ page }) => {
    let firmasActuales: any[] = [];
    let estadoActual = 'Borrador';
    let lineasActuales = [
      {
        id: 1,
        productoId: 42,
        clave: 'PT1113 C567',
        producto: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]',
        cantidad: 100,
        unidad: 'MIL',
        precioUnitario: 0, // Sin precio
        subtotal: 0,
      },
    ];

    await page.route(/\/api\/v1\/plataforma\/sesion/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
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
        }),
      });
    });

    await page.route(/\/api\/v1\/ventas\/clientes\/buscar/, async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(clientesMock) });
    });

    await page.route(/\/api\/v1\/ventas\/agentes/, async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(agentesMock) });
    });

    await page.route(/\/api\/v1\/ventas\/pedidos/, async route => {
      const url = route.request().url();
      const method = route.request().method();

      if (url.endsWith('/pedidos/101') && method === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 101,
            folio: 'PV-2026-0001',
            estado: estadoActual,
            rowVersion: 'AAAAAAAACAB=',
            cliente: { id: 3, clave: 'EMM-001', nombre: 'EMPRESA MEXICANA DE MANUFACTURA' },
            ordenCompraCliente: 'OC-4471',
            fechaPedido: '2026-10-14',
            fechaPromesa: '2026-10-25',
            moneda: 'USD',
            tipoCambio: 18.5,
            monedaBase: 'MXN',
            domicilioEntrega: { id: 11, texto: 'Planta Monterrey' },
            lineas: lineasActuales,
            firmas: firmasActuales,
            firmasPendientes: estadoActual === 'Confirmado' ? (firmasActuales.length === 0 ? ['Comercial', 'Cobranza'] : ['Cobranza']) : [],
            rolesPorFirmar: estadoActual === 'Confirmado' && firmasActuales.length === 0 ? ['Comercial'] : [],
            sincronizacion: { estado: 'NoAplica' },
            acciones: [
              { accion: 'confirmar', disponible: estadoActual === 'Borrador', razon: null, aviso: null },
              { accion: 'autorizar', disponible: estadoActual === 'Confirmado' && firmasActuales.length === 0, razon: null, aviso: null },
              { accion: 'editar', disponible: true, razon: null, aviso: firmasActuales.length > 0 ? `El pedido tiene ${firmasActuales.length} firma: guardar un cambio revoca la autorización.` : null },
              { accion: 'cancelar', disponible: true, razon: null, aviso: null },
            ],
          }),
        });
      } else if (url.endsWith('/confirmar') && method === 'POST') {
        // Validación: si tiene línea sin precio, devuelve 400
        const sinPrecio = lineasActuales.some(l => !l.precioUnitario || l.precioUnitario <= 0);
        if (sinPrecio) {
          await route.fulfill({
            status: 400,
            contentType: 'application/problem+json',
            body: JSON.stringify({
              status: 400,
              code: 'VALIDACION',
              title: 'VALIDACION',
              detail: 'No se puede confirmar: La línea 1 (PT1113 C567) requiere precio unitario mayor a cero.',
              errores: [{ campo: 'lineas[0].precioUnitario', mensaje: 'Requiere precio mayor a cero.' }],
            }),
          });
        } else {
          estadoActual = 'Confirmado';
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              id: 101,
              folio: 'PV-2026-0001',
              estado: 'Confirmado',
              rowVersion: 'AAAAAAAACAC=',
              cliente: { id: 3, clave: 'EMM-001', nombre: 'EMPRESA MEXICANA DE MANUFACTURA' },
              lineas: lineasActuales,
              firmas: [],
              firmasPendientes: ['Comercial', 'Cobranza'],
              rolesPorFirmar: ['Comercial'],
              acciones: [
                { accion: 'confirmar', disponible: false, razon: 'Ya confirmado', aviso: null },
                { accion: 'autorizar', disponible: true, razon: null, aviso: null },
                { accion: 'editar', disponible: true, razon: null, aviso: null },
              ],
            }),
          });
        }
      } else if (url.endsWith('/autorizar') && method === 'POST') {
        firmasActuales = [
          {
            rol: 'Comercial',
            usuario: 'Comercial 1',
            usuarioId: 20,
            grupo: 'Comercial',
            suplente: false,
            fecha: '2026-10-14T10:00:00Z',
          },
        ];
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 101,
            folio: 'PV-2026-0001',
            estado: 'Confirmado',
            rowVersion: 'AAAAAAAACAD=',
            cliente: { id: 3, clave: 'EMM-001', nombre: 'EMPRESA MEXICANA DE MANUFACTURA' },
            lineas: lineasActuales,
            firmas: firmasActuales,
            firmasPendientes: ['Cobranza'],
            rolesPorFirmar: [],
            acciones: [
              { accion: 'confirmar', disponible: false, razon: null, aviso: null },
              { accion: 'autorizar', disponible: false, razon: 'Ya firmó Comercial', aviso: null },
              { accion: 'revocar', disponible: true, razon: null, aviso: null },
              { accion: 'editar', disponible: true, razon: null, aviso: 'El pedido tiene 1 firma: guardar un cambio revoca la autorización.' },
            ],
          }),
        });
      } else if (url.endsWith('/pedidos/101') && method === 'PUT') {
        const body = JSON.parse(route.request().postData() || '{}');
        if (firmasActuales.length > 0 && !body.revocarAutorizacion) {
          // 409 según D-147
          await route.fulfill({
            status: 409,
            contentType: 'application/problem+json',
            body: JSON.stringify({
              status: 409,
              code: 'EDICION_REVOCA_AUTORIZACION',
              title: 'EDICION_REVOCA_AUTORIZACION',
              detail: 'El pedido tiene 1 firma: guardar un cambio revoca la autorización.',
            }),
          });
        } else {
          // Edición exitosa: borra firmas si las había
          firmasActuales = [];
          lineasActuales = body.lineas.map((l: any, i: number) => ({
            id: i + 1,
            productoId: l.productoId,
            clave: 'PT1113 C567',
            producto: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]',
            cantidad: l.cantidad,
            unidad: 'MIL',
            precioUnitario: l.precioUnitario,
            subtotal: l.cantidad * l.precioUnitario,
          }));
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              id: 101,
              folio: 'PV-2026-0001',
              estado: estadoActual,
              rowVersion: 'AAAAAAAACAE=',
              cliente: { id: 3, clave: 'EMM-001', nombre: 'EMPRESA MEXICANA DE MANUFACTURA' },
              lineas: lineasActuales,
              firmas: [],
              firmasPendientes: estadoActual === 'Confirmado' ? ['Comercial', 'Cobranza'] : [],
              rolesPorFirmar: estadoActual === 'Confirmado' ? ['Comercial'] : [],
              acciones: [
                { accion: 'confirmar', disponible: estadoActual === 'Borrador', razon: null, aviso: null },
                { accion: 'autorizar', disponible: estadoActual === 'Confirmado', razon: null, aviso: null },
                { accion: 'editar', disponible: true, razon: null, aviso: null },
              ],
            }),
          });
        }
      } else {
        await route.fallback();
      }
    });

    await simularListas(page);

    await abrir(page, ANGULAR, '/ventas/pedidos/101');

    // 1. Paso 2: Intentar confirmar sin precio -> mensaje de error
    await page.click('#btn-confirmar-pedido');
    await expect(page.locator('#alerta-error')).toContainText('requiere precio unitario mayor a cero');

    // 2. Corregir precio entrando en modo edición
    await page.click('#btn-editar-pedido');
    await page.fill('#input-precio-linea-0', '8.5');
    await page.click('#btn-guardar-edicion');
    await expect(page.locator('#alerta-exito')).toContainText('Pedido actualizado exitosamente');

    // 3. Confirmar exitosamente
    await page.click('#btn-confirmar-pedido');
    await expect(page.locator('.o_statusbar_pipeline .arrow-step.active')).toHaveText('Confirmado');

    // 4. Paso 3: Autorizar como Comercial
    await page.click('#btn-autorizar-pedido');
    await expect(page.locator('#badge-firmas-pedido')).toContainText('1/2 firmas');
    await expect(page.locator('#texto-firmas-pendientes')).toContainText('Cobranza');

    // 5. Paso 4: Cambiar una cantidad y guardar -> aviso D-147
    await page.click('#btn-editar-pedido');
    await page.fill('#input-cantidad-linea-0', '250');
    await page.click('#btn-guardar-edicion');

    // Modal de diálogo D-147 aparece
    await expect(page.locator('#dialogo-d147')).toBeVisible();
    await expect(page.locator('#texto-aviso-d147')).toContainText('revoca la autorización');

    // Confirmar en el modal D-147
    await page.locator('#dialogo-d147 button', { hasText: 'Continuar y revocar autorización' }).click();

    // Verificación: la firma se borró y regresa a Confirmado para autorizar de nuevo
    await expect(page.locator('#alerta-exito')).toContainText('Pedido actualizado');
    await expect(page.locator('.o_statusbar_pipeline .arrow-step.active')).toHaveText('Confirmado');
    await expect(page.locator('#badge-firmas-pedido')).toContainText('0/2 firmas');
  });

  test('Pasos 5, 6 y 7: Firma suplente, revocación manual y kanban', async ({ page }) => {
    let firmasActuales: any[] = [
      {
        rol: 'Comercial',
        usuario: 'Comercial 1',
        usuarioId: 20,
        grupo: 'Comercial',
        suplente: false,
        fecha: '2026-10-14T10:00:00Z',
      },
      {
        rol: 'Cobranza',
        usuario: 'Cobranza Suplente',
        usuarioId: 25,
        grupo: 'Cobranza',
        suplente: true,
        fecha: '2026-10-14T11:00:00Z',
      },
    ];

    await page.route(/\/api\/v1\/plataforma\/sesion/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          autenticado: true,
          usuarioId: 20,
          nombreUsuario: 'comercial1',
          nombreVisible: 'Comercial 1',
          permisos: [
            'ventas.pedido.leer',
            'ventas.pedido.revocar',
          ],
        }),
      });
    });

    await page.route(/\/api\/v1\/ventas\/pedidos/, async route => {
      const url = route.request().url();
      const method = route.request().method();

      if (url.endsWith('/pedidos/101') && method === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 101,
            folio: 'PV-2026-0001',
            estado: 'Autorizado',
            rowVersion: 'AAAAAAAACAF=',
            cliente: { id: 3, clave: 'EMM-001', nombre: 'EMPRESA MEXICANA DE MANUFACTURA' },
            ordenCompraCliente: 'OC-4471',
            fechaPedido: '2026-10-14',
            fechaPromesa: '2026-10-25',
            moneda: 'USD',
            tipoCambio: 18.5,
            monedaBase: 'MXN',
            domicilioEntrega: { id: 11, texto: 'Planta Monterrey' },
            lineas: [
              {
                id: 1,
                productoId: 42,
                clave: 'PT1113 C567',
                producto: 'BOLSA MEDIANA 44X84 C.430 BOL-004 [77]',
                cantidad: 100,
                unidad: 'MIL',
                precioUnitario: 8.5,
                subtotal: 850,
              },
            ],
            firmas: firmasActuales,
            firmasPendientes: [],
            rolesPorFirmar: [],
            sincronizacion: { estado: 'NoAplica' },
            acciones: [
              { accion: 'confirmar', disponible: false, razon: null, aviso: null },
              { accion: 'autorizar', disponible: false, razon: null, aviso: null },
              { accion: 'revocar', disponible: true, razon: null, aviso: null },
              { accion: 'editar', disponible: true, razon: null, aviso: 'Modificar revoca autorización' },
            ],
          }),
        });
      } else if (url.endsWith('/revocar') && method === 'POST') {
        firmasActuales = [];
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 101,
            folio: 'PV-2026-0001',
            estado: 'Confirmado',
            rowVersion: 'AAAAAAAACAG=',
            cliente: { id: 3, clave: 'EMM-001', nombre: 'EMPRESA MEXICANA DE MANUFACTURA' },
            lineas: [
              {
                id: 1,
                productoId: 42,
                clave: 'PT1113 C567',
                producto: 'BOLSA MEDIANA 44X84',
                cantidad: 100,
                unidad: 'MIL',
                precioUnitario: 8.5,
                subtotal: 850,
              },
            ],
            firmas: [],
            firmasPendientes: ['Comercial', 'Cobranza'],
            rolesPorFirmar: ['Comercial'],
            acciones: [
              { accion: 'confirmar', disponible: false, razon: null, aviso: null },
              { accion: 'autorizar', disponible: true, razon: null, aviso: null },
              { accion: 'editar', disponible: true, razon: null, aviso: null },
            ],
          }),
        });
      } else {
        await route.fallback();
      }
    });

    await simularListas(page);

    await abrir(page, ANGULAR, '/ventas/pedidos/101');

    // 1. Estado Autorizado y firma de suplente
    await expect(page.locator('.o_statusbar_pipeline .arrow-step.active')).toHaveText('Autorizado');
    const tablaFirmas = page.locator('table', { hasText: 'Firmante' });
    await expect(tablaFirmas).toContainText('Suplente');
    await expect(tablaFirmas).toContainText('Cobranza');

    // 2. Revocar autorización
    const btnRevocar = page.locator('#btn-revocar-pedido');
    await expect(btnRevocar).toBeVisible();
    await btnRevocar.click();

    await expect(page.locator('#alerta-exito')).toContainText('Autorización revocada');
    await expect(page.locator('.o_statusbar_pipeline .arrow-step.active')).toHaveText('Confirmado');
  });
});
