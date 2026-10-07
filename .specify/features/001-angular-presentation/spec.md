# Feature Specification: Presentación en Angular

**Feature Branch**: `001-angular-presentation`

**Created**: 2026-09-28 · **Ratified**: 2026-09-29 · **Responsable**: Luis Alvarado Martinez (camino 2)

**Status**: Ratificada. Plan y tareas listos; la implementación espera el merge de la 002 a `main`.

**Input**: Replicar en Angular la capa de presentación `PolyConecta.Presentation` (Blazor Server) con paridad 1:1, habilitar el modo libre ("Nuevo") y llevar el chatter en tiempo real a `PolyConecta.Api`.

**Decisiones que la rigen** (`docs/diseno/decisiones.md`): D-48 (Angular, réplica primero), D-52 (documentos libres, Principio X), D-58 a D-61 (alcance de esta feature).

---

## Contexto para el agente

Lee esto antes de cualquier tarea.

- **Qué se replica.** `PolyConecta.Presentation` es un prototipo navegable en Blazor Server: 18 páginas, 13 componentes compartidos y todo su estado en memoria del servidor (`Services/OperationalFlowState.cs`, `StockOperationState.cs`, `InventoryState.cs`, `SearchViews.cs`, `UiViewState.cs`). **No llama a la API** y simula los folios de CONTPAQi. Esta feature produce la misma aplicación en Angular, con el estado en el navegador.
- **El prototipo Blazor es el oráculo.** Ante cualquier duda de comportamiento, texto, orden de columnas, estilo o dato, la respuesta es lo que hace `PolyConecta.Presentation` corriendo en `http://localhost:9000`. No se corrige, no se mejora y no se modifica (D-60).
- **Las reglas de negocio** están en `docs/diseno/`. Solo se consultan para el modo libre (US-3), que el prototipo no tiene.
- **Fuera del modo libre y del chatter, no se inventa nada.** Si algo no está en el prototipo ni en esta spec, se registra en `bloqueos.md` (ver "Reglas de autonomía") y no se implementa.

---

## Alcance

| Dentro | Fuera |
| :--- | :--- |
| Proyecto `PolyConecta.Web` con las 18 páginas y los 13 componentes, con paridad 1:1 | Conectar las pantallas a la API REST: es la siguiente feature (D-48) |
| Estado en el navegador, portado 1:1 desde los servicios C#, con los mismos datos semilla | Autenticación, roles y permisos (la réplica no tiene usuarios, igual que el prototipo) |
| Botón "Nuevo" habilitado con formularios de creación libre en 9 documentos (D-59) | Corregir diferencias del prototipo con el diseño: estados de OF de D-42, alias de ubicaciones de D-43, nombres de lote de D-54 en documentos ligados. Se atienden al conectar la API |
| `ChatterHub` en `PolyConecta.Api`, con CORS para Angular (D-58) | Persistir mensajes del chatter |
| Pruebas de paridad automatizadas contra el prototipo | Cambios en `PolyConecta.Presentation`, `Domain`, `Infrastructure` o `Contpaq` |
| Integración en `scripts/run.sh` | Borrar el prototipo Blazor: se conserva como referencia (D-60) |

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - La misma aplicación, ahora en Angular (Priority: P1)

Como usuario del prototipo, necesito abrir la versión en Angular y encontrar exactamente las mismas pantallas, datos y navegación que en la versión Blazor.

**Why this priority**: Sin paridad visual y de navegación no hay réplica; todo lo demás se apoya en ella.

**Independent Test**: Levantar ambas aplicaciones, recorrer las 19 rutas con el mismo navegador y tamaño de ventana, y comparar las capturas automáticamente.

**Acceptance Scenarios**:

1. **Given** las dos aplicaciones corriendo con sus datos semilla, **When** se abre cualquiera de las 19 rutas, **Then** la captura de Angular difiere de la de Blazor en **1 % de píxeles o menos**, sin contar el botón "Nuevo".
2. **Given** un enlace directo a un formulario (`/fabricacion/BOL-2026-0001`), **When** se abre en una pestaña nueva, **Then** carga ese documento sin pasar por la lista.
3. **Given** cualquier pantalla, **When** se usan "atrás" y "adelante" del navegador, **Then** la navegación es la misma que en Blazor.

---

### User Story 2 - El flujo operativo funciona igual (Priority: P1)

Como usuario, necesito recorrer el flujo completo (pedido, fabricación, recolección, calidad, logística) y que cada acción produzca los mismos cambios de estado que en el prototipo.

**Why this priority**: Es la mitad funcional de la paridad; una réplica que se ve igual pero calcula distinto no sirve de referencia.

