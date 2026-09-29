# Feature Specification: Presentación en Angular

**Feature Branch**: `001-angular-presentation`

**Created**: 2026-09-28 · **Ratified**: 2026-09-29

**Status**: Ratificada. Lista para `/speckit-plan`.

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
| Proyecto `PolyConecta.Web.Angular` con las 18 páginas y los 13 componentes, con paridad 1:1 | Conectar las pantallas a la API REST: es la siguiente feature (D-48) |
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

- **FR-001**: El proyecto MUST vivir en `PolyConecta.Web.Angular/` en la raíz del repositorio, con componentes standalone, y fijar versiones exactas en `package.json` (sin `^` ni `~`). Se usa la última versión mayor estable de Angular disponible al crear el proyecto y no se actualiza durante la feature.
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
| Pedido de venta | Cliente, líneas (producto, cantidad, unidad) | Al confirmar recibe un Contpaq ID simulado (D-53). Sin precio (D-04; P-19 abierta) |
| Orden de fabricación | Proceso, producto, cantidad, unidad | Sin pedido. Sus lotes usan el folio de la OF raíz con `/` → `-` (D-54). Al confirmarla genera su recolección y sus controles igual que una ligada |
| Recolección | Planta, líneas (producto, cantidad) | MP → WIP sin OF; el saldo queda sin asignar (D-55) |
| Control de calidad | Lote(s) existentes | Solo sobre lotes existentes; aprobar o rechazar tiene los mismos efectos que en un control ligado |
| Traslado | Lotes | Solo lotes liberados por Calidad (hard-stop) |
| Recepción | Lotes | Solo lotes en `TRANS/*` (D-56) |
| Entrega | Cliente, lotes | Solo lotes liberados; sin pedido |
| Devolución (`REC-RET`) | Planta, lotes de WIP, cantidad | Cantidad capturada a mano |
| Incidencia | Igual que hoy | Ya es libre en el prototipo; no cambia |

- **FR-013**: Una OF MUST ofrecer la acción **"Asignar saldo de WIP"**, que liga saldo sin asignar de sus componentes. La asignación es manual y nunca automática, por la regla 008-FR-004: el sistema no asigna lotes sin confirmación.
- **FR-014**: Un documento creado libre MUST mostrar sus smart buttons de origen vacíos o deshabilitados, nunca con un origen falso.

**Chatter (D-58)**

- **FR-015**: `PolyConecta.Api` MUST exponer un `ChatterHub` en `/hubs/chatter` con el mismo contrato que `PolyConecta.Presentation/Hubs/ChatterHub.cs` (`SendMessage` → `ReceiveChatterMessage`) y permitir CORS desde `http://localhost:4200`. El hub del prototipo **no se mueve ni se modifica**: la API recibe una copia.
- **FR-016**: Angular MUST conectarse al hub con `@microsoft/signalr` y seguir funcionando si la API no está disponible (US-4, escenario 2).

**Verificación**

- **FR-017**: El proyecto MUST incluir `npm run parity`, que levanta o reutiliza Blazor (`:9000`) y Angular (`:4200`), recorre las 19 rutas en Chromium a 1600×900 con animaciones desactivadas, compara las capturas y guarda un informe en `PolyConecta.Web.Angular/parity-report/`, ignorado por git.
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
- **SC-006**: El prototipo Blazor y las pruebas .NET existentes (17) siguen pasando sin cambios.

---

## Ejecución por agentes (D-61)

Por fases. Una fase empieza cuando la anterior cumple su salida. Dentro de las fases 3 y 4, los agentes trabajan en paralelo y cada uno es **dueño exclusivo** de sus carpetas.

