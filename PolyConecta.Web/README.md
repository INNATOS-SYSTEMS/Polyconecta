# PolyConecta.Web

Réplica en Angular del prototipo Blazor `PolyConecta.Presentation` (spec 001). Tiene las mismas pantallas, datos semilla y reglas, más el botón "Nuevo" para crear documentos libres (D-59) y el chatter en vivo por el hub de `PolyConecta.Api` (D-58). No llama a la API REST: el estado vive en memoria del navegador y se pierde al recargar, igual que en Blazor.

## Requisitos

- Node 24.16.0 (`.nvmrc`; `nvm use` en esta carpeta).
- Dependencias: `npm ci`.
- Para las pruebas de navegador: `npx playwright install chromium`, el SDK de .NET de `global.json` y el runtime de ASP.NET Core 8, que usa el prototipo.
- Conexión a Google Fonts y jsDelivr: las dos aplicaciones cargan de ahí Inter, Bootstrap y Bootstrap Icons.

## Puertos

| Aplicación | Puerto |
| :--- | :--- |
| `PolyConecta.Web` (Angular) | 9000 |
| `PolyConecta.Api` (hub del chatter en `/hubs/chatter`) | 9020 |
| `PolyConecta.Contpaq` (bridge, con `--with-bridge`) | 9030 |
| `PolyConecta.Presentation` (prototipo Blazor): solo lo levantan las pruebas de paridad | 9010 |

Desde la raíz, `./run.sh` levanta la Web y la API, y `./run.sh --solo-web` solo la Web.

## Scripts

| Script | Qué hace |
| :--- | :--- |
| `npm start` | Servidor de desarrollo en `:9000` |
| `npm run build` | Compila |
| `npm test` | Pruebas unitarias (Vitest): estado portado, reglas del modo libre y componentes |
| `npm run parity` | Paridad visual: captura las 19 rutas en las dos aplicaciones y compara píxeles. Informe en `parity-report/index.html`. `-- --routes=/pedidos,/fabricacion` limita la corrida |
| `npm run scenarios` | Guiones de escenario: los mismos pasos en Blazor y en Angular, con el texto y la captura de cada punto de control comparados. Los guiones `soloAngular` prueban el modo libre |
| `npm run audit` | Auditor de primer nivel: en cada ruta pulsa cada botón o enlace en las dos aplicaciones y compara URL y texto. Informe en `auditoria-report/` |
| `npm run chatter` | Chatter en vivo: dos pestañas con la API corriendo (el mensaje llega en menos de 1 s) y sin conexión |
| `npm run tablero` | Tablero de flujo (spec 011): captura cada pantalla de `e2e/tablero/pantallas.ts` completa, a 1600 px, en `tablero-report/`. Solo levanta Angular |

`parity`, `scenarios` y `audit` levantan el prototipo y Angular si no están corriendo; `chatter` también levanta la API.

## Reglas de paridad

1. La referencia es el prototipo corriendo, no su código: si algo se ve o se comporta distinto, la réplica se corrige.
2. Chromium a 1600×900, sin animaciones, con las fuentes del CDN cargadas.
3. Umbral: 1 % de píxeles distintos por ruta y por punto de control. El umbral no se sube; una diferencia legítima se documenta en la sección "Exploración y cambios" de la spec.
4. "Nuevo" es la única diferencia permitida (D-59): se enmascara en las capturas y el auditor no lo pulsa.
5. Razor recorta los espacios alrededor de los bloques `@if`; en las plantillas de Angular esos bloques van en línea (`{{ folio }}@if (…) {<span>…</span>}`) para que el texto quede igual.
6. Un `computed` que devuelve el mismo objeto mutado lleva `{ equal: () => false }`; si no, no avisa a quien depende de él.

## Estructura

- `src/app/core/`: modelos, semilla y servicios de estado portados del prototipo (`state/`), y el modo libre (`state/libre/`).
- `src/app/shared/`: los componentes del prototipo (`odoo-*`, selectores de lotes, `poc-sales-order-form`), más `boton-nuevo` y `hoja-nueva`.
- `src/app/features/`: las páginas, por módulo.
- `e2e/`: `parity/`, `scenarios/`, `auditoria/`, `chatter/` y `tablero/`.