**Independent Test**: Ejecutar el mismo guion de acciones contra las dos aplicaciones y comparar los textos visibles en cada punto de control.

**Acceptance Scenarios**:

1. **Given** el pedido `IV310-26` confirmado, **When** se pulsa "Autorizar" dos veces, **Then** el pedido pasa a Autorizado con las dos firmas y se generan las mismas OF que en Blazor.
2. **Given** una OF con componentes, **When** se planea, se valida su recolección con lotes, se capturan rollos, Calidad aprueba o rechaza y se cierra la producción, **Then** estados, saldos de WIP, nombres de lote (incluido `.S`) e inventario coinciden con Blazor en cada paso.
3. **Given** un lote en revisión de Calidad, **When** se intenta cerrar la OF, **Then** el cierre se bloquea igual que en Blazor (hard-stop).
4. **Given** un traslado, su recepción y una entrega, **When** se comprueba la disponibilidad y se valida cada uno, **Then** los estados y el "Contpaq ID" simulado aparecen igual que en Blazor.
5. **Given** cualquier lista, **When** se busca texto, se aplican filtros o se quita una faceta, **Then** el resultado es el mismo que en Blazor.
6. **Given** el estado modificado por el usuario, **When** se recarga la pestaña, **Then** se conserva igual que en Blazor dentro de la sesión (ver FR-011).

---

### User Story 3 - Crear documentos en modo libre (Priority: P2)

Como usuario, necesito crear cualquier documento operativo con "Nuevo", sin documento de origen, conforme al Principio X.

**Why this priority**: Es la única funcionalidad nueva de la réplica (D-59); depende de que el estado y los formularios ya existan.

**Independent Test**: Para cada uno de los 9 documentos, pulsar "Nuevo", capturar, guardar y verificar que aparece en su lista sin origen y que sus reglas aplican.

**Acceptance Scenarios**:

1. **Given** la lista de órdenes de fabricación, **When** se crea una OF con "Nuevo" sin pedido y se confirma, **Then** queda sin origen, genera su recolección y sus lotes se nombran con el folio de la OF raíz (`R001-BOL-2026-0007`).
2. **Given** una recepción creada con "Nuevo", **When** se eligen lotes, **Then** solo se ofrecen lotes que estén en tránsito.
3. **Given** un traslado o una entrega libres, **When** se eligen lotes, **Then** solo se ofrecen lotes liberados por Calidad.
4. **Given** una recolección libre validada, **When** se consulta WIP, **Then** el material aparece como saldo sin asignar hasta que una OF lo tome.
5. **Given** un pedido libre, **When** se confirma, **Then** recibe un Contpaq ID simulado como si el bridge lo hubiera dado de alta, y sigue el mismo flujo de autorización.

---

### User Story 4 - Chatter en tiempo real desde la API (Priority: P3)

Como usuario, necesito que los mensajes del chatter de un documento aparezcan en vivo en otras pestañas abiertas sobre el mismo documento.

**Why this priority**: En el prototipo ya funciona; sin servidor Blazor, el hub pasa a la API (D-58).

**Independent Test**: Abrir el mismo documento en dos pestañas de Angular, escribir en una y verlo aparecer en la otra.

**Acceptance Scenarios**:

1. **Given** la API corriendo en `:9020`, **When** se envía un mensaje desde una pestaña, **Then** aparece en la otra en menos de 1 segundo.
2. **Given** la API apagada, **When** se abre el chatter, **Then** la aplicación sigue funcionando y el chatter indica que no hay conexión en vivo.

---

### Edge Cases

- Folios con `/` en la ruta: `entregas`, `recepcion`, `recolecciones` y `traslados` usan rutas comodín (`{*Folio}`); Angular debe resolver folios como `PIM/OUT/48214`.
- Folio inexistente: se muestra el mismo "no encontrado" que en Blazor.
- Alias de ruta: `/ventas/inventario` y `/inventario` llevan a la misma pantalla.
- Fuentes y CDN no disponibles: la captura de paridad espera a que carguen las fuentes. Si no cargan, la prueba falla con un motivo explícito, no por diferencia de píxeles.
- Recolección libre sin OF posterior: el saldo sin asignar solo sale de WIP por asignación manual, devolución o scrap.

---

## Requirements *(mandatory)*

### Functional Requirements

**Proyecto y plataforma**

