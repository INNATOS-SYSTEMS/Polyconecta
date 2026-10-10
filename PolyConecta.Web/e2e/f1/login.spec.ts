import { expect, test } from '@playwright/test';
import { ANGULAR, abrir } from '../soporte/apps';

/**
 * Inicio de sesión (D-166): pantalla completa en dos secciones, sin barra superior. El botón se habilita con
 * usuario y contraseña; un error de la API se muestra arriba de los campos. La API se simula.
 */
test.describe('Inicio de sesión (F1 / US2)', () => {
  test('el botón espera usuario y contraseña, y un rechazo se explica arriba de los campos', async ({ page }) => {
    await page.route(/\/api\/v1\/plataforma\/sesion$/, route =>
      route.request().method() === 'POST'
        ? route.fulfill({ status: 401, contentType: 'application/json', body: JSON.stringify({ code: 'CREDENCIALES_INVALIDAS' }) })
        : route.fulfill({ status: 401, contentType: 'application/json', body: '{}' }));
    await abrir(page, ANGULAR, '/login');

    await expect(page.locator('.o_main_navbar')).toHaveCount(0);
    await expect(page.locator('.o_login_arte img')).toBeVisible();

    const entrar = page.locator('[data-login-submit]');
    await expect(entrar).toBeDisabled();
    await page.locator('[data-login-usuario]').fill('ac1');
    await expect(entrar).toBeDisabled();
    await page.locator('[data-login-contrasena]').fill('equivocada');
    await expect(entrar).toBeEnabled();
    // El botón del ojo muestra y oculta la contraseña.
    await page.locator('[data-login-ver]').click();
    await expect(page.locator('[data-login-contrasena]')).toHaveAttribute('type', 'text');
    await page.locator('[data-login-ver]').click();
    await expect(page.locator('[data-login-contrasena]')).toHaveAttribute('type', 'password');
    await entrar.click();

    await expect(page.locator('[data-login-error]')).toHaveText('Usuario o contraseña incorrectos.');
    await expect(page).toHaveURL(/\/login/);
  });
});
