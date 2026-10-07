# Feature Specification: Contratos visuales y componentes (fase abierta)

**Feature Branch**: `011-contratos-visuales`

**Created**: 2026-10-07

**Status**: Definida en la sesión del 7-oct (D-134 a D-136). El tablero ya está publicado (E1); falta el resto.

**Fase del plan**: ninguna. Es una fase abierta, fuera del plan de trabajo, como la 001 (D-134, D-118). Va **antes del diseño de las pantallas de F1**; el backend de F1 no la espera.

**Input**:
- la réplica en Angular (`PolyConecta.Web`, spec 001 cerrada);
- el sistema de diseño de [05 §7](../../../docs/diseno/05-arquitectura-tecnica.md) (CT-24);
- los patrones de Odoo 19 (skills `odoo-design-system` y `odoo-model-philosophy`).

---

## Objetivo

Que cada pantalla se construya **componiendo piezas con contrato**, sobre librerías que ya traen los comportamientos esperados, y que la aplicación entera quede migrada a esas piezas antes de F1. Son cuatro resultados:

1. Ver todas las pantallas en el orden del flujo (tablero).
2. Saber qué es cada pieza: partes, entradas, salidas, estados y reglas (contratos).
3. Tener esas piezas construidas y probadas en una galería viva.
4. Migrar las 28 pantallas a esas piezas **sin cambiar su estructura**, con el acabado de botones e íconos refinado.

## Decisiones de la sesión del 7-oct

| Tema | Decisión | Registro |
| :--- | :--- | :--- |
| Librerías | **TanStack Table** (lógica de tablas), **Angular CDK** (drag & drop, overlay, diálogo, menú) y **Spartan `brain`** (primitivas sin estilo: autocompletar, pestañas, menús, diálogos). Todas MIT; el HTML y el estilo son nuestros, con las clases `o_*`. PrimeNG se descartó porque desde la versión 19 pide llave de licencia | D-135 |
| Íconos | **Lucide** (`@lucide/angular`), en lugar de Bootstrap Icons | D-135 |
| Paridad con el prototipo | La comparación de píxeles se **retira**. Los guiones de escenario se quedan y verifican textos y flujos | D-135 |
| Datos de las listas | Orden, filtro, agrupación y paginación **en el servidor** | D-135 |
| Kanban | Recibe la colección y las etapas. Soltar una tarjeta en otra etapa ejecuta la **transición con nombre**; si no procede, la tarjeta regresa con el motivo | D-135 |
| Modo libre | Estructura **completa** del documento ligado; maestro y líneas en un solo guardado; el chatter se ve desde el inicio y se activa al guardar | D-136 |
| Alcance | Se migran **las 28 pantallas** en esta spec | D-135 |
| Entrega | Documento `07-contratos-visuales.md` más una **galería viva** en `/catalogo` | — |
| Color de botones | **Variante A, "Índigo afinado"**, elegida entre tres en la página Botones del lienzo: primario índigo `#2E3889` y secundario blanco con borde neutro | 7-oct (E3) |

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

- **Contenido:** una ruta de `PolyConecta.Web` con cada componente en todos sus estados y datos de ejemplo, sin dependencias nuevas.
- **Pruebas:** cada comportamiento del contrato tiene su prueba unitaria y una captura en el tablero.

### E5 · Migración de las 28 pantallas

- Cada pantalla pasa a los componentes nuevos, con la misma estructura de regiones: panel, barra de acciones, etapas, hoja, pestañas, detalle y chatter.
- Los guiones de escenario (`npm run scenarios`) siguen en verde.
- Las hojas de "Nuevo" pasan a la estructura completa (D-136).

## Contratos por escribir

### Patrones de pantalla

- [ ] **Lista.** Panel de control con búsqueda, filtros, agrupación y favoritos, más la tabla y el paginador. Comportamientos (D-135):
  - agrupar por una o varias columnas, con subtotales por grupo, contraer y expandir;
  - ordenar por columna, ascendente o descendente;
  - filtrar por columna y desde la barra de búsqueda;
  - elegir las columnas visibles y su orden;
  - elegir cuántas filas se ven por página y navegar con el paginador;
  - seleccionar filas y aplicar acciones masivas (archivar, imprimir, exportar a Excel);
  - guardar la combinación de filtros, agrupación y columnas como favorito del usuario (`SavedSearch`);
  - todo en el servidor: la tabla manda página, orden, filtros y agrupación, y la API responde con filas, grupos y totales.
- [ ] **Kanban.** Recibe la colección, las etapas (estado, título, orden, plegada sí o no) y la plantilla de la tarjeta. Agrupa por etapa, con un contador por columna. Arrastrar ejecuta la transición validada (D-135); la carga es por etapa y desde el servidor.
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
- Las 28 pantallas usan los componentes nuevos; `npm run build`, `npm test` y `npm run scenarios` están en verde.
- El tablero está regenerado con las pantallas migradas e incluye la galería.
- La paleta elegida (E3) quedó como tokens y CT-24 apunta a `07-contratos-visuales.md`.
- La paridad de píxeles está retirada de `npm run parity` y de la CI, y `05 §7.5` lo dice.
- Los dos líderes revisaron el documento, la galería y el tablero.

## Preguntas abiertas

- **Contrato de consulta de listas en el servidor:** página, orden, filtros, agrupación y totales. Se define aquí como forma; la API lo implementa en F1 con su primera lista.
- **Acciones masivas por documento:** cuáles aplican a cada lista. Lo decide cada fase.

---

## Exploración y cambios

| Fecha | Cambio | Motivo | Impacto | Decisión |
| :--- | :--- | :--- | :--- | :--- |
| 2026-10-07 | La aplicación desplaza el contenido dentro de un contenedor, no la página, así que `fullPage` de Playwright no captura la pantalla completa. `npm run tablero` mide lo desplazable y agranda la ventana antes de capturar | Hallazgo al capturar | E1 | — |
| 2026-10-07 | En la cadena de OF, la raíz es bolseo (`BOL-2026-0001`), su hija impresión y la hija de esta extrusión. El tablero las muestra en ese orden de navegación, no en el orden físico de producción | Semilla de `core/seed/flujo.ts` | E1 | — |
| 2026-10-07 | Variante A de botones aplicada en `app.css` como variables. `npm run build`, `npm test` (76) y `npm run scenarios` (26) en verde: la estructura no cambió | Elección del usuario en el lienzo | E3 | — |
| 2026-10-07 | Se eligió PrimeNG y después se descartó: desde la versión 19 (incluida la 22, la de Angular 22) pide llave de licencia y solo es gratis para organizaciones con menos de 1 millón de dólares de ingresos, 5 desarrolladores y 10 empleados. Se usa el stack MIT | Licencia leída en el paquete `primeng@22.1.2` | Toda la spec | D-135 |