- **FR-001**: El proyecto MUST vivir en `PolyConecta.Web/` en la raíz del repositorio, con componentes standalone, **Angular 22** (D-68), TypeScript 6.0 y **Node 24 LTS** (D-69, fijado en `.nvmrc`). Las versiones van exactas en `package.json`, sin `^` ni `~` (CT-36); la versión mayor no cambia durante la feature.
- **FR-002**: El estilo MUST ser idéntico al del prototipo: `PolyConecta.Presentation/wwwroot/css/app.css` copiado sin cambios, Bootstrap 5.3.2, Bootstrap Icons 1.11.3 y la fuente Inter, con las mismas versiones que carga `App.razor`.
- **FR-003**: El servidor de desarrollo MUST correr en `http://localhost:4200`. `scripts/run.sh --with-angular` MUST levantarlo junto al resto sin afectar el comportamiento actual de `run.sh`.

**Rutas y páginas**

- **FR-004**: La aplicación MUST exponer exactamente estas rutas, con el mismo comportamiento que su página Blazor:

| Ruta | Página Blazor |
| :--- | :--- |
| `/` | `Dashboard.razor` |
| `/pedidos`, `/pedidos/:folio` | `PedidosList.razor`, `PedidoFormView.razor` (usa `Components/Poc/PocSalesOrderForm.razor`) |
| `/fabricacion`, `/fabricacion/:folioOf` | `FabricacionList.razor`, `FabricacionFormView.razor` |
| `/calidad`, `/calidad/:folioOf` | `CalidadList.razor`, `CalidadFormView.razor` |
| `/recolecciones`, `/recolecciones/**` | `RecoleccionesList.razor`, `RecoleccionFormView.razor` |
| `/traslados`, `/traslados/**` | `TrasladosList.razor`, `TrasladoFormView.razor` |
| `/recepcion`, `/recepcion/**` | `RecepcionList.razor`, `RecepcionFormView.razor` |
| `/entregas`, `/entregas/**` | `EntregasList.razor`, `EntregaFormView.razor` |
| `/inventario`, `/ventas/inventario` | `InventarioActualList.razor` |
| `/captura-masiva` | `CapturaMasivaPage.razor` |
| `/incidencias` | `IncidenciasPage.razor` |

- **FR-005**: Los 13 componentes compartidos MUST replicarse con sus mismas entradas, salidas y comportamiento: `OdooTopbar`, `MainLayout`, `OdooBreadcrumb`, `OdooSearchPanel`, `OdooViewSwitcher`, `OdooPager`, `OdooSmartButtons`, `OdooStatusPipeline`, `OdooLineCapture`, `OdooChatterDrawer`, `LotPickerModal`, `LotQuantityPickerModal` y `PocSalesOrderForm`.

**Estado y reglas**

- **FR-006**: El estado MUST portarse 1:1 desde `OperationalFlowState.cs`, `StockOperationState.cs`, `InventoryState.cs`, `SearchViews.cs` y `UiViewState.cs` a servicios de Angular con signals, con los mismos nombres de operación traducidos a camelCase (`Autorizar` → `autorizar`).
- **FR-007**: Los datos semilla MUST ser idénticos a los del prototipo: mismos folios, productos, lotes, cantidades y fechas.
- **FR-008**: Cada regla que el prototipo aplica MUST conservarse y quedar cubierta por una prueba unitaria. Por ejemplo: bloqueo del cierre con lotes en revisión, renombrado `.S` al rechazar, bloqueo del cierre con saldo en WIP, disponibilidad que excluye cuarentena, dos firmas para autorizar y cálculo de backorder en validaciones parciales.
- **FR-009**: La vista de búsqueda MUST ser declarativa por modelo, como en `SearchViews.cs`: facetas removibles, filtros del mismo campo combinados con O y de campos distintos con Y.
- **FR-010**: Los folios de CONTPAQi MUST seguir simulándose como en el prototipo. La réplica no llama a la API REST.
- **FR-011**: El estado MUST vivir en memoria del navegador durante la sesión y comportarse ante una recarga igual que el prototipo. Si Blazor pierde el estado al recargar, Angular también; si lo conserva por circuito, Angular lo conserva en `sessionStorage`. El agente de la Fase 1 verifica el comportamiento de Blazor y lo documenta en `plan.md`.

**Modo libre (D-52, D-59)**

- **FR-012**: El botón "Nuevo" MUST habilitarse en las listas y formularios de los 9 documentos de la tabla, abriendo el mismo formulario del documento vacío, en su estado inicial y sin origen:

