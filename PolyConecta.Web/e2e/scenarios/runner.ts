import { Page, expect, test } from '@playwright/test';
import { ANGULAR, BLAZOR, abrir } from '../soporte/apps';
import { rutaAngular } from '../soporte/rutas';
import { catalogoSemilla } from '../../src/app/core/seed/inventario';

/**
 * Guiones de escenario (spec 001, FR-018): la misma lista de pasos se ejecuta contra Blazor y contra
 * Angular, y se comparan los textos visibles de cada punto de control. Un guion `soloAngular` (modo
 * libre, que Blazor no tiene) solo verifica sus puntos de control contra lo esperado. Desde la spec 011 ya
 * no se comparan píxeles (D-135): los componentes nuevos cambian el acabado, no los textos ni el flujo.
 */
/**
 * Un paso marcado `soloAngular` solo corre en Angular: es el clic de un diálogo que la réplica agregó
 * por decisión de la spec 011 (confirmar al fallar un lote, validar parcial, recibir), sin cambiar el
 * flujo ni los textos que se comparan.
 */
export type Paso = PasoBase & { soloAngular?: true };

type PasoBase =
  | { ir: string }
  /** Cambia de ruta sin recargar, para conservar el estado (Blazor lo pierde al abrir otro circuito). */
  | { navegar: string }
  | { pulsar: string; texto?: string }
  | { capturar: string; valor: string; enter?: boolean }
  | { elegirLote: string; lote: string }
  /** Elige una opción de un <select> por su valor. */
  | { elegir: string; valor: string }
  | { control: string; en: string; esperado?: string | RegExp }
  /** Punto de control sobre si un botón está habilitado (el texto no lo dice). */
  | { control: string; habilitado: string; texto?: string; esperado?: boolean }
  /** Punto de control sobre el valor de un campo y si se puede editar (innerText no los incluye). */
  | { control: string; campo: string; esperado?: string | RegExp; editable?: boolean }
  | { recargar: true };

export interface Guion {
  nombre: string;
  pasos: Paso[];
  soloAngular?: boolean;
}

const normalizar = (texto: string): string => texto.replace(/\s+/g, ' ').trim();

/**
 * Diferencias de texto decididas después de la réplica (D-141), que el corredor lleva a una forma común
 * antes de comparar las dos aplicaciones. Los `esperado` de cada paso se revisan contra el texto real.
 * - Producto: el prototipo muestra la clave y el nombre en columnas separadas, o solo uno de los dos; la
 *   réplica muestra "Clave - Nombre". Las dos formas quedan como «Clave».
 * - Botones inteligentes: nombre por tipo en singular o plural y orden por grupo. Cada botón queda como
 *   «tipo conteo», ordenados (ver `textoComparable`).
 * - Botón "Pedido" de la OF y demás documentos en memoria: desde F1 el pedido vive en la API y el botón
 *   queda deshabilitado con "Se conecta en F2" (research R-10 de la spec 003), sin conteo comparable.
 *   Las dos formas quedan como «pedido».
 */
const PRODUCTOS = catalogoSemilla().sort((a, b) => b.clave.length - a.clave.length || b.nombre.length - a.nombre.length);
const escapar = (t: string) => t.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
export function canonico(texto: string): string {
  let t = texto.replace(/\bClave Producto\b/g, 'Producto').replace(/\bLote Clave\b/g, 'Lote Producto');
  for (const p of PRODUCTOS) {
    const c = escapar(p.clave), n = escapar(p.nombre);
    t = t.replace(new RegExp(`(?:\\[${c}\\]|${c})(?: - | )${n}`, 'g'), `«${p.clave}»`);
  }
  for (const p of PRODUCTOS) {
    t = t.replace(new RegExp(`(?<=^|\\s)${escapar(p.clave)}(?=\\s|$)`, 'g'), `«${p.clave}»`);
    t = t.replace(new RegExp(`(?<=^|\\s)${escapar(p.nombre)}(?=\\s|$)`, 'g'), `«${p.clave}»`);
  }
  // Una línea con la clave de un producto y la descripción de otro queda «A» - «B» en la réplica.
  return t.replace(/» - «/g, '» «').replace(/«pedido \d+»/g, '«pedido»');
}

const TIPOS_DE_BOTON: Record<string, string> = {
  'Pedido': 'pedido', 'Pedidos': 'pedido', 'Pedido de Venta': 'pedido', 'Entrega': 'entrega', 'Entregas': 'entrega',
  'Fabricación': 'orden', 'Orden de Fabricación': 'orden', 'Orden de fabricación': 'orden', 'Órdenes de fabricación': 'orden',
  'Recolección': 'recoleccion', 'Recolecciones': 'recoleccion', 'Traslado': 'traslado', 'Traslados': 'traslado',
  'Recepción': 'recepcion', 'Recepciones': 'recepcion', 'Calidad': 'control', 'Control de calidad': 'control', 'Controles de calidad': 'control',
};

/**
 * Texto de un punto de control: el real y el comparable, con los botones inteligentes como «tipo conteo»
 * en orden alfabético. Cambia el DOM solo durante la lectura y lo deja como estaba.
 */