| Fase | Agentes | Trabajo | Dueño de | Salida |
| :--- | :---: | :--- | :--- | :--- |
| 0 · Base | 1 | Crear el proyecto, fijar dependencias, estilos globales (FR-001 a FR-003), esqueleto de las 19 rutas y el arnés de paridad (FR-017), aunque todavía falle | Todo `PolyConecta.Web.Angular/` | `npm run build` y `npm run parity` corren (la paridad puede fallar) |
| 1 · Estado | 1 | Modelos, datos semilla y servicios de estado (FR-006 a FR-011) con sus pruebas | `src/app/core/` | `npm test` verde; FR-011 documentado en `plan.md` |
| 2 · Sistema de diseño y chatter | 2 en paralelo | **2A**: los 13 componentes y el layout (FR-005). **2B**: hub en la API y cliente SignalR (FR-015, FR-016) | 2A: `src/app/shared/`. 2B: `PolyConecta.Api/Hubs/`, CORS en `PolyConecta.Api/Program.cs`, `src/app/core/chatter/` | Componentes renderizan; US-4 pasa |
| 3 · Páginas | 3 en paralelo | **3A**: dashboard, pedidos, inventario. **3B**: fabricación, calidad, captura masiva, incidencias. **3C**: recolecciones, traslados, recepción, entregas | `src/app/features/<módulo>/` de cada uno | Sus rutas pasan la paridad visual y sus escenarios |
| 4 · Modo libre | 3 en paralelo | "Nuevo" y formularios de creación (FR-012 a FR-014), con los mismos dueños de la fase 3 | Los mismos | SC-004 |
| 5 · Cierre | 1 | Informe de paridad, `run.sh`, actualización de `docs/diseno/05-arquitectura-tecnica.md` y de los README | Documentación y scripts | FR-019 y SC-001 a SC-006 |

### Reglas de autonomía

1. **Rama y commits.** Todo el trabajo va en la rama `001-angular-presentation`, nunca en `main`. Un commit por tarea de `tasks.md`, con el id de la tarea en el mensaje.
2. **Qué se puede tocar.** Solo `PolyConecta.Web.Angular/`, los archivos de la API de FR-015, `scripts/run.sh` (fase 5) y la documentación (fase 5). Todo lo demás es de solo lectura, en especial `PolyConecta.Presentation/`.
3. **Código compartido.** Después de la fase 2, `src/app/core/` y `src/app/shared/` solo admiten **agregar**. Si un agente de módulo necesita cambiar algo existente ahí, lo registra en `bloqueos.md` y sigue con otra tarea.
4. **Dependencias.** Solo están aprobadas: Angular y sus paquetes oficiales, `bootstrap@5.3.2`, `bootstrap-icons@1.11.3`, `@microsoft/signalr`, `@playwright/test`, `pixelmatch` y `pngjs`. Cualquier otra se registra en `bloqueos.md` y no se instala.
5. **Ante una duda.** Primero, lo que hace el prototipo Blazor. Segundo, esta spec. Tercero, `docs/diseno/`. Si nada lo resuelve, se registra en `bloqueos.md` y no se inventa.
6. **`bloqueos.md`.** Vive en `.specify/features/001-angular-presentation/`. Cada entrada lleva fecha, agente, tarea, pregunta concreta y lo que se hizo mientras tanto. Lo revisa una persona entre fases.
7. **Hecho es verificado.** Una tarea se marca hecha solo si se ejecutaron sus comandos de verificación (`npm run build`, `npm test` y, cuando aplica, `npm run parity` sobre sus rutas) y pasaron. Si una tarea no se puede verificar, queda abierta.
8. **No se debilita una prueba para que pase.** Si una comparación visual falla por algo legítimo, se documenta la excepción en el informe de paridad con su captura. No se sube el umbral.

---

## Assumptions

- Node.js 24 está instalado. Angular CLI se usa con `npx` y no se instala globalmente.
- El umbral de 1 % de píxeles absorbe diferencias de antialiasing entre motores de render. Si resulta insuficiente, se ajusta por decisión registrada, no por un agente.
- La réplica no tiene autenticación; cualquier usuario ve todas las acciones, igual que en el prototipo.
- El chatter no persiste mensajes: al reiniciar la API se pierden, igual que hoy al reiniciar Blazor.
- Las diferencias del prototipo con el diseño vigente (estados de OF de D-42, alias de ubicaciones de D-43, nombres de lote de D-54 en documentos ligados) se conservan en la réplica para no romper la paridad, y se corrigen al conectar la API.