| Documento | Campos mínimos al crear | Regla del modo libre |
| :--- | :--- | :--- |
| Pedido de venta | Cliente, líneas (producto, cantidad, unidad, precio unitario y moneda) | Al confirmar recibe un Contpaq ID simulado (D-53). El precio solo existe en pedidos libres (D-74) |
| Orden de fabricación | Proceso, producto, cantidad, unidad | Sin pedido. Sus lotes usan el folio de la OF raíz con `/` → `-` (D-54). Al confirmarla genera su recolección y sus controles igual que una ligada |
| Recolección | Planta, líneas (producto, cantidad, unidad) | MP → WIP sin OF; el saldo queda sin asignar (D-55) |
| Control de calidad | Lote(s) existentes | Solo sobre lotes existentes; aprobar o rechazar tiene los mismos efectos que en un control ligado |
| Traslado | Lotes | Solo lotes liberados por Calidad (hard-stop) |
| Recepción | Lotes | Solo lotes en `TRANS/*` (D-56) |
| Entrega | Cliente, lotes | Solo lotes liberados; sin pedido |
| Devolución (`REC-RET`) | Planta, lotes de WIP, cantidad, unidad | Cantidad capturada a mano |
| Incidencia | Igual que hoy | Ya es libre en el prototipo; no cambia |

**Cantidad y unidad (D-127).** En modo libre, toda línea y toda asignación de lote lleva **cantidad** (número) y **unidad**. La unidad es siempre la **unidad base del producto en CONTPAQi** (`ProductRef.unidad`): se muestra al elegir el producto y no se edita. No hay campo de peso aparte ni conversión entre unidades.

- **FR-013**: Una OF MUST ofrecer la acción **"Asignar saldo de WIP"**, que liga saldo sin asignar de sus componentes. La asignación es manual y nunca automática, por la regla 008-FR-004: el sistema no asigna lotes sin confirmación.
- **FR-014**: Un documento creado libre MUST mostrar sus smart buttons de origen vacíos o deshabilitados, nunca con un origen falso.

**Chatter (D-58)**

- **FR-015**: `PolyConecta.Api` MUST exponer un `ChatterHub` en `/hubs/chatter` con el mismo contrato que `PolyConecta.Presentation/Hubs/ChatterHub.cs` (`SendMessage` → `ReceiveChatterMessage`) y permitir CORS desde `http://localhost:4200`. El hub del prototipo **no se mueve ni se modifica**: la API recibe una copia.
- **FR-016**: Angular MUST conectarse al hub con `@microsoft/signalr` y seguir funcionando si la API no está disponible (US-4, escenario 2).

**Verificación**

- **FR-017**: El proyecto MUST incluir `npm run parity`, que levanta o reutiliza Blazor (`:9000`) y Angular (`:4200`), recorre las 19 rutas en Chromium a 1600×900 con animaciones desactivadas, compara las capturas y guarda un informe en `PolyConecta.Web/parity-report/`, ignorado por git.
- **FR-018**: El proyecto MUST incluir guiones de escenario (US-2) que se ejecutan contra las dos aplicaciones y comparan los textos visibles en cada punto de control.
- **FR-019**: `npm run build`, `npm test` y `npm run parity` MUST terminar sin errores para dar la feature por hecha.

### Key Entities

Son las mismas del prototipo, tipadas en TypeScript bajo `src/app/core/models/`: `ManufacturingOrder`, `BomLine`, `SubProductLine`, `PlanningLine`, `ProductionLot`, `StockOperation`, `StockOperationLine`, `LotAllocation`, `OperationType`, `ProductRef`, `LotBalance`, `StockQuant`, `Incidencia`, `SearchView` y los estados de traslado, recepción y entrega. No se adoptan todavía las entidades de `docs/diseno/04-modelo-de-dominio.md`; eso ocurre al conectar la API.

---

## Success Criteria *(mandatory)*

- **SC-001**: Las 19 rutas pasan la comparación visual (≤ 1 % de píxeles distintos), sin contar el botón "Nuevo".
- **SC-002**: Todos los guiones de escenario producen los mismos textos en Blazor y en Angular.
- **SC-003**: Cada regla de FR-008 tiene al menos una prueba unitaria que pasa.
- **SC-004**: Los 9 documentos de FR-012 se crean en modo libre y cumplen su regla.
- **SC-005**: Un mensaje del chatter aparece en otra pestaña en menos de 1 segundo con la API corriendo.
- **SC-006**: El prototipo Blazor y las pruebas .NET existentes (17) siguen pasando. Si la migración a .NET 10 (A-9) ocurre durante esta feature, se verifica sobre la versión migrada.

---

## Ejecución por agentes (D-61)

Por fases. Una fase empieza cuando la anterior cumple su salida. Dentro de las fases 3 y 4, los agentes trabajan en paralelo y cada uno es **dueño exclusivo** de sus carpetas.