async function textoComparable(page: Page, selector: string): Promise<{ real: string; comparable: string }> {
  const el = page.locator(selector).first();
  const real = normalizar(await el.innerText());
  const conBotones = await el.evaluate((raiz, tipos) => {
    const cajas = new Set(Array.from(raiz.querySelectorAll('.o_smart_button')).map(b => b.parentElement!));
    const deshacer: (() => void)[] = [];
    for (const caja of cajas) {
      const botones = Array.from(caja.children).filter(b => b.classList.contains('o_smart_button')) as HTMLElement[];
      const marcas = botones.map(b => {
        const etiqueta = (b.querySelector('.stat-label, .o_stat_text')?.textContent ?? '').trim();
        const conteo = (b.querySelector('.stat-count, .o_stat_value')?.textContent ?? '').trim();
        return `«${tipos[etiqueta] ?? etiqueta} ${conteo}»`;
      });
      const marca = document.createElement('span');
      marca.textContent = marcas.sort().join(' ');
      botones.forEach(b => (b.style.display = 'none'));
      caja.appendChild(marca);
      deshacer.push(() => { marca.remove(); botones.forEach(b => (b.style.display = '')); });
    }
    const texto = (raiz as HTMLElement).innerText;
    deshacer.forEach(d => d());
    return texto;
  }, TIPOS_DE_BOTON);
  return { real, comparable: canonico(normalizar(conBotones)) };
}


interface Corrida {
  controles: Record<string, string>;
}

async function ejecutar(page: Page, base: string, pasos: Paso[]): Promise<Corrida> {
  const controles: Record<string, string> = {};
  for (const paso of pasos) {
    if (paso.soloAngular && base !== ANGULAR) continue;
    if ('ir' in paso) {
      await abrir(page, base, paso.ir);
    } else if ('navegar' in paso) {
      // La réplica lleva el prefijo de su módulo (D-155).
      const ruta = base === ANGULAR ? rutaAngular(paso.navegar) : paso.navegar;
      await page.evaluate(r => {
        const w = window as unknown as { Blazor?: { navigateTo(u: string): void }; __sinRecarga?: boolean };
        w.__sinRecarga = true;
        if (w.Blazor) w.Blazor.navigateTo(r);
        else {
          history.pushState({}, '', r);
          dispatchEvent(new PopStateEvent('popstate', { state: {} }));
        }
      }, ruta);
      await page.waitForURL(u => decodeURIComponent(u.pathname) === ruta);
      await page.waitForTimeout(400);
      const conservo = await page.evaluate(() => (window as unknown as { __sinRecarga?: boolean }).__sinRecarga === true);
      expect(conservo, `navegar a ${ruta} recargó la página y perdió el estado`).toBe(true);
    } else if ('pulsar' in paso) {
      const objetivo = paso.texto ? page.locator(paso.pulsar, { hasText: paso.texto }).first() : page.locator(paso.pulsar).first();
      await objetivo.click({ timeout: 5000 });
      await page.waitForTimeout(250);
    } else if ('capturar' in paso) {
      const campo = page.locator(paso.capturar).first();
      await campo.fill(paso.valor);
      await campo.dispatchEvent('change');
      if (paso.enter) await campo.press('Enter');
      await page.waitForTimeout(150);
    } else if ('elegir' in paso) {
      await page.locator(paso.elegir).first().selectOption(paso.valor);
      await page.waitForTimeout(150);
    } else if ('elegirLote' in paso) {
      const campo = page.locator(paso.elegirLote).first();
      await campo.fill(paso.lote);
      await campo.press('Enter');
      await page.waitForTimeout(150);
    } else if ('recargar' in paso) {
      await page.reload({ waitUntil: 'networkidle' });
      await page.waitForTimeout(500);
    } else if ('campo' in paso) {
      const campo = page.locator(paso.campo).first();
      const valor = await campo.inputValue();
      const editable = await campo.isEditable();
      controles[paso.control] = `${valor} (${editable ? 'editable' : 'no editable'})`;
      if (paso.esperado !== undefined) expect(valor, paso.control).toMatch(paso.esperado);
      if (paso.editable !== undefined) expect(editable, `${paso.control}: editable`).toBe(paso.editable);
    } else if ('habilitado' in paso) {
      const boton = paso.texto ? page.locator(paso.habilitado, { hasText: paso.texto }).first() : page.locator(paso.habilitado).first();
      const habilitado = await boton.isEnabled();
      controles[paso.control] = habilitado ? 'habilitado' : 'deshabilitado';
      if (paso.esperado !== undefined) expect(habilitado, paso.control).toBe(paso.esperado);
    } else {
      const { real, comparable } = await textoComparable(page, paso.en);
      controles[paso.control] = comparable;
      if (paso.esperado !== undefined) expect(real, paso.control).toMatch(paso.esperado);
    }
  }
  return { controles };
}

/** Registra el guion como prueba de Playwright. */
export function guion(g: Guion): void {
  test(`escenario: ${g.nombre}`, async ({ page }) => {
    // Blazor primero: si el guion falla ahí, el error es del guion; si falla solo en Angular, es una diferencia.
    const blazor = g.soloAngular ? undefined : await ejecutar(page, BLAZOR, g.pasos).catch(e => { throw new Error(`[Blazor] ${e.message}`); });
    const angular = await ejecutar(page, ANGULAR, g.pasos).catch(e => { throw new Error(`[Angular] ${e.message}`); });
    if (!blazor) return;
    for (const control of Object.keys(blazor.controles)) {
      expect(angular.controles[control], `punto de control "${control}"`).toBe(blazor.controles[control]);
    }
  });
}
