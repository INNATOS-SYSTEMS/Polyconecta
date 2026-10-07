# Feature Specification: Contratos visuales y componentes (fase abierta)

**Feature Branch**: `011-contratos-visuales`

**Created**: 2026-10-07

**Status**: Aclarada, probada y planeada el 7-oct: [plan.md](plan.md), [tasks.md](tasks.md) (59 tareas, P1 a P8). El tablero ya está publicado (E1) y los botones aplicados (E3).

**Fase del plan**: ninguna. Es una fase abierta, fuera del plan de trabajo, como la 001 (D-134, D-118). Va **antes del diseño de las pantallas de F1**; el backend de F1 no la espera.

**Líder**: L2 · Luis Alvarado Martinez (camino 2, dueño de `PolyConecta.Web`).

**Entregas por partes** (cada una se integra a `main` por su propio PR, CT-44):

| Parte | Contenido | Para cuándo |
| :--- | :--- | :--- |
| P1 | Componentes, galería `/catalogo`, contratos y el flujo de **Pedidos** migrado | Antes del 12-oct, inicio de F1 |
| P2 a P8 | Un flujo por parte: Fabricación, Recolección, Calidad, Traslados, Recepción, Entregas e Inicio e inventario | En paralelo a F1, cada uno antes de la fase que lo usa |

**Input**:
- la réplica en Angular (`PolyConecta.Web`, spec 001 cerrada);
- el sistema de diseño de [05 §7](../../../docs/diseno/05-arquitectura-tecnica.md) (CT-24);
- los patrones de Odoo 19 (skills `odoo-design-system` y `odoo-model-philosophy`).

---

## Objetivo

Que cada pantalla se construya **componiendo piezas con contrato**, sobre librerías que ya traen los comportamientos esperados, y que la aplicación entera quede migrada a esas piezas, flujo por flujo, antes de la fase que usa cada uno. Son cuatro resultados:

1. Ver todas las pantallas en el orden del flujo (tablero).
2. Saber qué es cada pieza: partes, entradas, salidas, estados y reglas (contratos).
3. Tener esas piezas construidas y probadas en una galería viva.
4. Migrar las 28 pantallas a esas piezas **sin cambiar su estructura**, con el acabado de botones e íconos refinado.

## Decisiones de la sesión del 7-oct

| Tema | Decisión | Registro |
| :--- | :--- | :--- |
| Librerías | **TanStack Table** (lógica de tablas), **Angular CDK** (drag & drop, overlay, diálogo, menú) y **Spartan `brain`** (primitivas sin estilo: autocompletar, pestañas, menús, diálogos). Todas MIT; el HTML y el estilo son nuestros, con las clases `o_*`. PrimeNG se descartó porque desde la versión 19 pide llave de licencia | D-135 |
| Íconos | **Lucide** (`@lucide/angular`), en lugar de Bootstrap Icons | D-135 |
| Exportar a Excel | **`write-excel-file`** (MIT), en el navegador | Clarificación del 7-oct |
| Paridad con el prototipo | La comparación de píxeles se **retira**: se quitan `npm run parity` y su informe. Los guiones de escenario (`npm run scenarios`) y el auditor (`npm run audit`) se quedan y siguen levantando el prototipo, porque comparan textos, URL y flujos | D-135 |
| Datos de las listas | Orden, filtro, agrupación y paginación **en el servidor** | D-135 |
| Kanban | Recibe la colección y las etapas. Soltar una tarjeta en otra etapa ejecuta la **transición con nombre**; si no procede, la tarjeta regresa con el motivo | D-135 |
| Modo libre | Estructura **completa** del documento ligado; maestro y líneas en un solo guardado; el chatter se ve desde el inicio y se activa al guardar | D-136 |
| Alcance | Se migran **las 28 pantallas** en esta spec | D-135 |
| Entrega | Documento `07-contratos-visuales.md` más una **galería viva** en `/catalogo` | — |
| Color de botones | **Variante A, "Índigo afinado"**, elegida entre tres en la página Botones del lienzo: primario índigo `#2E3889` y secundario blanco con borde neutro | 7-oct (E3) |

