import { expect, test } from '@playwright/test';
import { ANGULAR, abrir } from '../soporte/apps';

/**
 * Escenarios de Usuarios y Grupos (spec 003, L2-T012, US2, quickstart §2):
 * - Administrador entra y ve usuarios y grupos en Configuración
 * - Grupo Comercial con permisos en pc-odoo-dual-list (árbol Módulo › Objeto › Acción)
 * - Mover permisos y guardar con PUT /api/v1/plataforma/grupos/{id}
 * - Crear grupo copiando otro (POST /api/v1/plataforma/grupos con copiarDe)
 * - Crear usuario con asignación grupo × planta × suplente y agente CONTPAQi
 * - Menú protegido por permisos (oculto si no tiene plataforma.*.leer)
 */
test.describe('Usuarios y grupos (F1 / US2)', () => {
  const arbolPermisos = [
    {
      modulo: 'ventas',
      etiqueta: 'Ventas',
      objetos: [
        {
          objeto: 'pedido',
          etiqueta: 'Pedido',
          tipo: 'documento',
          acciones: [
            { clave: 'ventas.pedido.crear', accion: 'crear', etiqueta: 'Crear' },
            { clave: 'ventas.pedido.firmar_comercial', accion: 'firmar_comercial', etiqueta: 'Firmar como Comercial' },
            { clave: 'ventas.pedido.firmar_cobranza', accion: 'firmar_cobranza', etiqueta: 'Firmar como Cobranza' },
          ],
        },
      ],
    },
    {
      modulo: 'plataforma',
      etiqueta: 'Plataforma',
      objetos: [
        {
          objeto: 'usuarios',
          etiqueta: 'Usuarios',
          tipo: 'funcionalidad',
          acciones: [
            { clave: 'plataforma.usuarios.leer', accion: 'leer', etiqueta: 'Consultar usuarios' },
            { clave: 'plataforma.usuarios.administrar', accion: 'administrar', etiqueta: 'Administrar usuarios' },
          ],
        },
        {
          objeto: 'grupos',
          etiqueta: 'Grupos',
          tipo: 'funcionalidad',
          acciones: [
            { clave: 'plataforma.grupos.leer', accion: 'leer', etiqueta: 'Consultar grupos' },
            { clave: 'plataforma.grupos.administrar', accion: 'administrar', etiqueta: 'Administrar grupos' },
          ],
        },
      ],
    },
  ];

  const gruposMock = [
    { id: 1, codigo: 'COMERCIAL', nombre: 'Comercial', descripcion: 'Ventas y Comercial', activo: true, miembros: 3, _filtros: ['Activos'] },
    { id: 2, codigo: 'SUPERVISOR', nombre: 'Supervisor de turno', descripcion: 'Planta y producción', activo: true, miembros: 2, _filtros: ['Activos'] },
  ];

  const plantasMock = [
    { id: 1, codigo: 'PIM', nombre: 'PIM (Apodaca)' },
    { id: 2, codigo: 'SC', nombre: 'Santa Cruz' },
  ];

  const agentesMock = [
    { id: 10, codigo: 'AG-01', nombre: 'Juan Agente', erpId: 'ERP-AG-1' },
  ];

  test.beforeEach(async ({ page }) => {
    // Sesión de admin
    await page.route(/\/api\/v1\/plataforma\/sesion/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          usuario: { id: 1, usuario: 'admin', nombre: 'Administrador PolyConecta' },
          asignaciones: [{ grupo: 'ADMIN', nombreGrupo: 'Administradores', planta: 'PIM', suplente: false }],
          permisos: [
            'plataforma.usuarios.leer',
            'plataforma.usuarios.administrar',
            'plataforma.grupos.leer',
            'plataforma.grupos.administrar',
            'ventas.pedido.leer',
          ],
        }),
      });
    });

    // Catálogos
    await page.route(/\/api\/v1\/plataforma\/plantas/, async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(plantasMock) });
    });

    await page.route(/\/api\/v1\/ventas\/agentes/, async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(agentesMock) });
    });

    await page.route(/\/api\/v1\/plataforma\/permisos/, async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(arbolPermisos) });
    });
  });

  test('administrador ve menús de Configuración y lista de grupos', async ({ page }) => {
    await page.route(/\/api\/v1\/plataforma\/grupos/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          completo: true,
          total: gruposMock.length,
          filas: gruposMock,
        }),
      });
    });

    await abrir(page, ANGULAR, '/plataforma/grupos');

    // Barra superior: nombre del módulo
    await expect(page.locator('.o_module_name')).toHaveText('Configuración');

    // Menús visibles
    await expect(page.locator('.o_header_menu a', { hasText: 'Usuarios' })).toBeVisible();
    await expect(page.locator('.o_header_menu a', { hasText: 'Grupos' })).toBeVisible();

    // Tabla de grupos
    await expect(page.getByRole('cell', { name: 'Comercial', exact: true })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'Supervisor de turno' })).toBeVisible();
  });

  test('abrir grupo Comercial, visualizar dual-list de permisos y guardar cambios', async ({ page }) => {
    let grupoEditado: any = null;

    await page.route(/\/api\/v1\/plataforma\/grupos/, async route => {
      const url = route.request().url();
      const method = route.request().method();

      if (url.includes('/conjunto')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ completo: true, total: 2, filas: gruposMock }),
        });
        return;
      }

      if (url.includes('/1')) {
        if (method === 'GET') {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              id: 1,
              codigo: 'COMERCIAL',
              nombre: 'Comercial',
              descripcion: 'Ventas y Comercial',
              activo: true,
              rowVersion: 'AAAA',
              miembros: 3,
              permisos: ['ventas.pedido.crear', 'ventas.pedido.firmar_comercial'],
            }),
          });
          return;
        }

        if (method === 'PUT') {
          grupoEditado = JSON.parse(route.request().postData() || '{}');
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              id: 1,
              codigo: 'COMERCIAL',
              nombre: grupoEditado.nombre,
              descripcion: grupoEditado.descripcion,
              activo: true,
              rowVersion: 'BBBB',
              miembros: 3,
              permisos: grupoEditado.permisos,
            }),
          });
          return;
        }
      }

      await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
    });

    await abrir(page, ANGULAR, '/plataforma/grupos/1');

    // El formulario abre con datos del grupo
    await expect(page.locator('#campo-codigo')).toHaveValue('COMERCIAL');
    await expect(page.locator('#campo-nombre')).toHaveValue('Comercial');

    // El dual-list muestra los dos paneles
    await expect(page.locator('[data-dual-panel="disponibles"]')).toBeVisible();
    await expect(page.locator('[data-dual-panel="asignados"]')).toBeVisible();

    // Quitar todos los permisos (» / «)
    await page.locator('[data-btn-quitar-todo]').click();
    await expect(page.locator('[data-conteo-asignados]')).toHaveText('0');

    // Asignar todo
    await page.locator('[data-btn-asignar-todo]').click();
    await expect(page.locator('[data-conteo-disponibles]')).toHaveText('0');

    // Guardar
    await page.locator('#btn-guardar-grupo').click();

    // Verificamos que se guardó exitosamente
    await expect(page.locator('.alert-success')).toContainText('Grupo guardado');
    expect(grupoEditado).not.toBeNull();
    expect(grupoEditado.permisos.length).toBeGreaterThan(0);
  });

  test('crear grupo nuevo copiando permisos de otro (copiarDe)', async ({ page }) => {
    let peticionCrear: any = null;

    await page.route(/\/api\/v1\/plataforma\/grupos/, async route => {
      const url = route.request().url();
      const method = route.request().method();

      if (url.includes('/conjunto')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ completo: true, total: 2, filas: gruposMock }),
        });
        return;
      }

      if (url.endsWith('/99') && method === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 99,
            codigo: 'PROD_SUPERVISOR',
            nombre: 'Producción · Supervisor',
            descripcion: 'Copia de Supervisor de turno',
            activo: true,
            rowVersion: 'CCCC',
            miembros: 0,
            permisos: ['ventas.pedido.crear'],
          }),
        });
        return;
      }

      if (method === 'POST') {
        peticionCrear = JSON.parse(route.request().postData() || '{}');
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 99,
            codigo: peticionCrear.codigo,
            nombre: peticionCrear.nombre,
            descripcion: peticionCrear.descripcion,
            activo: true,
            rowVersion: 'CCCC',
            miembros: 0,
            permisos: ['ventas.pedido.crear'],
          }),
        });
        return;
      }

      await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
    });

    await abrir(page, ANGULAR, '/plataforma/grupos/nuevo');

    await page.locator('#campo-codigo').fill('PROD_SUPERVISOR');
    await page.locator('#campo-nombre').fill('Producción · Supervisor');
    await page.locator('#campo-descripcion').fill('Copia de Supervisor de turno');
    await page.locator('#campo-copiar-de').selectOption({ label: 'Supervisor de turno (SUPERVISOR)' });

    await page.locator('#btn-guardar-grupo').click();

    // Redirige a /plataforma/grupos/99
    await expect(page).toHaveURL(/.*\/plataforma\/grupos\/99/);
    expect(peticionCrear).not.toBeNull();
    expect(peticionCrear.copiarDe).toBe(2);
  });

  test('crear usuario con asignaciones y agente CONTPAQi', async ({ page }) => {
    let peticionCrearUsuario: any = null;
    let agenteLigado: any = null;

    await page.route(/\/api\/v1\/plataforma\/grupos/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ completo: true, total: 2, filas: gruposMock }),
      });
    });

    await page.route(/\/api\/v1\/plataforma\/usuarios/, async route => {
      const url = route.request().url();
      const method = route.request().method();

      if (url.includes('/55/agente') && method === 'PUT') {
        agenteLigado = JSON.parse(route.request().postData() || '{}');
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 55,
            usuario: 'atrevino',
            nombre: 'Ana Treviño',
            email: 'ana@polyconecta.com',
            activo: true,
            agenteId: agenteLigado.agenteId,
            rowVersion: 'EEEE',
            asignaciones: [],
          }),
        });
        return;
      }

      if (url.endsWith('/55') && method === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 55,
            usuario: 'atrevino',
            nombre: 'Ana Treviño',
            email: 'ana@polyconecta.com',
            activo: true,
            agenteId: 10,
            rowVersion: 'EEEE',
            asignaciones: [{ grupoId: 1, plantaId: 1, suplente: true }],
          }),
        });
        return;
      }

      if (method === 'POST') {
        peticionCrearUsuario = JSON.parse(route.request().postData() || '{}');
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 55,
            usuario: peticionCrearUsuario.usuario,
            nombre: peticionCrearUsuario.nombre,
            email: peticionCrearUsuario.email,
            activo: true,
            agenteId: null,
            rowVersion: 'DDDD',
            asignaciones: peticionCrearUsuario.asignaciones,
          }),
        });
        return;
      }

      await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
    });

    await abrir(page, ANGULAR, '/plataforma/usuarios/nuevo');

    await page.locator('#campo-usuario').fill('atrevino');
    await page.locator('#campo-nombre').fill('Ana Treviño');
    await page.locator('#campo-email').fill('ana@polyconecta.com');
    await page.locator('#campo-contrasena').fill('Comercial2026');
    await page.locator('#campo-agente').selectOption({ label: 'AG-01 - Juan Agente' });

    // Marcar suplente en la asignación por defecto
    await page.locator('tbody input[type="checkbox"]').first().check();

    await page.locator('button:has-text("Guardar")').click();

    await expect(page).toHaveURL(/.*\/plataforma\/usuarios\/55/);
    expect(peticionCrearUsuario).not.toBeNull();
    expect(peticionCrearUsuario.usuario).toBe('atrevino');
    expect(peticionCrearUsuario.asignaciones[0].suplente).toBe(true);
    expect(agenteLigado?.agenteId).toBe(10);
  });

  test('usuario sin permisos de plataforma no ve las opciones de Configuración en el menú', async ({ page }) => {
    // Sesión de usuario sin permisos de plataforma (solo ventas)
    await page.route(/\/api\/v1\/plataforma\/sesion/, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          usuario: { id: 2, usuario: 'vendedor', nombre: 'Carlos Vendedor' },
          asignaciones: [{ grupo: 'VENTAS', nombreGrupo: 'Vendedores', planta: 'PIM', suplente: false }],
          permisos: ['ventas.pedido.leer'],
        }),
      });
    });

    await abrir(page, ANGULAR, '/');

    // La barra superior no debe tener menús de Configuración
    await expect(page.locator('.o_header_menu a', { hasText: 'Usuarios' })).not.toBeVisible();
    await expect(page.locator('.o_header_menu a', { hasText: 'Grupos' })).not.toBeVisible();
  });
});
