# Tasks: Presentación en Angular

> **Una sola sección (L2).** Esta spec es de un solo camino (plan, primera nota), así que no hay secciones L1 ni Común. Los ids llevan el prefijo del camino: `L2-T001`. Responsable: Luis Alvarado Martinez. Las tareas que nazcan de la exploración se agregan con la referencia a su fila en "Exploración y cambios" de `spec.md` (CT-43).

**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: la spec los pide. Hay pruebas unitarias de cada regla (FR-008, SC-003), paridad visual (FR-017), guiones de escenario (FR-018) y la prueba del chatter (SC-005).

**Rutas**: todo vive bajo `PolyConecta.Web/`, salvo el hub (`PolyConecta.Api/`) y `scripts/run.sh`. Los nombres de archivo siguen la convención que deje la tarea 0.5 de la spec 002. Aquí se indica la carpeta o el archivo principal.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: se puede hacer en paralelo: archivos distintos y sin depender de tareas abiertas.
- **[US1]…[US4]**: historia de usuario de la spec.
- **Agente**: entre corchetes, el agente de la tabla "Ejecución por agentes" (`1`, `2B`, `3A`, `3B`, `3C`, `5`). Cada uno es dueño exclusivo de sus carpetas (regla de autonomía 2).

## Reglas que aplican a todas las tareas

- **Verificación:** una tarea se marca hecha solo si pasaron sus comandos de verificación (regla de autonomía 7). Se hace un commit por tarea, con su id en el mensaje (regla 1).
- **Dudas y faltantes:** si el prototipo, la spec y `docs/diseno/` no resuelven algo, se registra en `bloqueos.md` y no se inventa (regla 5).
- **Código compartido:** después de la fase 2, en `src/app/core/` y `src/app/shared/` solo se agrega (regla 3). Por eso la lógica del modo libre va en archivos nuevos por módulo (`core/state/libre/`) y no en los servicios existentes.

---

## Fase 0: Arranque (después del merge de la 002)

**Propósito**: comprobar lo que dejó la tarea 0.5 y montar los arneses de verificación.

**⚠️ No empieza** hasta que `002-construccion-tecnica` esté integrada a `main` (research R-05).

- [x] L2-T001 [1] Traer `main` a la rama `001-angular-presentation` y verificar `cd PolyConecta.Web && npm ci && npm run build`. Comprobar que la 0.5 dejó:
  - `.nvmrc` en 24.16.0 y versiones exactas en `package.json` (CT-36);
  - `src/styles/app.css` idéntico a `PolyConecta.Presentation/wwwroot/css/app.css` (`diff` vacío);
  - en `src/index.html`, Inter, Bootstrap 5.3.2 y Bootstrap Icons 1.11.3 por las mismas URL que `App.razor`;
  - los 12 componentes de `src/app/shared/` (todos los de FR-005 salvo `PocSalesOrderForm`);
  - los tipos `ProductRef`, `LotBalance`, `LotAllocation`, `StockOperationLine` y `ProductionLot` en `src/app/core/models/`, y `UiViewState`.

  Cada faltante se registra en `bloqueos.md`.
- [x] L2-T002 [1] Crear `.specify/features/001-angular-presentation/bloqueos.md` con el encabezado de la regla de autonomía 6 (fecha, agente, tarea, pregunta y lo hecho mientras tanto).
- [x] L2-T003 [1] Si la 0.5 no lo dejó: instalar `vitest@5.0.3` y `jsdom@30.1.2` exactos y configurar `ng test` con el builder `@angular/build:unit-test` en `PolyConecta.Web/angular.json`, en una corrida sin modo observador para `npm test` (research R-06).
- [x] L2-T004 [P] [1] Instalar `@playwright/test@1.63.0`, `pixelmatch@7.2.0` y `pngjs@7.0.0` exactos. Crear `PolyConecta.Web/e2e/playwright.config.ts` con Chromium a 1600×900, `reducedMotion: 'reduce'`, `baseURL` por proyecto (`blazor` en `:9000` y `angular` en `:4200`) y `webServer` que reutilice los servidores si ya responden (research R-08).
- [x] L2-T005 [1] Completar `PolyConecta.Web/src/app/app.routes.ts` con las 19 rutas de FR-004. Las páginas que aún no existan llevan un componente provisional. Los folios con `/` de `entregas`, `recepcion`, `recolecciones` y `traslados` se resuelven con un `UrlMatcher` que toma el resto de la ruta como folio (caso borde de la spec). `/ventas/inventario` y `/inventario` llevan a la misma página.
- [x] L2-T006 [1] Crear el arnés de paridad en `PolyConecta.Web/e2e/parity/` según [contracts/parity.md](contracts/parity.md):
  - `routes.ts` con las 19 rutas y sus folios de semilla, leídos del prototipo corriendo;
  - `parity.spec.ts`, que espera `document.fonts.ready` (falla con motivo explícito si no cargan las fuentes o el CDN), espera el circuito de Blazor, enmascara el botón "Nuevo" y compara con `pixelmatch` (`threshold` 0.1; pasa con ≤ 1 % de píxeles distintos);
  - el informe en `parity-report/index.html`;
  - el filtro `--routes`;
  - el script `"parity"` en `package.json` y `parity-report/` en `.gitignore`.

  Se verifica corriendo `npm run parity`: puede fallar en las rutas, pero no en el arnés.