## Clarifications

### Session 2026-10-07

- Q: Mientras no exista la API, ¿de dónde sacan los datos las listas migradas? → A: De una interfaz de origen de datos. La tabla trabaja en modo servidor desde ya; ahora la resuelve un origen en memoria sobre la semilla y en F1 uno por HTTP, sin cambiar la pantalla.
- Q: ¿Qué pantallas llevan kanban en esta spec? → A: Todas las listas de documentos. Corregido al planear: son 8 (Pedidos y Fabricación, que ya lo tienen, más Incidencias, Recolecciones, Calidad, Traslados, Recepción y Entregas). La pregunta decía, por error, que Captura masiva e Incidencias ya tenían kanban; Captura masiva es una pantalla de captura, no una lista de documentos, y queda fuera.
- Q: Sin usuarios hasta F1, ¿dónde se guardan las vistas favoritas? → A: En el navegador por ahora, con la forma de `SavedSearch`; en F1 pasan a la base por usuario detrás de la misma interfaz y lo guardado antes no se migra.
- Q: ¿Cómo se exporta a Excel desde una lista? → A: Con `write-excel-file` (MIT), en el navegador, con lo que devuelve la consulta del origen de datos: las filas seleccionadas o, sin selección, todas las del filtro actual. SheetJS (`xlsx`) se descartó por dos vulnerabilidades conocidas en su versión de npm.
- Q: ¿Quién dirige la spec y cómo se entrega? → A: L2, Luis Alvarado Martinez, por partes. Antes del 12-oct: componentes, galería y el flujo de Pedidos. Los otros 7 flujos se migran e integran uno por uno, en paralelo a F1 y antes de la fase que los usa.

## Entregables

### E1 · Tablero de flujo ✅ 7-oct