| Fase | Agentes | Trabajo | Dueño de | Salida |
| :--- | :---: | :--- | :--- | :--- |
| 0 · Base | 1 | Crear el proyecto, fijar dependencias, estilos globales (FR-001 a FR-003), esqueleto de las 19 rutas y el arnés de paridad (FR-017), aunque todavía falle | Todo `PolyConecta.Web/` | `npm run build` y `npm run parity` corren (la paridad puede fallar) |
| 1 · Estado | 1 | Modelos, datos semilla y servicios de estado (FR-006 a FR-011) con sus pruebas | `src/app/core/` | `npm test` verde; FR-011 documentado en `plan.md` |
| 2 · Sistema de diseño y chatter | 2 en paralelo | **2A**: los 13 componentes y el layout (FR-005). **2B**: hub en la API y cliente SignalR (FR-015, FR-016) | 2A: `src/app/shared/`. 2B: `PolyConecta.Api/Hubs/`, CORS en `PolyConecta.Api/Program.cs`, `src/app/core/chatter/` | Componentes renderizan; US-4 pasa |
| 3 · Páginas | 3 en paralelo | **3A**: dashboard, pedidos, inventario. **3B**: fabricación, calidad, captura masiva, incidencias. **3C**: recolecciones, traslados, recepción, entregas | `src/app/features/<módulo>/` de cada uno | Sus rutas pasan la paridad visual y sus escenarios |
| 4 · Modo libre | 3 en paralelo | "Nuevo" y formularios de creación (FR-012 a FR-014), con los mismos dueños de la fase 3 | Los mismos | SC-004 |
| 5 · Cierre | 1 | Informe de paridad, `run.sh`, actualización de `docs/diseno/05-arquitectura-tecnica.md` y de los README | Documentación y scripts | FR-019 y SC-001 a SC-006 |

### Reglas de autonomía

1. **Rama y commits.** Todo el trabajo va en la rama `001-angular-presentation`, nunca en `main`. Un commit por tarea de `tasks.md`, con el id de la tarea en el mensaje.
2. **Qué se puede tocar.** Solo `PolyConecta.Web/`, los archivos de la API de FR-015, `scripts/run.sh` (fase 5) y la documentación (fase 5). Todo lo demás es de solo lectura, en especial `PolyConecta.Presentation/`.
3. **Código compartido.** Después de la fase 2, `src/app/core/` y `src/app/shared/` solo admiten **agregar**. Si un agente de módulo necesita cambiar algo existente ahí, lo registra en `bloqueos.md` y sigue con otra tarea.
4. **Dependencias.** Solo están aprobadas: Angular y sus paquetes oficiales, `bootstrap@5.3.2`, `bootstrap-icons@1.11.3`, `@microsoft/signalr`, `@playwright/test`, `pixelmatch`, `pngjs`, `vitest` y `jsdom`. Cualquier otra se registra en `bloqueos.md` y no se instala.
5. **Ante una duda.** Primero, lo que hace el prototipo Blazor. Segundo, esta spec. Tercero, `docs/diseno/`. Si nada lo resuelve, se registra en `bloqueos.md` y no se inventa.
6. **`bloqueos.md`.** Vive en `.specify/features/001-angular-presentation/`. Cada entrada lleva fecha, agente, tarea, pregunta concreta y lo que se hizo mientras tanto. Lo revisa una persona entre fases.
7. **Hecho es verificado.** Una tarea se marca hecha solo si se ejecutaron sus comandos de verificación (`npm run build`, `npm test` y, cuando aplica, `npm run parity` sobre sus rutas) y pasaron. Si una tarea no se puede verificar, queda abierta.
8. **No se debilita una prueba para que pase.** Si una comparación visual falla por algo legítimo, se documenta la excepción en el informe de paridad con su captura. No se sube el umbral.

---

## Assumptions

- Node.js 24 LTS está instalado (24.16 o posterior, lo que exige Angular 22). Angular CLI se usa con `npx` y no se instala globalmente.
- El umbral de 1 % de píxeles absorbe diferencias de antialiasing entre motores de render. Si resulta insuficiente, se ajusta por decisión registrada, no por un agente.
- La réplica no tiene autenticación; cualquier usuario ve todas las acciones, igual que en el prototipo.
- El chatter no persiste mensajes: al reiniciar la API se pierden, igual que hoy al reiniciar Blazor.
- Las diferencias del prototipo con el diseño vigente (estados de OF de D-42, alias de ubicaciones de D-43, nombres de lote de D-54 en documentos ligados) se conservan en la réplica para no romper la paridad, y se corrigen al conectar la API.

---

## Exploración y cambios *(obligatoria, CT-43)*

Las secciones anteriores son el **objetivo primario**, fijado al ratificar la spec. Lo que surja después se registra aquí y se ejecuta en esta misma spec. Si un cambio modifica una decisión validada, se registra también en `docs/diseno/decisiones.md` y se anota aquí su número. Esta spec es de un solo camino (camino 2), así que la columna "Camino" no aplica.

