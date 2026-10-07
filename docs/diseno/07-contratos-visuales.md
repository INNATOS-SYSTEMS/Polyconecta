# Contratos visuales

Qué es cada pieza de la interfaz de `PolyConecta.Web`: sus partes, qué recibe, qué emite, sus estados y lo que nunca hace. Una pantalla nueva se diseña **componiendo estos contratos**, no inventando (D-134, CT-24). Cada contrato tiene su ejemplo vivo en la galería `/catalogo` y sus pruebas en `e2e/catalogo/` y junto a cada componente (`*.spec.ts`). Las capturas salen de la galería.

Sistema de diseño (estilo, estructura del documento y modo libre): [05 §7](05-arquitectura-tecnica.md#7-interfaz-polyconectaweb). Librerías: TanStack Table, Angular CDK y Spartan `brain`, todas sin estilo propio; el aspecto lo dan las clases `o_*` de `src/styles/app.css` (D-135).

---

## 1. Patrones de pantalla

### 1.1 Lista

![Lista](img/07/lista.png)

**Anatomía**:
- panel de control: "Nuevo", migas, barra de búsqueda con facetas y el menú de filtros, agrupaciones y favoritos;
- barra de selección, solo con filas marcadas;
- tabla con casillas, encabezados ordenables y el botón de columnas al final;
- filas de grupo, filas y pie con totales;
- paginador.

**Comportamientos**:

| Hace | Criterio |
| :--- | :--- |
| Ordenar | Clic en el encabezado: primero ascendente, luego descendente. Ordena **todo el filtro** en el origen, no la página |
| Filtrar y buscar | Texto libre sobre los campos de la vista de búsqueda; filtros con nombre del mismo campo con O y de campos distintos con Y. Cambiar el filtro regresa a la página 1 |
| Agrupar | Uno o varios niveles, desde el menú de búsqueda. Cada grupo muestra conteo y subtotales; se abre bajo pedido |
| Columnas | Mostrar u ocultar y subir o bajar entre las visibles |
| Paginar | 20, 40, 80 (por omisión) o 200 por página; el texto es "inicio-fin / total" |
| Seleccionar | Casillas, con "seleccionar todo" de la página. Aparece la barra con las acciones masivas y "Exportar" |
| Exportar | `.xlsx` con las seleccionadas o, sin selección, todo el filtro; columnas visibles en su orden, números como números y fechas `dd/mm/yyyy` |
| Favoritos | Guardar la búsqueda, la agrupación, el orden, las columnas y el tamaño de página con nombre; uno por omisión se aplica al abrir. Hasta F1 se guardan en el navegador |

**Reglas**:
- **Nunca** ordena, filtra, agrupa ni pagina en el navegador: todo lo pide al `OrigenDeLista` (data-model de la spec 011).
- Hasta F1 el origen es en memoria; en F1, HTTP, sin cambiar la pantalla.
- Clic en la fila abre el formulario; la casilla no.
- **Sin registros:** muestra "No hay registros que mostrar."

**Implementación**: `pc-odoo-list` (TanStack Table en modo servidor) con `pc-odoo-search-panel` y `pc-odoo-pager`.

### 1.2 Kanban

![Kanban](img/07/kanban.png)

**Anatomía**: una columna por etapa, con título, cuenta y tarjetas. Las etapas terminales empiezan plegadas; doble clic en el título pliega o despliega.

**Comportamientos**:

| Hace | Criterio |
| :--- | :--- |
| Mover con transición | Soltar una tarjeta en otra etapa ejecuta la transición con nombre, el mismo método que el botón del formulario |
| Rechazar | Sin transición declarada, o si no procede, la tarjeta regresa y la columna muestra el motivo |
| Pedir datos | Si la transición pide datos (firma, resultado de calidad), abre su diálogo; la tarjeta se mueve solo si se confirma |
| Filtrar | Usa la misma búsqueda y los mismos filtros que la lista |

**Reglas**:
- No hay transiciones que solo existan en el kanban.
- Las que avanza el sistema, como Autorizado → En progreso, no se arrastran.
- **Sin estado:** un documento sin estado (incidencias) agrupa por otro campo y no se arrastra.

**Implementación**: `pc-odoo-kanban` (CDK `DragDrop` y `Dialog`).

### 1.3 Formulario de documento

![Formulario](img/07/formulario.png)

**Anatomía, en este orden**:
1. panel de control: "Nuevo", migas y botones inteligentes;
2. barra de acciones: primario, secundarios, "Acciones" y estado de sincronización;
3. hoja: etapas arriba a la derecha, título y folio, maestro en dos columnas, pestañas y detalle con su captura;
4. chatter a la derecha.

**Reglas**:
- Los botones inteligentes sin origen se ven atenuados y no navegan (documento libre).
- La acción primaria es una sola.
- Las secundarias poco frecuentes van en "Acciones".

### 1.4 "Nuevo" (modo libre)

- El mismo formulario completo, en Borrador y sin origen (D-136): etapas, maestro, pestañas, detalle con captura y chatter.
- Maestro y líneas se guardan con un solo "Guardar".
- El chatter se ve desde el inicio y se activa al guardar.
- Las reglas del documento son las mismas con o sin origen (Principio X).

### 1.5 Pestañas, detalle y captura

- **Pestañas:** marcado `nav-tabs`; con teclado, flechas para moverse entre ellas.
- **Captura de líneas:** `[Clave / Producto] [Cantidad] [Unidad] [Agregar]`.
  - La unidad es la base del producto en CONTPAQi y no se edita (D-127).
  - Al autorizar, las líneas se bloquean.
  - Cada línea tiene sus acciones con botones de ícono (editar y quitar).
- **Implementación:** `pc-odoo-tabs` (Spartan `BrnTabs`) y `pc-odoo-line-capture`.

### 1.6 Campos del maestro

![Campos](img/07/campos.png)

| Campo | Componente | Reglas |
| :--- | :--- | :--- |
| Selección de registro (cliente, producto, almacén) | `pc-odoo-many2one` | Busca en el origen al escribir; hasta 8 opciones, "Sin resultados" y "Buscar más…", que abre la lista completa. Teclado: flechas y Enter. El foco no sale del campo |
| Fecha | `pc-odoo-date` | Calendario en español, semana desde el lunes, `min` y `max`. Muestra "7 oct 2026" |
| Número, moneda, porcentaje | `pc-odoo-number` | Muestra `5,500.0`; al enfocar, `5500`; al salir valida el mínimo y avisa. La cantidad lleva su unidad base, no editable |

Todos tienen solo lectura (gris, no editable) y funcionan con formularios de Angular (`ngModel` o formularios reactivos).

### 1.7 Avisos y bloqueos

![Avisos](img/07/avisos.png)

| Caso | Pieza | Regla |
| :--- | :--- | :--- |
| Confirmación | `pc-odoo-dialog` (o `OdooConfirmacion`) | Título, mensaje, primario y "Cancelar". Esc y clic fuera cancelan; el foco queda dentro |
| Hard-stop (Calidad, existencia) | `pc-odoo-dialog` | Explica por qué no procede y qué hacer; no ofrece continuar |
| Resultado de una acción | `AvisosService` | Éxito y aviso se cierran solos a los 4 s; el error se queda hasta cerrarlo |
| Error en la hoja | Alerta en la hoja | Junto al campo o arriba de la hoja, en rojo |

### 1.8 Estado de sincronización con CONTPAQi (CT-15)

![Sincronización](img/07/sincronizacion.png)

- **Dónde va:** en la barra de acciones del formulario.
- **Estados:** `No aplica`, `Pendiente`, `Enviado`, `Confirmado` (con el folio de CONTPAQi) y `Error` (con código y mensaje del contrato).
- **Reintentar:** aparece solo en `Error` y solo para Sistemas (D-93).
- **Uso:** F1 lo conecta; en la réplica ningún documento sincroniza.
- **Implementación:** `pc-odoo-sync-status`.

---

## 2. Piezas

### 2.1 Botones e íconos (variante A, "Índigo afinado")

![Botones](img/07/botones.png)

| Botón | Clases | Uso |
| :--- | :--- | :--- |
| Primario | `btn btn-primary` | Una acción principal por barra |
| Secundario | `btn btn-outline-secondary` | Cancelar, descartar y acciones secundarias |
| Ícono | `btn o_btn_icon` + `aria-label` | Acciones de línea y de tabla |
| Cargando | `o_btn_loading` + ícono `cargando` | Mientras la acción corre |

- **Estados:** reposo, hover, foco con teclado (anillo de 2 px separado por 2 px de blanco), presionado, deshabilitado (45 %) y cargando. Los colores son las variables de E3 en `app.css`; los componentes no escriben colores sueltos.
- **Íconos:** `pc-odoo-icon` con un nombre del catálogo (`confirmar`, `editar`, `hard-stop`…).
  - Tamaño por contexto: 16 px en botones, 18 px en botones de ícono y 15 px en botones inteligentes.
  - Lucide con trazo 2.
  - Acepta el nombre viejo de Bootstrap mientras dura la migración.

### 2.2 Barra superior, migas, vistas y paginador

![Navegación](img/07/navegacion.png)

| Pieza | Componente | Reglas |
| :--- | :--- | :--- |
| Barra superior | `pc-odoo-topbar` en `pc-main-layout` | Menú del módulo de la ruta actual |
| Migas | `pc-odoo-breadcrumb` | Nivel actual y uno atrás; lo anterior se resume en "…" |
| Cambio de vista | `pc-odoo-view-switcher` | Lista y kanban; sin kanban, solo lista |
| Paginador | `pc-odoo-pager` | Con `inicio` y `fin` pagina y cambia el tamaño; sin ellos, "1-N / N" como el prototipo |

### 2.3 Selección de lotes

- **`lot-picker-modal`:** elige lotes completos.
- **`lot-quantity-picker-modal`:** elige lote y cantidad.
- Solo ofrecen lotes que pueden moverse: liberados por Calidad (hard-stop).

### 2.4 Páginas vacías

![Páginas](img/07/paginas.png)

- **`pagina-pendiente`:** módulo en construcción.
- **`pagina-no-encontrada`:** ruta inexistente.

---

## 3. Dónde se usa cada patrón

| Patrón | Pantallas (tablero de flujo) |
| :--- | :--- |
| Lista y kanban | Pedidos, Fabricación, Incidencias, Recolecciones, Calidad, Traslados, Recepción y Entregas |
| Formulario | Pedido, OF (bolseo, impresión y extrusión), Recolección, Control de calidad, Traslado, Recepción y Entrega |
| "Nuevo" | Las mismas siete |
| Lista sin kanban | Inventario actual, Inventario para Ventas |
| Captura | Captura masiva |

La migración de cada pantalla a estos componentes se hace por flujo, en las partes P1 a P8 de la spec 011.