- [x] L2-T007 [P] [1] Crear el arnés de guiones en `PolyConecta.Web/e2e/scenarios/runner.ts`. Los pasos son `ir`, `pulsar`, `capturar` y `elegirLote`, y los puntos de control leen el texto visible de un contenedor. El runner corre el mismo guion contra `:9000` y `:4200` y compara los textos; los guiones marcados `soloAngular` solo corren contra `:4200`. Agregar el script `"scenarios"` en `package.json` ([contracts/parity.md](contracts/parity.md)).

**Punto de control**: `npm run build`, `npm test` (sin pruebas todavía) y `npm run parity` corren.

---

## Fase 1: Estado (bloquea todas las historias)

**Propósito**: portar los servicios de estado, la búsqueda y la semilla con sus reglas probadas (FR-006 a FR-011). Agente `1`, dueño de `src/app/core/`.

**⚠️ CRÍTICO**: ninguna página se construye antes de cerrar esta fase.

### Modelos y semilla

- [x] L2-T008 [1] Completar `PolyConecta.Web/src/app/core/models/` con los tipos de [data-model.md §1](data-model.md#1-tipos-de-coremodels):
  - `ProductClass`, `LotStatus`, `StockQuant`, `OperationType`, `StockOperation`, `BomLine`, `SubProductLine`, `PlanningLine`, `ManufacturingOrder`, `QualityControlState`, `SalesOrderLine` (con `moneda` opcional), `ProcessCheck`, `ShipmentLine` e `Incidencia`;
  - los cuatro documentos que pasan a colección (`SalesOrder`, `InterplantTransfer`, `Reception`, `Delivery`), con `libre: boolean`;
  - los textos de estado como uniones de cadenas idénticas a las del prototipo (`'Borrador' | 'Planeado' | 'En progreso' | 'Hecho'`, `'En revisión' | 'Aprobado' | 'Rechazado'`).

  No se cambian los tipos que dejó la 0.5.
- [x] L2-T009 [1] Crear la semilla en `PolyConecta.Web/src/app/core/seed/`, copiada valor por valor de los constructores e inicializadores de `PolyConecta.Presentation/Services/`:
  - productos y lotes (`InventoryState.cs`);
  - tipos de operación, incluidos `PIM-REC-RET` y `STC-REC-RET`, y la secuencia de folios que arranca en 48213 (`StockOperationState.cs`);
  - las OF `BOL-2026-0001`, `IMP-2026-0001` y `EXT-2026-0001`, la cartera de extrusión de `BuildExtrusionBacklog` y la lista de precios;
  - el pedido `IV310-26` con `ordenCompraCliente` 3893, `agente` "Celia Villarreal", `contpaqId` 26200, `cantidadPedido` 5500 y sus líneas y procesos;
  - el traslado `PIM/OUT/48213`, la recepción `SC/IN/50974`, la entrega `SC/OUT/31688` y las incidencias (`OperationalFlowState.cs`).

  Mismos folios, cantidades y fechas (FR-007).

### Servicios

- [x] L2-T010 [1] Implementar `PolyConecta.Web/src/app/core/state/inventory-state.ts` con signals: `getProducto`, `fisico`, `reservado`, `enWip`, `disponible`, `lotesDe`, `disponibleDeLote`, `lotesDisponibles`, `existencias`, `reservar`, `liberarReservasDe`, `moverAWip`, `devolverDeWip`, `saldoWip` y `saldoWipTotal`, más las estáticas `classLabel` y `esVendible`. Misma lógica que `InventoryState.cs`.
- [x] L2-T011 [1] Implementar `PolyConecta.Web/src/app/core/state/stock-operation-state.ts`: `getTipo`, `get`, `deOf`, `tieneRecoleccion`, `asegurarRecoleccion`, `confirmarRecoleccion`, `emitirDevolucion`, `asignarLote`, `quitarLote`, `comprobarDisponibilidad`, `validar` (devuelve `{ op, error }`), `cancelar`, `puedeCerrarOf` (devuelve `{ ok, motivo }`), `totalRecolectado` y `totalDevuelto`. Los mensajes de error y aviso deben ser idénticos, con el formato `N1` de .NET (depende de L2-T010 y L2-T015).
- [x] L2-T012 [1] Implementar `PolyConecta.Web/src/app/core/state/operational-flow-state.ts` con colecciones de pedidos, traslados, recepciones y entregas (research R-02). Cada operación que en el prototipo actuaba sobre el documento único recibe su folio: `autorizar(folio)`, `revocarFirmas(folio)`, `setOrderStage(folio, stage)`, `validarTraslado(folio)`, `validarRecepcion(folio)`, `validarEntrega(folio)`, `comprobarDisponibilidad*(folio)`. Sobre la semilla, el comportamiento es idéntico a `OperationalFlowState.cs`. Incluye `agregarLineaPedido` con precio precargado de la lista, `planear`, `agregarPlaneacion`, `registrarPesajeRollo`, `aprobarLote`, `rechazarLote`, `cerrarProduccion`, el alta de recolecciones por OF del constructor y `resetAll` (depende de L2-T010 y L2-T011).
- [x] L2-T013 [P] [1] Si la 0.5 no lo dejó, implementar `PolyConecta.Web/src/app/core/state/ui-view-state.ts`: `setViewMode`, `setDocumentType` y `setSearchQuery`.
- [x] L2-T014 [1] Implementar `PolyConecta.Web/src/app/core/search/search-view.ts`, con `SearchField`, `SearchFilter`, `SearchGroupBy` y `SearchView<T>.aplicar`: el texto se busca en todos los campos, los filtros del mismo `campo` se combinan con O y los de campos distintos con Y (FR-009). Agregar las vistas por modelo de `SearchViews.cs` (OF, operaciones, pedidos con `SalesOrderRow`, logística con `LogisticsRow`) en `PolyConecta.Web/src/app/core/search/views.ts`.
- [x] L2-T015 [P] [1] Crear `PolyConecta.Web/src/app/core/format/` con los formatos que usa Blazor: números `N0`, `N1` y `N2`, moneda y fechas (`dd/MM/yyyy`, `h:mm tt`). Usar la misma cultura que el prototipo; se comprueba contra textos reales de `:9000`.

### Pruebas de reglas (FR-008, SC-003)

- [x] L2-T016 [P] [1] Pruebas en `PolyConecta.Web/src/app/core/state/inventory-state.spec.ts`:
  - la disponibilidad excluye la cuarentena;
  - `reservar` es por lote;
  - `moverAWip` y `devolverDeWip` mueven saldo y conservan el total;
  - `saldoWip` por OF.
- [x] L2-T017 [P] [1] Pruebas en `PolyConecta.Web/src/app/core/state/stock-operation-state.spec.ts`:
  - validar una recolección parcial crea un backorder con `backorderDe` y lo pendiente;
  - declarar más de lo pendiente da el error exacto del prototipo;
  - declarar más de lo disponible en el lote da el error exacto;
  - `puedeCerrarOf` con saldo en WIP devuelve el motivo exacto.
- [x] L2-T018 [P] [1] Pruebas en `PolyConecta.Web/src/app/core/state/operational-flow-state.spec.ts`:
  - `autorizar` solo en Confirmado; la primera firma no cambia la etapa y la segunda lleva a Autorizado;
  - `revocarFirmas` borra las firmas, libera reservas y regresa a Confirmado;
  - `rechazarLote` agrega `.S` una sola vez y el lote no suma en `producidoTotal`;
  - `cerrarProduccion` no pasa a Hecho con lotes "En revisión" si `calidadRequerida` (hard-stop);
  - el pedido pasa a Hecho cuando todas sus OF están en Hecho;
  - cada OF con componentes nace con su recolección en Borrador.
- [x] L2-T019 [P] [1] Pruebas en `PolyConecta.Web/src/app/core/search/search-view.spec.ts`: búsqueda de texto, O dentro del mismo campo, Y entre campos y agrupaciones.
- [x] L2-T020 [1] Revisar `OperationalFlowState.cs`, `StockOperationState.cs` e `InventoryState.cs` línea por línea, agregar en [data-model.md §3](data-model.md#3-reglas-que-se-prueban-fr-008-fr-012) cada regla que falte y escribir su prueba en el `.spec.ts` que corresponda. FR-008 da ejemplos, no una lista cerrada.

**Punto de control**: `npm test` en verde, con al menos una prueba por cada regla de data-model §3 que no sea de modo libre.

---

## Fase 2: User Story 1 – La misma aplicación, ahora en Angular (P1) 🎯 MVP

**Objetivo**: las 18 páginas, en sus 19 rutas, se ven y navegan igual que en Blazor con los datos semilla.

**Prueba independiente**: `npm run parity` pasa en las 19 rutas (SC-001), y los enlaces directos y la navegación atrás y adelante se comportan igual.

Los tres agentes trabajan en paralelo, cada uno en sus carpetas (plan, "Decisión de estructura"). Cada página reproduce el marcado y las clases de su `.razor` para que la paridad pase.

### Agente 3A · `features/plataforma/`, `features/ventas/`, `features/inventario/`

- [x] L2-T021 [P] [US1] [3A] Dashboard en `PolyConecta.Web/src/app/features/plataforma/dashboard/`, desde `Pages/Dashboard.razor`: mosaico de aplicaciones que navega a cada módulo. Verificar con `npm run parity -- --routes=/`.
- [x] L2-T022 [P] [US1] [3A] Lista de pedidos en `PolyConecta.Web/src/app/features/ventas/pedidos-list/`, desde `Pages/PedidosList.razor`, con la búsqueda de L2-T014. Verificar con `--routes=/pedidos`.
- [x] L2-T023 [US1] [3A] Agregar `PocSalesOrderForm` en `PolyConecta.Web/src/app/shared/poc-sales-order-form/`, desde `Components/Poc/PocSalesOrderForm.razor`, con las entradas de ese componente y los servicios `OperationalFlowState` e `InventoryState`. Es un componente nuevo en `shared/`, así que lo permite la regla 3 (research R-04).
- [x] L2-T024 [US1] [3A] Formulario del pedido en `PolyConecta.Web/src/app/features/ventas/pedido-form/`, desde `Pages/PedidoFormView.razor`, con `PocSalesOrderForm`, el chatter y las entradas semilla del chatter. Verificar con `--routes=/pedidos/IV310-26` (depende de L2-T023).
- [x] L2-T025 [P] [US1] [3A] Inventario actual en `PolyConecta.Web/src/app/features/inventario/inventario-actual/`, desde `Pages/InventarioActualList.razor`, servido en `/inventario` y `/ventas/inventario`. Verificar con `--routes=/inventario,/ventas/inventario`.

### Agente 3B · `features/produccion/`, `features/calidad/`

- [x] L2-T026 [P] [US1] [3B] Lista de fabricación en `PolyConecta.Web/src/app/features/produccion/fabricacion-list/`, desde `Pages/FabricacionList.razor`. Verificar con `--routes=/fabricacion`.
- [x] L2-T027 [US1] [3B] Formulario de la OF en `PolyConecta.Web/src/app/features/produccion/fabricacion-form/`, desde `Pages/FabricacionFormView.razor` (418 líneas): pestañas, componentes, subproductos, producción, planeación, smart buttons, pipeline de estado y chatter. Verificar con `--routes=/fabricacion/BOL-2026-0001,/fabricacion/IMP-2026-0001,/fabricacion/EXT-2026-0001`.
- [x] L2-T028 [P] [US1] [3B] Captura masiva en `PolyConecta.Web/src/app/features/produccion/captura-masiva/`, desde `Pages/CapturaMasivaPage.razor`. Verificar con `--routes=/captura-masiva`.
- [x] L2-T029 [P] [US1] [3B] Incidencias en `PolyConecta.Web/src/app/features/produccion/incidencias/`, desde `Pages/IncidenciasPage.razor`. Verificar con `--routes=/incidencias`.
- [x] L2-T030 [P] [US1] [3B] Lista de calidad en `PolyConecta.Web/src/app/features/calidad/calidad-list/`, desde `Pages/CalidadList.razor`. Verificar con `--routes=/calidad`.
- [x] L2-T031 [US1] [3B] Formulario de calidad en `PolyConecta.Web/src/app/features/calidad/calidad-form/`, desde `Pages/CalidadFormView.razor`. Verificar con `--routes=/calidad/BOL-2026-0001`.

### Agente 3C · `features/logistica/`

- [ ] L2-T032 [P] [US1] [3C] Lista y formulario de recolecciones en `PolyConecta.Web/src/app/features/logistica/recolecciones/`, desde `RecoleccionesList.razor` y `RecoleccionFormView.razor`, con `LotQuantityPickerModal`. El formulario resuelve folios con `/`. Verificar con `--routes=/recolecciones,/recolecciones/<folio de la semilla>`.
- [ ] L2-T033 [P] [US1] [3C] Lista y formulario de traslados en `PolyConecta.Web/src/app/features/logistica/traslados/`, desde `TrasladosList.razor` y `TrasladoFormView.razor`, con `LotPickerModal`. Verificar con `--routes=/traslados,/traslados/PIM/OUT/48213`.
- [ ] L2-T034 [P] [US1] [3C] Lista y formulario de recepción en `PolyConecta.Web/src/app/features/logistica/recepcion/`, desde `RecepcionList.razor` y `RecepcionFormView.razor`. Verificar con `--routes=/recepcion,/recepcion/SC/IN/50974`.
- [ ] L2-T035 [P] [US1] [3C] Lista y formulario de entregas en `PolyConecta.Web/src/app/features/logistica/entregas/`, desde `EntregasList.razor` y `EntregaFormView.razor`. Verificar con `--routes=/entregas,/entregas/SC/OUT/31688`.

### Navegación (agente 3A, al terminar los tres)

- [ ] L2-T036 [US1] [3A] Crear la prueba `PolyConecta.Web/e2e/parity/navigation.spec.ts`, que corre contra las dos aplicaciones:
  - un enlace directo a `/fabricacion/BOL-2026-0001` abre el documento sin pasar por la lista;
  - un folio inexistente muestra el mismo "no encontrado" que Blazor;
  - atrás y adelante del navegador dejan la misma URL y el mismo título en las dos aplicaciones.
- [ ] L2-T037 [US1] Correr `npm run parity` completo y dejar las 19 rutas en ≤ 1 % de píxeles distintos. Una diferencia legítima va a `parity-report/excepciones.md` con su captura; el umbral no se sube (regla 8).

**Punto de control**: SC-001 cumplido. La réplica se ve y navega como el prototipo.

---

## Fase 3: User Story 2 – El flujo operativo funciona igual (P1)

**Objetivo**: cada acción de cada página produce los mismos cambios de estado que en Blazor.

**Prueba independiente**: `npm run scenarios` da los mismos textos en Blazor y en Angular en cada punto de control (SC-002).

### Agente 3A

- [ ] L2-T038 [US2] [3A] Conectar las acciones del pedido en `features/ventas/pedido-form/` y en `shared/poc-sales-order-form/`: confirmar, autorizar (botón único que firma el rol pendiente), revocar firmas y agregar o quitar líneas con precio precargado.
- [ ] L2-T039 [P] [US2] [3A] Guion `PolyConecta.Web/e2e/scenarios/autorizacion-pedido.scenario.ts` (US-2, escenario 1): confirmar `IV310-26`, pulsar "Autorizar" dos veces y comprobar la etapa, las firmas y las OF del pedido.
- [ ] L2-T040 [P] [US2] [3A] Guion `PolyConecta.Web/e2e/scenarios/busqueda.scenario.ts` (escenario 5): en pedidos, fabricación e inventario, buscar texto, aplicar dos filtros del mismo campo y dos de campos distintos, y quitar una faceta.

### Agente 3B

- [ ] L2-T041 [US2] [3B] Conectar las acciones de la OF en `features/produccion/fabricacion-form/`:
  - planear;
  - agregar y quitar componentes, subproductos y lotes de producción;
  - agregar planeación;
  - registrar el pesaje de un rollo;
  - cerrar la producción, con el bloqueo del hard-stop visible igual que en Blazor.
- [ ] L2-T042 [P] [US2] [3B] Conectar aprobar y rechazar lote en `features/calidad/calidad-form/`, la captura en `features/produccion/captura-masiva/` y el alta de incidencias en `features/produccion/incidencias/`.
- [ ] L2-T043 [US2] [3B] Guion `PolyConecta.Web/e2e/scenarios/flujo-of.scenario.ts` (escenario 2): planear una OF, validar su recolección con lotes, capturar rollos, aprobar uno, rechazar otro (nombre con `.S`) y cerrar. Comprueba estados, saldo en WIP, nombres de lote e inventario en cada paso. Depende de L2-T047.
- [ ] L2-T044 [P] [US2] [3B] Guion `PolyConecta.Web/e2e/scenarios/hard-stop.scenario.ts` (escenario 3): con un lote en revisión, intentar cerrar la OF y comprobar que se bloquea.

### Agente 3C

- [ ] L2-T045 [US2] [3C] Conectar las acciones de la recolección en `features/logistica/recolecciones/`: asignar y quitar lotes en `LotQuantityPickerModal`, comprobar disponibilidad, validar (con backorder si es parcial), cancelar y emitir devolución.
- [ ] L2-T046 [P] [US2] [3C] Conectar comprobar disponibilidad, seleccionar lotes y validar en traslados, recepción y entregas, con el "Contpaq ID" simulado y los avisos y errores iguales a Blazor.
- [ ] L2-T047 [US2] [3C] Guion `PolyConecta.Web/e2e/scenarios/recoleccion.scenario.ts`: validación parcial con backorder y devolución a stock.
- [ ] L2-T048 [P] [US2] [3C] Guion `PolyConecta.Web/e2e/scenarios/logistica.scenario.ts` (escenario 4): traslado, recepción y entrega, con estados y "Contpaq ID" en cada paso.
- [ ] L2-T049 [P] [US2] [3C] Guion `PolyConecta.Web/e2e/scenarios/recarga.scenario.ts` (escenario 6, research R-03): modificar el estado, recargar la pestaña y comprobar que vuelve a la semilla en las dos aplicaciones.

**Punto de control**: SC-002 cumplido, con `npm run parity` todavía en verde.

---

## Fase 4: User Story 3 – Crear documentos en modo libre (P2)

**Objetivo**: "Nuevo" crea los 9 documentos de FR-012 sin origen y con sus reglas.

**Prueba independiente**: para cada documento, pulsar "Nuevo", capturar, guardar y ver que aparece en su lista sin origen y que su regla aplica (SC-004).

**Regla común (FR-012, D-127)**: toda línea y toda asignación de lote lleva cantidad y unidad. La unidad es `ProductRef.unidad`, se muestra al elegir el producto y no se edita. No hay campo de kg ni conversión. Un documento libre muestra sus smart buttons de origen vacíos o deshabilitados (FR-014).

### Base (agente 1, antes de los tres)

- [ ] L2-T050 [1] Crear `PolyConecta.Web/src/app/core/state/libre/linea-libre.ts` con el borrador de línea libre (producto, cantidad, unidad tomada de `ProductRef.unidad`) y su validación: no se guarda sin cantidad mayor que cero ni con una unidad distinta a la del producto. Agregar su prueba en `linea-libre.spec.ts`.

### Agente 3A

- [ ] L2-T051 [US3] [3A] Agregar `PolyConecta.Web/src/app/core/state/libre/pedido-libre.ts`, que crea un `SalesOrder` con `libre = true`, un cliente y líneas con cantidad, unidad, precio unitario y moneda (D-74). Al confirmarlo recibe un Contpaq ID simulado (D-53) y sigue el mismo flujo de dos firmas. Probarlo en `pedido-libre.spec.ts`.
- [ ] L2-T052 [US3] [3A] Habilitar "Nuevo" en `features/ventas/pedidos-list/` y `features/ventas/pedido-form/`: abre el formulario vacío en Borrador y sin origen.
- [ ] L2-T053 [P] [US3] [3A] Guion `soloAngular` en `PolyConecta.Web/e2e/scenarios/libre-pedido.scenario.ts`.

### Agente 3B

- [ ] L2-T054 [US3] [3B] Agregar `PolyConecta.Web/src/app/core/state/libre/of-libre.ts`, que crea una OF sin pedido con proceso, producto, cantidad y unidad:
  - sus lotes usan el folio de la OF raíz con `/` → `-`, como `R001-BOL-2026-0007` (D-54);
  - al confirmarla genera su recolección y sus controles igual que una ligada.

  Probarlo en `of-libre.spec.ts`.
- [ ] L2-T055 [US3] [3B] Agregar `PolyConecta.Web/src/app/core/state/libre/asignar-saldo-wip.ts` con la acción "Asignar saldo de WIP" (FR-013). Liga saldo sin asignar de los componentes de la OF solo por acción explícita y nunca de forma automática. Probarlo en `asignar-saldo-wip.spec.ts`.
- [ ] L2-T056 [US3] [3B] Agregar `PolyConecta.Web/src/app/core/state/libre/calidad-libre.ts`, que crea un control sobre lotes existentes. Aprobar o rechazar tiene los mismos efectos que en un control ligado. Probarlo en `calidad-libre.spec.ts`.
- [ ] L2-T057 [US3] [3B] Habilitar "Nuevo" en fabricación y calidad (listas y formularios), y la acción "Asignar saldo de WIP" en `features/produccion/fabricacion-form/`. Verificar que incidencias sigue igual que en el prototipo.
- [ ] L2-T058 [P] [US3] [3B] Guiones `soloAngular` en `PolyConecta.Web/e2e/scenarios/libre-of.scenario.ts` y `libre-calidad.scenario.ts`.

### Agente 3C

- [ ] L2-T059 [US3] [3C] Agregar `PolyConecta.Web/src/app/core/state/libre/operaciones-libres.ts`. Probar cada regla en `operaciones-libres.spec.ts`:
  - **Recolección libre:** planta y líneas; MP → WIP sin OF y el saldo queda sin asignar (D-55).
  - **Traslado libre:** solo lotes liberados por Calidad.
  - **Recepción libre:** solo lotes en `TRANS/*` (D-56).
  - **Entrega libre:** cliente y lotes liberados, sin pedido.
  - **Devolución `REC-RET`:** planta, lotes de WIP, cantidad y unidad.
- [ ] L2-T060 [US3] [3C] Habilitar "Nuevo" en recolecciones, traslados, recepción y entregas (listas y formularios). Los selectores de lotes solo ofrecen los lotes que permite cada regla.
- [ ] L2-T061 [P] [US3] [3C] Guiones `soloAngular` en `PolyConecta.Web/e2e/scenarios/libre-operaciones.scenario.ts`, uno por documento.

**Punto de control**: SC-004 cumplido. `npm run parity` sigue en verde, porque "Nuevo" está enmascarado.

---

## Fase 5: User Story 4 – Chatter en tiempo real desde la API (P3)

**Objetivo**: un mensaje del chatter aparece en otras pestañas abiertas sobre el mismo documento.

**Prueba independiente**: `npm run chatter` (SC-005).

**Cuándo**: es la fase 2B. Empieza en cuanto la API esté en .NET 10 (tarea 0.3 de la 002) y puede correr en paralelo con la fase 1. Debe terminar antes de que se cierre la fase 1, porque después `shared/` solo admite agregar (regla 3) y L2-T065 cambia el panel. Agente `2B`, dueño de `PolyConecta.Api/Hubs/`, el CORS en `PolyConecta.Api/Program.cs` y `src/app/core/chatter/`.

- [x] L2-T062 [US4] [2B] Copiar `PolyConecta.Presentation/Hubs/ChatterHub.cs` a `PolyConecta.Api/Hubs/ChatterHub.cs`, cambiando solo el namespace. El original no se toca (D-60).
- [x] L2-T063 [US4] [2B] En `PolyConecta.Api/Program.cs`:
  - agregar `AddSignalR()`;
  - crear una política de CORS con nombre que admita el origen `http://localhost:4200`, cualquier encabezado y método, y credenciales;
  - mapear `/hubs/chatter` con esa política.

  La política por omisión de los controladores no cambia ([contracts/chatter-hub.md](contracts/chatter-hub.md), research R-07). Verificar con `dotnet build Polyconecta.slnx`.
- [x] L2-T064 [US4] [2B] Implementar `PolyConecta.Web/src/app/core/chatter/chatter.service.ts` con `@microsoft/signalr@10.0.11`:
  - URL del hub por configuración (`http://localhost:9020/hubs/chatter`);
  - `withAutomaticReconnect`;
  - un signal con el estado de la conexión;
  - `enviar(documentId, text)`;
  - un flujo de mensajes filtrado por `documentId`, con la hora local de recepción en `h:mm tt` (no se usa la del servidor).

  Probarlo con un hub simulado en `chatter.service.spec.ts`.
- [x] L2-T065 [US4] [2B] Integrar el servicio en `PolyConecta.Web/src/app/shared/` (`OdooChatterDrawer`):
  - con conexión, envía por el hub y muestra el mensaje al recibirlo, sin duplicar el propio;
  - sin conexión, agrega local como en Blazor (autor `Administrator`) y muestra "Sin conexión en vivo".

  Con la API apagada, el panel se ve igual que en el prototipo para que la paridad no cambie. Depende de L2-T064.
- [ ] L2-T066 [US4] [2B] Crear la prueba `PolyConecta.Web/e2e/chatter/chatter.spec.ts` y el script `"chatter"` en `package.json`. Dos pestañas sobre el mismo formulario con la API corriendo: el mensaje llega en menos de 1 s (SC-005). Con la API apagada: aparece "Sin conexión en vivo" y el mensaje se agrega solo en local.

**Punto de control**: US-4 y SC-005 cumplidos.

---

## Fase 6: Cierre

**Propósito**: FR-003, FR-019, SC-006 y el cierre de la spec (CT-43, CT-44). Agente `5`.

- [ ] L2-T067 [5] Agregar `--with-angular` a `scripts/run.sh`:
  - comprueba Node 24.16 (`.nvmrc`);
  - corre `npm ci` si falta `node_modules`;
  - levanta `npm start` en `:4200` en segundo plano y lo apaga con el resto.

  Sin la opción, `run.sh` hace lo mismo que hoy. Funciona igual desde `./run.sh`. Verificar las dos formas.
- [ ] L2-T068 [P] [5] Escribir `PolyConecta.Web/README.md`: requisitos, scripts (`start`, `build`, `test`, `parity`, `scenarios`, `chatter`), puertos y reglas de paridad.
- [ ] L2-T069 [P] [5] Actualizar `docs/diseno/05-arquitectura-tecnica.md`: la capa `PolyConecta.Web` en Angular (`:4200`), el hub del chatter en `PolyConecta.Api` y el prototipo Blazor como referencia (D-60). Agregar `--with-angular` en la sección de comandos y en el `README.md` de la raíz.
- [ ] L2-T070 [5] Correr `npm run build`, `npm test`, `npm run parity`, `npm run scenarios` y `npm run chatter`, todos en verde (FR-019). Guardar `parity-report/` final y el resumen en el commit.
- [ ] L2-T071 [5] SC-006: `dotnet build Polyconecta.slnx` y `dotnet test Polyconecta.slnx` en verde, y `git diff main --stat -- PolyConecta.Presentation/` vacío.
- [ ] L2-T072 [5] Recorrer [quickstart.md](quickstart.md) completo en el navegador y anotar el resultado de cada sección en la exploración de la spec.
- [ ] L2-T073 [5] Cerrar la spec:
  - integrar en `docs/diseno/` lo que cambió, como el sistema de diseño de CT-24 y las colecciones de R-02;
  - actualizar `docs/ROADMAP.md`;
  - revisar que `bloqueos.md` no tenga nada abierto;
  - pedir la aprobación para hacer merge de `001-angular-presentation` a `main` (CT-44);
  - después del merge, borrar la carpeta de la spec y la rama.

---

## Dependencias y orden

### Entre fases

| Fase | Depende de | Bloquea |
| :--- | :--- | :--- |
| 0 · Arranque | Merge de la 002 a `main` (R-05) | Todo |
| 1 · Estado | Fase 0 | US1, US2, US3 |
| 2 · US1 | Fase 1 | US2 (las acciones van sobre las páginas) |
| 3 · US2 | US1 de cada agente (no hace falta esperar a los tres) | US3 de ese agente |
| 4 · US3 | US2 de cada agente y L2-T050 | Cierre |
| 5 · US4 | API en .NET 10 (0.3); en paralelo con la fase 1 | Cierre |
| 6 · Cierre | US1 a US4 | Merge a `main` |

### Dentro de cada fase

- **Fase 1:**
  - L2-T008 y L2-T009 van antes que los servicios.
  - L2-T015 va antes que L2-T011.
  - L2-T010 → L2-T011 → L2-T012.
  - Las pruebas L2-T016 a L2-T019 se escriben junto con su servicio.
- **US1:** L2-T023 → L2-T024. L2-T036 y L2-T037 al final.
- **US2:** L2-T047 → L2-T043, porque el flujo de la OF valida la recolección.
- **US4:** L2-T062 → L2-T063 → L2-T064 → L2-T065 → L2-T066.

### En paralelo

- **Fase 0:** L2-T004 y L2-T007 en paralelo con L2-T005 y L2-T006.
- **Fase 1:** L2-T013, L2-T015 y las cuatro pruebas.
- **US1 a US3:** los agentes 3A, 3B y 3C, cada uno en sus carpetas. Dentro de cada agente, las tareas marcadas [P].
- **US4:** toda la fase corre en paralelo con la fase 1.

```text
Agente 3A: L2-T021, L2-T022, L2-T025   (en paralelo; L2-T023 → L2-T024 aparte)
Agente 3B: L2-T026, L2-T028, L2-T029, L2-T030   (L2-T027 y L2-T031 aparte)
Agente 3C: L2-T032, L2-T033, L2-T034, L2-T035
```

## Estrategia

1. **MVP = US1.** Cuando las 19 rutas pasan la paridad, la réplica ya sirve como referencia visual para las pantallas de F1 en adelante (D-118). Se revisa con una persona antes de seguir.
2. **US2** completa la paridad de comportamiento.
3. **US3** agrega lo único nuevo, "Nuevo", sin tocar lo ya verificado.
4. **US4** corre desde el principio en paralelo y se integra antes de cerrar la fase 1.
5. Entre fases, una persona revisa `bloqueos.md` (regla de autonomía 6).