| Fecha | Cambio | Motivo | Impacto (requisitos y tareas) | Decisión |
| :--- | :--- | :--- | :--- | :---: |
| 2026-10-05 | Las fases 0 y 2A las hace la tarea 0.5 de la spec 002. La 001 empieza en la fase 1, sobre lo que deje la 0.5 | Evitar dos proyectos Angular (FR-022 de la 002) | Tabla de "Ejecución por agentes"; plan, "Fases ajustadas" | Aprobado por el usuario (002) |
| 2026-10-05 | El proyecto se llama `PolyConecta.Web` | Nombre de capa | FR-001, FR-017, alcance | D-126 |
| 2026-10-05 | `PocSalesOrderForm` pasa de la fase 2A a la 3A. La 0.5 hace 12 componentes y crea solo los tipos que usan | Inyecta `OperationalFlowState` e `InventoryState`, que no existen hasta la fase 1 | FR-005; fase 3A. FR-022 de la 002 debe decir 12 componentes (se corrige en la rama de la 002) | research R-04 |
| 2026-10-05 | El pedido, el traslado, la recepción y la entrega son colecciones con un documento semilla idéntico al del prototipo. Las operaciones reciben el folio | El prototipo tiene un solo documento de cada uno, y "Nuevo" necesita dónde crear otro | FR-006 se lee como "1:1 en comportamiento sobre la semilla"; FR-012 | research R-02 |
| 2026-10-05 | El estado se pierde al recargar, sin `sessionStorage` | En Blazor los servicios son `Scoped` por circuito y recargar abre uno nuevo | FR-011 resuelto; el escenario 6 de US-2 queda como "al recargar, el estado vuelve a la semilla, igual que en Blazor" | research R-03 |
| 2026-10-05 | El chatter del prototipo no está en vivo: el hub existe, pero ninguna página se conecta. El chatter en vivo es comportamiento nuevo, como "Nuevo". CORS con credenciales para `:4200` | Hallazgo al revisar el código | US-4 ("en el prototipo ya funciona" no es cierto), FR-015, FR-016; la paridad corre con la API apagada | research R-07 |
| 2026-10-05 | La fase 2B espera a la API en .NET 10 (tarea 0.3 de la 002) | La 2B y la 0.3 tocan `PolyConecta.Api` | Orden de fases; SC-006 se verifica sobre .NET 10 | — |
| 2026-10-05 | Traslados, recepción y entregas comparten una lista y un formulario (`features/logistica/documento/`), configurados por tipo en `tipos.ts`, en lugar de una carpeta por documento | Sus tres `.razor` son el mismo marcado; solo cambian textos, etapas, Imprimir, el prefijo "Cliente: " y los smart buttons | L2-T033 a L2-T035 (rutas de archivo); la paridad no cambia | — |
| 2026-10-05 | Ruta inexistente: Blazor responde un 404 vacío del servidor al cargarla directo; la réplica muestra el contenido de `<NotFound>` de `Routes.razor` | Una SPA no puede responder 404 desde el servidor de desarrollo; una página en blanco no ayuda a nadie | FR-004; L2-T036 lo prueba así. Un folio inexistente sí da el mismo "no encontrado" en las dos | — |
| 2026-10-06 | Al autorizar el pedido, la réplica oculta la captura de líneas y deshabilita editar y eliminar en el acto. El prototipo lo hace solo al volver a entrar, porque `PocSalesOrderForm` no escucha `OnChange` y no se vuelve a pintar | Es un fallo de renderizado del prototipo; la regla (`PedidoAbierto`) es la misma | US-2; el guion `autorizacion-pedido` compara al volver a entrar a la página | — |
| 2026-10-06 | Pesaje de rollo, agregar planeación, emitir devolución y captura en la captura masiva no tienen botón en el prototipo: solo existen en sus servicios. La réplica los porta en el estado con sus pruebas y, igual que Blazor, no los muestra. Por eso, con saldo en WIP, la OF no se puede cerrar desde la interfaz en ninguna de las dos | Paridad 1:1 de la interfaz | L2-T041, L2-T042, L2-T045 y L2-T047 se cumplen en el estado; los guiones `flujo-of` y `cierre de la OF` lo comprueban | — |
| 2026-10-06 | Auditor automático `npm run audit` (`e2e/auditoria/`): en cada ruta pulsa cada botón o enlace visible en las dos apps y compara la URL y el texto | Encontrar diferencias de comportamiento de primer nivel sin escribir un guion por botón | Complementa a `npm run scenarios`; encontró dos espacios de más ("maestra" y "backorder") | — |
| 2026-10-06 | "Nuevo" abre `<lista>/nuevo`: una hoja en Borrador con los campos mínimos de FR-012, "Guardar" y "Descartar" (`shared/hoja-nueva`). Al guardar se crea el documento libre y se abre su formulario normal, donde se capturan las líneas o los lotes | Patrón de Odoo: la misma estructura de formulario en todos los documentos | FR-012; L2-T052, L2-T057, L2-T060 | — |
| 2026-10-06 | Folios de los documentos libres: pedido `PV-2026-0001`; OF `EXT-`, `IMP-` o `BOL-2026-NNNN`, la siguiente de su proceso; control de calidad `QC-2026-0001`; recolección, devolución, traslado, recepción y entrega con la secuencia de su planta (`PIM/OUT/…`, `SC/IN/…`). El Contpaq ID simulado del pedido libre es el siguiente al mayor que existe (`26201`) | La spec no fija folios para el modo libre; la secuencia real la dará `IReferenceSequenceService` (002) al conectar la API | L2-T051, L2-T054, L2-T056, L2-T059 | — |
| 2026-10-06 | La logística libre usa lotes reales del inventario: el traslado libre los deja en `TRANS/<planta destino>` al llegar a Hecho, la recepción libre los mete a `<planta>/Stock/MP` y la entrega libre les da salida. Reciben Contpaq ID simulado (`TR-…`, `REM-…`). Los documentos semilla no mueven inventario, igual que el prototipo | Sin traslado que deje lotes en tránsito, la recepción libre (D-56) no tendría qué recibir: la semilla no tiene lotes en `TRANS/*` | FR-012; L2-T059, L2-T060 | — |
| 2026-10-06 | "Liberado por Calidad" en el inventario es un lote libre en `<planta>/Stock/*`, fuera de cuarentena, scrap, WIP y tránsito. Un lote rechazado (`.S`) vive en cuarentena, así que nunca se ofrece. El control de calidad libre se crea sobre lotes de producción en revisión, identificados por OF y lote, porque la semilla repite nombres de lote entre órdenes | Aplicar el hard-stop sin inventar estados nuevos | FR-012; L2-T056, L2-T059 | — |
| 2026-10-06 | "Asignar saldo de WIP" aparece en la barra de estado de una OF solo cuando hay saldo sin asignar de sus componentes en su WIP, y abre un modal donde se asigna lote por lote con la cantidad. Nada se asigna sin esa acción | FR-013; con el botón siempre visible, la OF semilla dejaría de verse igual que en Blazor | L2-T055, L2-T057 | — |
| 2026-10-06 | El hub del chatter no negociaba: `UseCors()` estaba antes de `UseRouting()`, así que la política `chatter` del endpoint no se aplicaba ("Endpoint … contains CORS metadata, but a middleware was not found"). Se movió después de `UseRouting()`; la política por omisión de los controladores no cambia | Hallazgo al escribir L2-T066: L2-T063 compilaba pero no se había probado con el navegador | L2-T063, L2-T066 | — |
| 2026-10-06 | Las páginas se cargan bajo demanda (`loadComponent` en cada archivo de rutas): el bundle inicial bajó de 525.63 kB, sobre el presupuesto de 500 kB, a 248.54 kB | `npm run build` advertía del presupuesto | FR-019; la paridad y los guiones siguen en verde | — |
| 2026-10-06 | Recorrido del quickstart (L2-T072): §1 `./run.sh --with-angular` compila, aplica migraciones, prueba y levanta `:9000`, `:9020` y `:4200`; con Ctrl+C se apagan los tres. §2 `npm ci`, `npm run build` (sin advertencias) y `npm test` (76 pruebas) en verde. §3 paridad 19/19 rutas, máximo 38 px (0.003 %, `/calidad/BOL-2026-0001`), y navegación 6/6. §4 y §5 `npm run scenarios` 26/26 (19 de comportamiento contra Blazor y 7 del modo libre). §6 `npm run chatter` 2/2, también contra la API levantada por `run.sh` con base de datos. §7 `dotnet build` sin errores, `dotnet test` 99 correctas y 29 omitidas (necesitan `BRIDGE_URL`), y `git diff main -- PolyConecta.Presentation/` vacío. Además, `npm run audit` sin diferencias en las 19 rutas | Cierre de la spec | SC-001 a SC-006 | — |
| 2026-10-06 | El merge de `001-angular-presentation` a `main` espera a que la 002 esté terminada y en `main`: esta rama contiene la 002 (se integró en ella, opción B) y llevaría a `main` trabajo sin terminar | CT-44: solo se integra a `main` lo terminado y verificado | L2-T073 queda abierta hasta el merge de la 002 | Decisión del usuario |
| 2026-10-06 | Puertos: `PolyConecta.Web` en `:9000` (también `npm start`), la API en `:9020` y el bridge en `:9030`. `run.sh` ya no levanta el prototipo; el arnés de paridad lo arranca en `:9010`. `--with-angular` se acepta pero ya no hace falta | Decisión del usuario | FR-003, FR-015, FR-017; quickstart y contratos actualizados | D-128 |
| 2026-10-05 | Responsable: Luis Alvarado Martinez, que también revisa `bloqueos.md` entre fases | D-118 no le asignaba líder | Encabezado; regla de autonomía 6 | Decisión del usuario |
| 2026-10-05 | Se aprueban `vitest` y `jsdom` | El runner de pruebas de Angular 22 los necesita | Regla de autonomía 4; FR-019 | Decisión del usuario (research R-06) |
| 2026-10-05 | En modo libre, toda línea y toda asignación de lote lleva cantidad y unidad. La unidad es la unidad base del producto en CONTPAQi, se toma del producto y no se edita. No hay campo de kg aparte ni conversión | Aplicar D-127 a la réplica aunque no esté conectada a la API | FR-012 (nota de cantidad y unidad; filas de recolección y devolución); data-model §1 y §3 | D-127, decisión del usuario (research R-09) |
| 2026-10-05 | La 001 espera a que la 002 se integre a `main` y después trae `main` a su rama. Hasta entonces, solo documentación | Regla de ramas (CT-44) | Orden de fases; plan, "Dependencias con la spec 002" | Decisión del usuario (research R-05) |
| 2026-10-06 | **Cambia R-05: la 001 trae la rama `002-construccion-tecnica` (opción B)** y empieza ya. La parte de la 002 que necesita (0.5: `PolyConecta.Web`, 12 componentes y tipos; 0.3: API en .NET 10) está terminada y con la CI en verde; lo que le falta a la 002 (firma del contrato y VPS) no toca `PolyConecta.Web/`. Cuando la 002 entre a `main`, la 001 trae `main` | Avanzar sin esperar el cierre completo de F0 | Plan, "Dependencias con la spec 002"; fase 0 | Decisión del usuario |
| 2026-10-06 | El prototipo formatea con la cultura del sistema, que en esta máquina es **es-419** (medido con .NET 8): `N` usa 3 decimales, los meses son `ene … sept … dic` y la hora sale como "7:05 p.m.". La réplica fija esos formatos en `core/format/numero.ts`. Si la paridad se corre en una máquina con otra cultura, Blazor cambia y Angular no | Hallazgo al portar los textos | FR-007, FR-008; L2-T015 | — |
| 2026-10-06 | Los servicios mutan los mismos objetos que el prototipo y exponen la señal `cambios`, que sube en cada `Notify()`; las pantallas la leen para volver a pintarse. Es la forma más directa de portar 1:1 la lógica mutable con signals | Paridad de comportamiento (FR-006) | L2-T010 a L2-T012 | — |
| 2026-10-06 | El aviso "Sin conexión en vivo" del panel no cambia los píxeles: va en el `title` del encabezado y como texto solo después de enviar una nota sin conexión. Así la paridad (que corre sin API) no se rompe y US-4, escenario 2, se cumple | La spec pedía las dos cosas | FR-016, contracts/chatter-hub.md; L2-T065 | — |
| 2026-10-06 | L2-T066 (prueba de dos pestañas) se hace cuando exista el formulario del pedido (L2-T024), porque necesita una página con el panel | Orden de tareas | L2-T066 | — |
| 2026-10-06 | Razor recorta el espacio alrededor de los bloques `@if`/`@foreach`, y Angular (con `preserveWhitespaces`, para conservar los espacios entre elementos en línea) no. Donde un bloque queda dentro de texto en línea, la plantilla se escribe en una sola línea para dar el mismo espacio (por ejemplo, la flecha de jerarquía en fabricación) | Paridad visual | FR-005; L2-T026 | — |
| 2026-10-05 | El `./run.sh` de la raíz delega en `scripts/run.sh`, así que FR-003 no cambia | Duda sobre cuál integrar | FR-003 | research R-10 |
| 2026-10-07 | Se trajo `main` con la 002 integrada. Los scripts del VPS (`scripts/vps/`) y `PolyConecta.Contpaq/AGENTS.md` usaban el puerto `5005`; pasan a `9030`, el de `appsettings.json`. La tarea de arranque levanta el bridge sin variables de puerto, así que escucharía en `9030` y la medición lo buscaría en `5005` | Choque entre D-128 y los scripts de la 002 | Scripts del VPS; al integrar hay que volver a publicar el bridge en el VPS | D-128 |
