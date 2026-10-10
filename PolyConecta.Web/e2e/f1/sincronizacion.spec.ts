import { expect, test } from '@playwright/test';
import { ANGULAR, abrir } from '../soporte/apps';
import { simularListas } from '../soporte/listas';
import { aviso, elegir, pestana } from '../soporte/pedido';

const fila = (page: import('@playwright/test').Page, texto: string) => page.locator('pc-odoo-list tbody tr', { hasText: texto });

/**
 * Escenarios de Sincronización, Productos y Clientes (F1 / US3, quickstart §3, L2-T018):
 * - Sistemas entra a Configuración › Sincronización y ve el estado de catálogos
 * - "Sincronizar todo" ejecuta POST /api/v1/plataforma/sincronizacion y actualiza la tabla
 * - "Sincronizar ahora" individual ejecuta POST /api/v1/plataforma/sincronizacion/{catalogo}
 * - Sincronización con error muestra badge y mensaje de error
 * - Productos: lista en Inventario › Productos, detalle con clasificación y captura de ficha técnica (Rollo y PT)
 * - Clientes: lista en Ventas › Clientes, detalle con datos de CONTPAQi y domicilios fiscales y de envío
 */
test.describe('Sincronización, productos y clientes (F1 / US3)', () => {
  const sesionSistemas = {
    autenticado: true,
    usuarioId: 10,
    nombreUsuario: 'sistemas',
    nombreVisible: 'Sistemas',
    permisos: [
      'plataforma.sincronizacion.leer',
      'plataforma.sincronizacion.ejecutar',
      'inventario.producto.leer',
      'inventario.producto.clasificar',
      'inventario.ficha.editar',
      'ventas.cliente.leer',
      'plataforma.usuarios.leer',
      'plataforma.grupos.leer',
    ],
  };

  const estadosIniciales = [
    {
      catalogo: 'productos',
      ultimaCorrida: '2026-10-08T10:00:00Z',
      ultimaExitosa: '2026-10-08T10:00:00Z',
      resultado: 'Exito',
      leidos: 150,
      cambiados: 10,
      archivados: 0,
      duracionMs: 1200,
      error: null,
    },
    {
      catalogo: 'clientes',
      ultimaCorrida: '2026-10-08T10:00:00Z',
      ultimaExitosa: '2026-10-08T10:00:00Z',
      resultado: 'Exito',
      leidos: 85,
      cambiados: 2,
      archivados: 0,
      duracionMs: 800,
      error: null,
    },
    {
      catalogo: 'almacenes',
      ultimaCorrida: '2026-10-08T10:00:00Z',
      ultimaExitosa: '2026-10-08T10:00:00Z',
      resultado: 'Exito',
      leidos: 5,
      cambiados: 0,
      archivados: 0,
      duracionMs: 150,
      error: null,
    },
  ];

  test('Sistemas consulta el estado de sincronización y ejecuta "Sincronizar todo"', async ({ page }) => {
    let sincronizadoTodo = false;

    await page.route(/\/api\/v1\/plataforma\/sesion/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(sesionSistemas),
      });
    });

    await page.route(/\/api\/v1\/plataforma\/sincronizacion$/, async route => {
      if (route.request().method() === 'POST') {
        sincronizadoTodo = true;
        const estadosActualizados = estadosIniciales.map(e => ({
          ...e,
          ultimaCorrida: new Date().toISOString(),
          ultimaExitosa: new Date().toISOString(),
          leidos: e.leidos + 5,
          cambiados: 1,
        }));
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(estadosActualizados),
        });
      } else {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(estadosIniciales),
        });
      }
    });

    await simularListas(page);

    await abrir(page, ANGULAR, '/plataforma/sincronizacion');

    // La lista de los contratos visuales (P-14): "Sincronizar todo" es la acción primaria.
    await expect(fila(page, 'Productos')).toBeVisible();
    await expect(fila(page, 'Clientes')).toBeVisible();

    const btnTodo = page.locator('[data-sincronizar-todo]');
    await expect(btnTodo).toBeEnabled();
    await btnTodo.click();

    await expect(aviso(page)).toContainText('Sincronización de todos los catálogos terminada');
    expect(sincronizadoTodo).toBe(true);
  });

  test('"Sincronizar ahora" individual por catálogo actualiza solo esa fila', async ({ page }) => {
    let catalogoSincronizado: string | null = null;

    await page.route(/\/api\/v1\/plataforma\/sesion/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(sesionSistemas),
      });
    });

    await page.route(/\/api\/v1\/plataforma\/sincronizacion$/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(estadosIniciales),
      });
    });

    await page.route(/\/api\/v1\/plataforma\/sincronizacion\/productos/, async route => {
      catalogoSincronizado = 'productos';
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          catalogo: 'productos',
          ultimaCorrida: new Date().toISOString(),
          ultimaExitosa: new Date().toISOString(),
          resultado: 'Exito',
          leidos: 155,
          cambiados: 0,
          archivados: 1,
          duracionMs: 950,
          error: null,
        }),
      });
    });

    await simularListas(page);

    await abrir(page, ANGULAR, '/plataforma/sincronizacion');

    // "Sincronizar ahora" es una acción contextual de la selección, en el menú "Acciones" (07 §1.1).
    await fila(page, 'Productos').locator('input[type="checkbox"]').check();
    await page.locator('.o_control_panel [data-lista="acciones"]').click();
    await page.locator('[data-accion-masiva="Sincronizar ahora"]').click();

    await expect(aviso(page)).toContainText('Productos sincronizado');
    expect(catalogoSincronizado).toBe('productos');
    await expect(fila(page, 'Productos')).toContainText('155');
  });

  test('Fallo de sincronización muestra badge y detalle de error', async ({ page }) => {
    await page.route(/\/api\/v1\/plataforma\/sesion/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(sesionSistemas),
      });
    });

    await page.route(/\/api\/v1\/plataforma\/sincronizacion$/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            catalogo: 'productos',
            ultimaCorrida: '2026-10-08T10:00:00Z',
            ultimaExitosa: null,
            resultado: 'Fallo',
            leidos: 0,
            cambiados: 0,
            archivados: 0,
            duracionMs: 5000,
            error: 'Bridge no responde (SDK_TIMEOUT)',
          },
        ]),
      });
    });

    await simularListas(page);

    await abrir(page, ANGULAR, '/plataforma/sincronizacion');

    await expect(fila(page, 'Productos')).toContainText('Error');
    await expect(fila(page, 'Productos')).toContainText('Bridge no responde (SDK_TIMEOUT)');
  });

  test('Lista de productos, clasificación y captura de ficha técnica', async ({ page }) => {
    let clasificacionGuardada: number | null = null;
    let fichaGuardada: any = null;

    // `ProductoDetalle` de la API: la ficha técnica viaja en `ficha` (FR-018).
    const productoMock = {
      id: 42,
      clave: 'PROD-PT-001',
      nombre: 'Bolsa Polietileno Impresa 50x70',
      etiqueta: 'PROD-PT-001 - Bolsa Polietileno Impresa 50x70',
      unidad: 'MILLAR',
      llevaLote: true,
      clasificacionId: null as number | null,
      clasificacion: null as string | null,
      activo: true,
      ficha: null as unknown,
      acciones: [
        { accion: 'clasificar', disponible: true, razon: null },
        { accion: 'editar_ficha', disponible: true, razon: null },
      ],
    };

    const clasificacionesMock = [
      { id: 1, codigo: 'ROLLO', nombre: 'Rollo o Bobina' },
      { id: 2, codigo: 'PT', nombre: 'Producto Terminado' },
    ];

    await page.route(/\/api\/v1\/plataforma\/sesion/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(sesionSistemas),
      });
    });

    const productosLista = [
      {
        id: 42,
        codigo: 'PROD-PT-001',
        nombre: 'Bolsa Polietileno Impresa 50x70',
        unidadBase: 'MILLAR',
        clasificacion: 'Sin clasificar',
        activo: true,
        _filtros: ['Activos'],
      },
    ];

    await page.route(/\/api\/v1\/inventario\/clasificaciones/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(clasificacionesMock),
      });
    });

    await page.route(/\/api\/v1\/inventario\/productos/, async route => {
      const url = route.request().url();
      const method = route.request().method();

      if (url.includes('/conjunto')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            completo: true,
            total: productosLista.length,
            filas: productosLista,
          }),
        });
      } else if (url.includes('/consulta')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            filas: productosLista,
            grupos: null,
            total: productosLista.length,
            totales: {},
          }),
        });
      } else if (url.endsWith('/clasificacion') && method === 'PUT') {
        const data = JSON.parse(route.request().postData() || '{}');
        clasificacionGuardada = data.clasificacionId;
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            ...productoMock,
            clasificacionId: clasificacionGuardada,
          }),
        });
      } else if (url.endsWith('/ficha-tecnica') && method === 'PUT') {
        fichaGuardada = JSON.parse(route.request().postData() || '{}');
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            ...productoMock,
            clasificacionId: clasificacionGuardada,
            ficha: { rollo: fichaGuardada.rollo, pt: fichaGuardada.pt, rolloLigadoProductoId: null, rolloLigadoProducto: null },
          }),
        });
      } else if (url.match(/\/productos\/42$/)) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            ...productoMock,
            clasificacionId: clasificacionGuardada,
            ficha: fichaGuardada ? { rollo: fichaGuardada.rollo, pt: fichaGuardada.pt, rolloLigadoProductoId: null, rolloLigadoProducto: null } : null,
          }),
        });
      } else {
        await route.fallback();
      }
    });

    // 1. Abrir lista de productos
    await simularListas(page);
    await abrir(page, ANGULAR, '/inventario/productos');
    await expect(page.getByText('PROD-PT-001')).toBeVisible();

    // 2. Abrir detalle del producto
    await page.getByText('PROD-PT-001').click();
    await expect(page.locator('[data-nombre-registro]')).toContainText('Bolsa Polietileno Impresa 50x70');

    // 3. Cambiar la clasificación en su lugar y guardar (D-164)
    await elegir(page, 'Clasificación', 'Terminado');
    await page.locator('[data-guardar]').click();
    await expect(aviso(page)).toContainText('Producto guardado');
    expect(clasificacionGuardada).toBe(2);

    // 4. Ficha técnica en sus pestañas Rollo y PT
    await pestana(page, 'rollo');
    await page.locator('[data-campo="materialType"] input').fill('Polietileno BD');
    await page.locator('[data-campo="rollTypeSize"] input').fill('50 cm');
    await page.locator('[data-campo="gaugeMicrons"] input').fill('60');
    await page.locator('[data-campo="gaugeMicrons"] input').press('Tab');
    await pestana(page, 'pt');
    await page.locator('[data-campo="customerPartNumber"] input').fill('PARTE-CLIENTE-123');
    await page.locator('[data-campo="finalSize"] input').fill('50x70 cm');

    // 5. Guardar la ficha técnica
    await page.locator('[data-guardar]').click();
    await expect(aviso(page)).toContainText('Producto guardado');
    expect(fichaGuardada).not.toBeNull();
    expect(fichaGuardada.rollo.materialType).toBe('Polietileno BD');
    expect(fichaGuardada.pt.customerPartNumber).toBe('PARTE-CLIENTE-123');
  });

  test('Lista de clientes y consulta de detalle con domicilios de CONTPAQi', async ({ page }) => {
    const clienteMock = {
      id: 99,
      clave: 'CLI-001',
      codigo: 'CLI-001',
      razonSocial: 'Empaques del Norte S.A. de C.V.',
      rfc: 'ENO900101XYZ',
      moneda: 'MXN',
      activo: true,
      domicilios: [
        {
          id: 1,
          tipo: 'Fiscal',
          texto: 'Av. Industrial 123, Parque Industrial, Monterrey, NL',
        },
        {
          id: 2,
          tipo: 'Envio',
          sucursal: 'Planta 2 Santa Catarina',
          texto: 'Carretera Saltillo Km 15, Santa Catarina, NL',
        },
      ],
    };

    const clientesLista = [
      {
        id: 99,
        codigo: 'CLI-001',
        razonSocial: 'Empaques del Norte S.A. de C.V.',
        rfc: 'ENO900101XYZ',
        moneda: 'MXN',
        activo: true,
        _filtros: ['Activos'],
      },
    ];

    await page.route(/\/api\/v1\/plataforma\/sesion/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(sesionSistemas),
      });
    });

    await page.route(/\/api\/v1\/ventas\/clientes/, async route => {
      const url = route.request().url();
      if (url.includes('/conjunto')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            completo: true,
            total: clientesLista.length,
            filas: clientesLista,
          }),
        });
      } else if (url.includes('/consulta')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            filas: clientesLista,
            grupos: null,
            total: clientesLista.length,
            totales: {},
          }),
        });
      } else if (url.match(/\/clientes\/99$/)) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(clienteMock),
        });
      } else {
        await route.fallback();
      }
    });

    // 1. Abrir lista de clientes
    await simularListas(page);
    await abrir(page, ANGULAR, '/ventas/clientes');
    await expect(page.getByText('Empaques del Norte S.A. de C.V.')).toBeVisible();

    // 2. Abrir detalle de cliente
    await page.getByText('Empaques del Norte S.A. de C.V.').click();
    await expect(page.locator('[data-nombre-registro]')).toContainText('Empaques del Norte');
    await expect(page.locator('pc-odoo-maestro')).toContainText('CLI-001');

    // 3. Verificar domicilio fiscal y domicilio de envío
    await expect(page.getByText('Av. Industrial 123')).toBeVisible();
    await expect(page.getByText('Planta 2 Santa Catarina')).toBeVisible();
    await expect(page.getByText('Carretera Saltillo Km 15')).toBeVisible();
  });
});
