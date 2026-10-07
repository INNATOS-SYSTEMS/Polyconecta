# Feature Specification: Contratos visuales (fase abierta)

**Feature Branch**: `011-contratos-visuales`

**Created**: 2026-10-07

**Status**: En curso. El tablero está publicado; los contratos se escriben uno por patrón.

**Fase del plan**: ninguna. Es una fase abierta, fuera del plan de trabajo, como la 001 (D-134, D-118). Va **antes del diseño de las pantallas de F1**; el backend de F1 no la espera.

**Input**: la réplica en Angular (`PolyConecta.Web`, spec 001 cerrada), el sistema de diseño de [05 §7](../../../docs/diseno/05-arquitectura-tecnica.md) (CT-24) y los patrones de Odoo 19 (skills `odoo-design-system` y `odoo-model-philosophy`).

---

## Objetivo

Que cada pantalla nueva se diseñe **componiendo piezas con contrato**, no inventando. Para eso hacen falta dos cosas:

1. **Ver todas las pantallas que existen, en el orden del flujo**, en un solo lugar.
2. **Saber qué es cada pieza**: sus partes, qué datos recibe, qué eventos emite, sus estados y lo que nunca hace.

## Entregables

### E1 · Tablero de flujo ✅ 7-oct

- Lienzo en claude.ai: **Tablero de pantallas PolyConecta** (https://claude.ai/artifact/Vt8o8ef4t9FuziZMKrvmyw). Privado hasta compartirlo desde su menú Share.
- Una portada y una franja por flujo: Inicio e inventario, Pedidos, Fabricación, Recolección, Calidad, Traslados, Recepción y Entregas. Cada franja muestra sus pantallas en orden, la acción que lleva a la siguiente y, al final, la variante "Nuevo" (modo libre).
- 28 pantallas capturadas de `PolyConecta.Web` con `npm run tablero`. La lista de flujos está en `PolyConecta.Web/e2e/tablero/pantallas.ts`: una pantalla nueva se agrega ahí, se recaptura y se sube al lienzo.

### E2 · Contratos visuales

Documento nuevo `docs/diseno/07-contratos-visuales.md`. Cada contrato tiene:

| Sección | Qué dice |
| :--- | :--- |
| Anatomía | Partes y su orden, con una captura |
| Entradas | Datos que recibe (tipo y si es obligatorio) |
| Salidas | Eventos que emite |
| Estados | Vacío, cargando, solo lectura, deshabilitado, error, documento libre sin origen, sin conexión |
| Reglas | El patrón de Odoo 19 que sigue (CT-24) y lo que **nunca** hace |
| Dónde se usa | Pantallas del tablero que lo usan |

**Patrones de pantalla** (componen a los componentes):

- [ ] Lista (panel de control, búsqueda, agrupación, paginador, selección)
- [ ] Formulario de documento (barra de acciones, etapas, hoja, chatter)
- [ ] Hoja de "Nuevo" (modo libre, Principio X)
- [ ] Pestañas de la hoja
- [ ] Tabla de líneas (captura, columnas, totales, acciones por línea, bloqueo al autorizar)
- [ ] Campos de la hoja en dos columnas (etiqueta, valor, editable, solo lectura)
- [ ] Avisos y bloqueos (hard-stop, existencia insuficiente, confirmaciones)
- [ ] Estado de sincronización con CONTPAQi (CT-15): nuevo, se diseña aquí para F1

**Componentes compartidos** (`PolyConecta.Web/src/app/shared/`):

- [ ] `odoo-topbar` y `main-layout`
- [ ] `odoo-breadcrumb`
- [ ] `odoo-search-panel` y `odoo-view-switcher`
- [ ] `odoo-pager`
- [ ] `odoo-smart-buttons`
- [ ] `odoo-status-pipeline`
- [ ] `odoo-line-capture`
- [ ] `odoo-chatter-drawer`
- [ ] `lot-picker-modal` y `lot-quantity-picker-modal`
- [ ] `boton-nuevo` y `hoja-nueva`
- [ ] `pagina-no-encontrada` y `pagina-pendiente`

## Criterios de cierre

- Cada patrón y cada componente de la lista tiene su contrato en `07-contratos-visuales.md`, con captura y la lista de pantallas que lo usan.
- El tablero tiene todas las rutas de `PolyConecta.Web`, regenerado con la versión de `main` al cerrar.
- CT-24 apunta a `07-contratos-visuales.md`.
- Los dos líderes revisaron el documento y el tablero.

---

## Exploración y cambios

| Fecha | Cambio | Motivo | Impacto | Decisión |
| :--- | :--- | :--- | :--- | :--- |
| 2026-10-07 | La aplicación desplaza el contenido dentro de un contenedor, no la página, así que `fullPage` de Playwright no captura la pantalla completa. `npm run tablero` mide lo desplazable y agranda la ventana antes de capturar | Hallazgo al capturar | E1 | — |
| 2026-10-07 | En la cadena de OF, la raíz es bolseo (`BOL-2026-0001`), su hija impresión y la hija de esta extrusión. El tablero las muestra en ese orden de navegación, no en el orden físico de producción | Semilla de `core/seed/flujo.ts` | E1 | — |