- **Lienzo:** **Tablero de pantallas PolyConecta** (https://claude.ai/artifact/Vt8o8ef4t9FuziZMKrvmyw), privado hasta compartirlo desde su menú Share.
- **Contenido:** una franja por flujo con sus pantallas en orden y, al final, la variante "Nuevo". Son 28 pantallas.
- **Cómo se regenera:** con `npm run tablero`. La lista está en `PolyConecta.Web/e2e/tablero/pantallas.ts`.
- **Cuándo se actualiza:** al cerrar, con las pantallas migradas y la galería.

### E2 · Contratos visuales

En el documento nuevo `docs/diseno/07-contratos-visuales.md`, cada contrato lleva estas secciones:

| Sección | Qué dice |
| :--- | :--- |
| Anatomía | Partes y su orden, con una captura de la galería |
| Entradas | Datos que recibe: tipo y si es obligatorio |
| Salidas | Eventos que emite |
| Estados | Vacío, cargando, solo lectura, deshabilitado, error, documento libre sin origen y sin conexión, según aplique |
| Comportamientos | Lo que hace, con su criterio de aceptación |
| Reglas | El patrón de Odoo 19 que sigue (CT-24) y lo que **nunca** hace |
| Implementación | Librería y pieza que lo sostiene |
| Dónde se usa | Pantallas del tablero que lo usan |

### E3 · Botones e íconos

Hay que refinar el acabado **sin cambiar la estructura** de ninguna barra ni hoja.
1. **Color y contraste:** el primario y el secundario se distinguen a simple vista, y el texto cumple 4.5:1.
2. **Estados:** reposo, hover, foco visible, presionado, deshabilitado y cargando, iguales en todos los botones.
3. **Íconos:** Lucide con un solo grosor de trazo y tamaño por contexto (barra, línea, botón inteligente), alineados con el texto.
4. **Propuesta:** tres variantes en la página Botones del lienzo. Se eligió la **A, "Índigo afinado"**, el 7-oct. Sus colores ya son variables de `src/styles/app.css`:

   | Variable | Valor | Uso |
   | :--- | :--- | :--- |
   | `--brand-primary` | `#2E3889` | Primario |
   | `--brand-primary-hover` | `#232C6B` | Primario, hover |
   | `--brand-primary-active` | `#1B2254` | Primario, presionado |
   | `--btn-secondary-bg`, `-border`, `-color` | `#FFFFFF`, `#C5CAD6`, `#374151` | Secundario |
   | `--btn-secondary-hover-bg`, `-hover-border` | `#F3F4F6`, `#AEB4C2` | Secundario, hover |
   | `--btn-secondary-active-bg` | `#E5E7EB` | Secundario, presionado |
   | `--btn-icon-hover-bg` | `#EEF0FA` | Botón de ícono, hover (se aplica al migrar a Lucide) |
   | `--focus-ring` | `#6F78C2` | Anillo de foco con teclado: 2 px separado por 2 px de blanco |
   | `--btn-disabled-opacity` | `0.45` | Deshabilitado |

   Ya se aplicaron a `btn-primary`, `btn-outline-secondary` y al foco de todos los botones, sin cambiar la estructura. Los íconos Lucide y el botón de ícono llegan con la migración de cada pantalla (E5).

### E4 · Galería viva (`/catalogo`)

- **Contenido:** una ruta de `PolyConecta.Web` con cada componente en todos sus estados y datos de ejemplo. Es parte de la aplicación: no usa Storybook ni otra herramienta de catálogo.
- **Pruebas:** cada comportamiento del contrato tiene su prueba unitaria y una captura en el tablero.

### E5 · Migración de las 28 pantallas

- Cada pantalla pasa a los componentes nuevos, con la misma estructura de regiones: panel, barra de acciones, etapas, hoja, pestañas, detalle y chatter.
- Los guiones de escenario (`npm run scenarios`) siguen en verde.
- Las hojas de "Nuevo" pasan a la estructura completa (D-136).

## Prueba técnica (7-oct)

Se corrió en una copia de `PolyConecta.Web` y pasaron **10 de 10 pruebas de Playwright**. Las 76 pruebas unitarias de la aplicación siguieron en verde con las librerías instaladas. El código está en [`prueba-tecnica/`](prueba-tecnica/README.md).

**Versiones verificadas** (exactas, CT-36; `npm audit` sin vulnerabilidades):

| Paquete | Versión | Licencia |
| :--- | :--- | :--- |
| `@tanstack/angular-table` | 9.2.6 | MIT |
| `@angular/cdk` | 22.2.2 | MIT |
| `@spartan-ng/brain` | 1.6.1 | MIT |
| `@lucide/angular` | 1.52.0 | ISC |
| `write-excel-file` | 4.1.1 | MIT |

**Qué quedó probado:**
- **Tabla:** paginación y filas por página; orden de las 57 filas en el origen, no solo de la página; filtro; agrupación con conteo y total por grupo, y grupos que se abren bajo pedido; ocultar y reordenar columnas; exportar a `.xlsx` las seleccionadas o todo el filtro, con las columnas visibles.
- **Kanban:** la transición válida mueve la tarjeta; la inválida la regresa con el motivo; la que pide datos abre su diálogo, y cancelar la regresa.
- **Combobox:** filtra al escribir, se maneja con teclado y muestra "Sin resultados".
- **Calendario:** está en español, navega por mes y elige una fecha.

**Hallazgos que el plan debe recoger:**
1. **TanStack 9 cambió su API** respecto de la 8: `injectTable`, `tableFeatures` y `createColumnHelper<features, T>`. Los ejemplos de internet suelen ser de la 8. Usar las guías que trae el paquete (`node_modules/@tanstack/*/skills/`).
2. **Modo servidor:** además de `manualSorting` y `manualPagination` hacen falta `autoResetPageIndex: false` y `autoResetExpanded: false`. Si no, TanStack regresa a la página 1 al cambiar los datos y cierra los grupos.
3. **Orden como Odoo:** `sortDescFirst: false`. Si no, las columnas numéricas empiezan de mayor a menor.
4. **Agrupar en el servidor:** la agrupación de TanStack es del navegador. Los grupos del origen se pintan como filas sintéticas con `getSubRows` y expansión; abrir un grupo pide sus filas con el filtro del grupo.
5. **Spartan `brain` no trae estilos ni textos:**
   - las opciones filtradas (`data-hidden`) y el aviso "Sin resultados" (`data-empty`) se ocultan con nuestro CSS;
   - el encabezado del calendario lo pintamos nosotros;
   - los textos de accesibilidad vienen en inglés ("Go to the next month") si no se dan en `provideBrnCalendarI18n`.
6. **`BrnComboboxImports` no sirve tal cual:** trae `BrnCombobox` y `BrnComboboxMultiple` con el mismo selector. Se importan directiva por directiva.
7. **Dependencias de Spartan:** npm instala Tailwind 4, `tw-animate-css` y `clsx` porque Spartan las declara obligatorias. No entran al build (cero reglas de Tailwind en el CSS), pero quedan en `node_modules` y en `package-lock.json`.
8. **Tamaño:** la página con todas las librerías pesa 87 kB comprimida y se carga solo al abrirla; la carga inicial creció 7 kB comprimidos. `write-excel-file` se importa dinámicamente al exportar, para no cargarlo con la lista.
9. **Exportar:** se usa `write-excel-file/browser` (`toFile` o `toBlob`). El archivo resultante es un `.xlsx` válido.

## Contratos por escribir

### Patrones de pantalla

- [ ] **Lista.** Panel de control con búsqueda, filtros, agrupación y favoritos, más la tabla y el paginador. Comportamientos (D-135):
  - agrupar por una o varias columnas, con subtotales por grupo, contraer y expandir;
  - ordenar por columna, ascendente o descendente;
  - filtrar por columna y desde la barra de búsqueda;
  - elegir las columnas visibles y su orden;
  - elegir cuántas filas se ven por página y navegar con el paginador;
  - seleccionar filas y aplicar acciones masivas (archivar, imprimir, exportar a Excel);
  - exportar a Excel genera un `.xlsx` en el navegador con `write-excel-file`: las filas seleccionadas o, sin selección, todas las del filtro actual, con las columnas visibles en su orden y los números como números;
  - guardar la combinación de filtros, agrupación y columnas como favorito (`SavedSearch`), detrás de una interfaz de almacén de favoritos. En esta spec el almacén es el navegador; en F1, la base por usuario. Lo guardado en el navegador no se migra; si el navegador no permite guardar, la lista funciona igual y "Guardar favorito" avisa que no se pudo;
  - todo en el servidor: la tabla trabaja en modo manual de TanStack y pide cada vista a un **origen de datos de lista** (`OrigenDeLista<T>`). La consulta lleva página, filas por página, orden, filtros y agrupación; la respuesta trae filas, grupos con sus totales y el total de registros;
  - en esta spec el origen es **en memoria** sobre la semilla actual; en F1 se agrega el origen **HTTP** con la misma interfaz, y la pantalla no cambia.
- [ ] **Kanban.** Recibe la colección, las etapas (estado, título, orden, plegada sí o no) y la plantilla de la tarjeta. Agrupa por etapa, con un contador por columna. Arrastrar ejecuta la transición validada (D-135); la carga es por etapa, desde el mismo origen de datos que la lista.
  - Va en **8 listas**: Pedidos y Fabricación (ya lo tienen) e Incidencias, Recolecciones, Calidad, Traslados, Recepción y Entregas (nuevo). Captura masiva no lleva kanban.
  - Las incidencias no tienen estado: su kanban agrupa por centro de trabajo y no permite arrastrar.
  - Cada documento declara sus etapas (sus estados, en orden) y qué transición corresponde a cada movimiento entre etapas. Un movimiento sin transición, o hacia atrás, regresa la tarjeta con el motivo.
  - Si la transición pide datos (por ejemplo, la firma al autorizar un pedido o el resultado de un control de calidad), soltar abre su diálogo; la tarjeta se mueve solo si se confirma.
- [ ] **Formulario de documento.** Panel con migas y botones inteligentes, barra de acciones, etapas, hoja (título, folio, maestro en dos columnas, pestañas y detalle) y chatter.
- [ ] **"Nuevo" (modo libre).** El formulario anterior completo, en Borrador y sin origen; un solo guardado; el chatter se activa al guardar (D-136).
- [ ] **Pestañas de la hoja.**
- [ ] **Detalle y su captura.**
  - Captura `[Clave / Producto] [Cantidad] [Unidad] [Agregar]`, con columnas, totales y acciones por línea.
  - Bloqueo de líneas al autorizar.
  - Unidad base no editable (D-127).
- [ ] **Campos del maestro.** Etiqueta, valor, editable, solo lectura y obligatorio.
- [ ] **Avisos y bloqueos.** Hard-stop, existencia insuficiente, confirmaciones y errores.
- [ ] **Estado de sincronización con CONTPAQi (CT-15).** Es nuevo y se diseña aquí para F1. Muestra `No aplica`, `Pendiente`, `Enviado`, `Confirmado` y `Error`, con el folio de CONTPAQi o el error, y el reintento para Sistemas (D-93).

### Componentes

| Componente | Implementación |
| :--- | :--- |
| Tabla de lista | TanStack Table |
| Kanban | Angular CDK (`DragDrop`) |
| Barra superior y layout (`odoo-topbar`, `main-layout`) | Propio |
| Migas (`odoo-breadcrumb`) | Propio |
| Búsqueda y cambio de vista (`odoo-search-panel`, `odoo-view-switcher`) | Propio, con el overlay del CDK |
| Paginador (`odoo-pager`) | Propio, sobre el estado de TanStack |
| Botones inteligentes (`odoo-smart-buttons`) | Propio |
| Etapas (`odoo-status-pipeline`) | Propio |
| Captura de líneas (`odoo-line-capture`) | Propio, con el autocompletar de Spartan |
| Chatter (`odoo-chatter-drawer`) | Propio |
| Selección de registro (many2one: cliente, producto, almacén, con "Buscar más…") | Spartan `brain` (combobox) |
| Fecha | Spartan `brain`, formato es-MX |
| Número, moneda y cantidad con unidad | Propio, con `Intl.NumberFormat` es-MX |
| Diálogo, confirmación y aviso flotante | CDK `Dialog` y Spartan |
| Pestañas | Spartan `brain` |
| Menú y botón con acciones ("⚙ Acciones") | CDK `Menu` |
| Selección de lotes (`lot-picker-modal`, `lot-quantity-picker-modal`) | Diálogo del CDK con la tabla |
| Botón "Nuevo" y hoja nueva (`boton-nuevo`, `hoja-nueva`) | Propio (D-136) |
| Páginas vacías (`pagina-no-encontrada`, `pagina-pendiente`) | Propio |
| Botones (primario, secundario, de ícono) | Propio, con los tokens de E3 |

## Criterios de cierre

- Cada patrón y cada componente tiene su contrato en `07-contratos-visuales.md`, su entrada en `/catalogo` y sus pruebas.
- Cada parte (P1 a P8) se cierra cuando sus pantallas usan los componentes nuevos y `npm run build`, `npm test`, `npm run scenarios` y `npm run audit` están en verde; la spec se cierra con la P8.
- Las 28 pantallas usan los componentes nuevos.
- El tablero está regenerado con las pantallas migradas e incluye la galería.
- La paleta elegida (E3) quedó como tokens y CT-24 apunta a `07-contratos-visuales.md`.
- La paridad de píxeles está retirada de `npm run parity` y de la CI, y `05 §7.5` lo dice.
- Los dos líderes revisaron el documento, la galería y el tablero.

## Preguntas abiertas

- **Contrato HTTP de la consulta de listas:** la forma de `OrigenDeLista<T>` se fija en esta spec; la ruta y el formato en la API se definen en F1 con su primera lista.
- **Acciones masivas por documento:** cuáles aplican a cada lista. Lo decide cada fase.

---

## Exploración y cambios

| Fecha | Cambio | Motivo | Impacto | Decisión |
| :--- | :--- | :--- | :--- | :--- |
| 2026-10-07 | La aplicación desplaza el contenido dentro de un contenedor, no la página, así que `fullPage` de Playwright no captura la pantalla completa. `npm run tablero` mide lo desplazable y agranda la ventana antes de capturar | Hallazgo al capturar | E1 | — |
| 2026-10-07 | En la cadena de OF, la raíz es bolseo (`BOL-2026-0001`), su hija impresión y la hija de esta extrusión. El tablero las muestra en ese orden de navegación, no en el orden físico de producción | Semilla de `core/seed/flujo.ts` | E1 | — |
| 2026-10-07 | Los guiones de escenario también comparaban píxeles (1 % por punto de control), no solo `npm run parity`. Se quitó esa comparación de `e2e/scenarios/runner.ts` y se desinstalaron `pixelmatch` y `pngjs`. Carga inicial medida antes de la spec: 74.0 kB comprimidos; `verificar-build` usa 89 kB de límite | L2-T002 a L2-T004 | Pruebas | D-135 |
| 2026-10-07 | Revisión de coherencia (analyze): sin críticos. Se corrigieron 5 hallazgos: contratos y galería también para los componentes existentes y los patrones (L2-T025, L2-T027), aviso flotante (L2-T018), estado cargando de botones (L2-T011), E4 sin Storybook y conteo de componentes del plan | Revisión antes de implementar | Tareas de US1 | — |
| 2026-10-07 | Plan y tareas: research R-01 a R-10, tipos de lista, favoritos y kanban (data-model), contrato de 12 componentes, quickstart y 59 tareas en 9 historias (P1 = US1 + US2). La navegación pasa de `npm run parity` a `npm run audit`, y la verificación del build entra a la CI | Plan de la spec | Toda la spec | — |
| 2026-10-07 | La aclaración del kanban partía de un dato falso: solo Pedidos y Fabricación tienen kanban hoy (Captura masiva e Incidencias lo tienen apagado). "Todas las listas de documentos" son 8; Captura masiva queda fuera. Las incidencias no tienen estado, así que su kanban agrupa por centro de trabajo sin arrastre | Revisión del código al planear | Kanban, P2 | — |
| 2026-10-07 | Prueba técnica: 10/10 en Playwright. Hallazgos en "Prueba técnica": API nueva de TanStack 9, opciones del modo servidor, estilos y textos que Spartan deja a cargo de la aplicación, y dependencias de Tailwind que no entran al build | Verificar las librerías antes de planear | Plan de P1 | — |
| 2026-10-07 | Variante A de botones aplicada en `app.css` como variables. `npm run build`, `npm test` (76) y `npm run scenarios` (26) en verde: la estructura no cambió | Elección del usuario en el lienzo | E3 | — |
| 2026-10-07 | Se eligió PrimeNG y después se descartó: desde la versión 19 (incluida la 22, la de Angular 22) pide llave de licencia y solo es gratis para organizaciones con menos de 1 millón de dólares de ingresos, 5 desarrolladores y 10 empleados. Se usa el stack MIT | Licencia leída en el paquete `primeng@22.1.2` | Toda la spec | D-135 |
