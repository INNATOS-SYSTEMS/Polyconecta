# PolyConecta.Web

Interfaz de PolyConecta en Angular. Desde F1 (spec 003), Pedidos, Usuarios, Grupos, Productos, Clientes y Sincronización leen y escriben por la API de `PolyConecta.Api`, con sesión por cookie. El resto de las pantallas sigue siendo la réplica del prototipo Blazor `PolyConecta.Presentation` (spec 001): mismas pantallas, datos semilla y reglas, más el botón "Nuevo" para crear documentos libres (D-59), con el estado en memoria del navegador. Cómo se conectan las dos partes está en `docs/diseno/05-arquitectura-tecnica.md` §7.2.

## Requisitos

- Node 24.16.0 (`.nvmrc`; `nvm use` en esta carpeta).
- Dependencias: `npm ci`.
- Para las pruebas de navegador: `npx playwright install chromium`, el SDK de .NET de `global.json` y el runtime de ASP.NET Core 8, que usa el prototipo.
- Conexión a Google Fonts y jsDelivr: las dos aplicaciones cargan de ahí Inter y Bootstrap (el prototipo también Bootstrap Icons; la réplica usa Lucide, empaquetado).

## Puertos

| Aplicación | Puerto |
| :--- | :--- |
| `PolyConecta.Web` (Angular) | 9000 |
| `PolyConecta.Api` (API y hub del chatter en `/hubs/chatter`) | 9020 |
| `PolyConecta.Contpaq` (bridge, con `--with-bridge`) | 9030 |
| `PolyConecta.Presentation` (prototipo Blazor): solo lo levantan las pruebas de paridad | 9010 |

Desde la raíz, `./run.sh` levanta la Web y la API, y `./run.sh --solo-web` solo la Web.

**Proxy de desarrollo.** `npm start` reenvía `/api` y `/hubs` (con WebSocket) a `:9020` (`proxy.conf.json`): la web llama a la API en su mismo origen y la cookie de sesión viaja sola. Sin la API, las pantallas que la usan llevan a `/login`; las que siguen en memoria funcionan igual.

**Sesión.** Se entra en `/login`. `run.sh` genera en `.env.local` la contraseña del Administrador (`LOCAL_ADMIN_PASSWORD`, usuario `admin`) y la de los usuarios de ejemplo de R1 (`LOCAL_R1_PASSWORD`: `ac1`, `comercial1`, `cobranza1`, `cobranza-suplente`, `doble`, `planner-pim` y `sistemas`), que la API crea en desarrollo.

## Scripts

| Script | Qué hace |
| :--- | :--- |
| `npm start` | Servidor de desarrollo en `:9000` |
| `npm run build` | Compila |
| `npm test` | Pruebas unitarias (Vitest): estado portado, reglas del modo libre, componentes, cliente de la API, listas HTTP, favoritos y chatter |
| `npm run e2e` | Pruebas de F1 (`e2e/f1`). La mayoría simula la API con `page.route`; `pedido-real.spec.ts` y `chatter.spec.ts` van contra la API real con los usuarios de R1 y necesitan `./run.sh --with-bridge` con los catálogos sincronizados |
| `npm run verificar-build` | Después de `build`: falla si el CSS trae reglas de Tailwind o si la carga inicial pasa de 89 kB comprimidos |
| `npm run scenarios` | Guiones de escenario: los mismos pasos en Blazor y en Angular, con el texto de cada punto de control comparado. Los guiones `soloAngular` prueban el modo libre |
| `npm run audit` | Auditor de primer nivel: en cada ruta pulsa cada botón o enlace en las dos aplicaciones y compara URL y texto, y prueba la navegación (enlace directo, folio inexistente, atrás y adelante). Informe en `auditoria-report/`. `PARITY_ROUTES=/pedidos,/fabricacion` limita las rutas |
| `npm run chatter` | Chatter en vivo de las pantallas en memoria: dos pestañas con la API corriendo (el mensaje llega en menos de 1 s) y sin conexión. Entra como `ac1`, porque el hub exige sesión |
| `npm run tablero` | Tablero de flujo (spec 011): captura cada pantalla de `e2e/tablero/pantallas.ts` completa, a 1600 px, en `tablero-report/`. Solo levanta Angular |

`scenarios` y `audit` levantan el prototipo y Angular si no están corriendo. Desde F1 toda ruta exige sesión: `scenarios`, `audit`, `chatter`, `tablero` y la galería (`e2e/catalogo`) entran antes con un usuario de R1 (`E2E_USUARIO`, `planner-pim` por omisión) y necesitan la API arriba (`./run.sh`).

## Reglas de comparación con el prototipo

1. La referencia de comportamiento es el prototipo corriendo, no su código: si un texto, una URL o un flujo difieren, la aplicación se corrige.
2. Chromium a 1600×900, sin animaciones, con las fuentes del CDN cargadas.
3. Desde la spec 011 no se comparan píxeles (D-135). Las diferencias legítimas están en `docs/diseno/05-arquitectura-tecnica.md` §7.5.
4. "Nuevo" no existe en el prototipo (D-59): el auditor no lo pulsa y los guiones que lo usan son `soloAngular`.
5. Razor recorta los espacios alrededor de los bloques `@if`; en las plantillas de Angular esos bloques van en línea (`{{ folio }}@if (…) {<span>…</span>}`) para que el texto quede igual.
6. Un `computed` que devuelve el mismo objeto mutado lleva `{ equal: () => false }`; si no, no avisa a quien depende de él.

## Estructura

- `src/app/core/`: sesión y cliente de la API (`sesion/`), listas y sus orígenes en memoria y HTTP (`lista/`), chatter (`chatter/`), formatos (`format/`), y los modelos, semilla y servicios de estado portados del prototipo (`state/`, con el modo libre en `state/libre/`).
- `src/app/shared/`: los componentes del prototipo (`odoo-*`, selectores de lotes, `poc-sales-order-form`), más `boton-nuevo` y `hoja-nueva`.
- `src/app/features/`: las páginas, por módulo.
- `e2e/`: `f1/`, `scenarios/`, `auditoria/`, `chatter/`, `tablero/`, `catalogo/` y `soporte/` (apertura de las dos aplicaciones, rutas, simulación de las vistas de las listas y entrada con los usuarios de R1).
