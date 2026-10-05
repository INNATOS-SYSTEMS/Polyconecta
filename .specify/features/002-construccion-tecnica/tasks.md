# Tasks: Construcción técnica (F0)

> **Secciones por líder (CT-34, D-120).** Las tareas van en una sección **Común**, una **L1** y una **L2**, y dentro de cada una por tarea del plan. Los ids llevan la sección (`C-T001`, `L1-T001`, `L2-T001`). Cada líder solo edita la suya; la sección Común nombra a su responsable. Las tareas que nacen de la exploración se agregan con la referencia a su fila en "Exploración y cambios" de [spec.md](spec.md) (CT-43).

**Branch**: `002-construccion-tecnica` | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

**Status**: Tareas generadas el 2026-10-05. La spec sigue en borrador; si la ratificación cambia algo, se ajustan aquí.

**Tests**: la spec los pide (SC-002 a SC-004, SC-007; regla de autonomía 6). Cada tarea dice cómo se verifica.

## Format: `[ID] [P?] [US] Descripción`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin dependencias).
- **[US]**: historia de usuario de la spec (US1, US2…).
- Cada tarea nombra las rutas exactas que toca y cómo se verifica.

---

## Común · los dos líderes

### 0.2 · Acordar el intercambio PolyConecta ↔ conector: comandos, datos, respuestas (5 – 6 oct, 15 h)

Responsables: Alejandro Ponce y Luis Alvarado Martinez. En cada sección del contrato, uno redacta (R) y el otro revisa (V), como indica `bridge-v1.md`.

- [ ] C-T001 [US1] **Sesión 1, secciones 2 a 4** de `docs/contratos/bridge-v1.md`:
  - aceptar o corregir cada ✏️ del sobre, los estados y el callback;
  - decidir la derivación de la referencia de 20 caracteres (propuesta en research R-06);
  - fijar el formato de la firma `X-Bridge-Signature`;
  - cerrar el catálogo de errores, incluido `SDK_ERROR`.

  Hecha cuando las tres secciones no tienen ✏️ ni ❓.
- [ ] C-T002 [US1] **Sesión 2, sección 5**: completar la ficha de los cinco comandos (`ALTA_ALMACEN`, `TRASPASO`, `ALTA_PEDIDO`, `CIERRE_PRODUCCION` y `REMISION`). Cada uno lleva carga, resultado, errores y traducción al SDK, citando su decisión o prueba (FR-002, Principio VII). Aplicar D-127: la unidad de la línea es la unidad base del producto, y `UNIDAD_NO_ADMITIDA` cambia de sentido.
- [ ] C-T003 [US1] **Decidir T-17**: quién aplica la regla de costo de D-79 en `TRASPASO` y `CIERRE_PRODUCCION`, y si el resultado devuelve el costo.
  - **Si se decide:** registrarlo en `docs/diseno/decisiones.md` y quitar T-17 de `docs/diseno/preguntas-abiertas.md`.
  - **Si no:** dejar `costo` como campo opcional del resultado (research R-06) y T-17 abierta.
- [ ] C-T004 [US1] **Sección 6 (lecturas) y sección 7 (fuera del contrato)**:
  - paginación con `limit` y `cursor`, `modified_since` y `snake_case`;
  - la unidad base y el uso de lote en `catalogs/products`;
  - varios productos en `inventory/stocks`;
  - `inventory/purchases` (FR-004);
  - decidir si `concepts` e `invoices` quedan fuera de `1.0`.
- [ ] C-T005 [P] [US1] Completar `docs/contratos/ejemplos/` con una carga válida y una inválida por comando, nombradas `<comando>.valido.json` y `<comando>.invalido.<motivo>.json`, más los callbacks `confirmado`, `error` y `dead-letter`. Cada ejemplo debe cumplir lo que dice la ficha de su comando.
- [ ] C-T006 [US1] Firmar `1.0`: las dos firmas con fecha en la tabla del encabezado, el estado en ✅ y la fila correspondiente en "Exploración y cambios". SC-001: a más tardar el 6 de octubre.
- [ ] C-T007 [US1] Escribir `docs/contratos/bridge-v1.openapi.yaml` a partir del contrato firmado y validarlo con `npx @redocly/cli lint docs/contratos/bridge-v1.openapi.yaml`, sin errores. La herramienta se anota en "Exploración y cambios" (regla de autonomía 5).

### Versiones centralizadas (primera tarea .NET de la fase)

Responsable: Luis Alvarado Martinez; revisa Alejandro Ponce.

- [ ] C-T008 [US2] Crear `global.json` (SDK 10.0.300, `rollForward: latestPatch`) y `Directory.Packages.props` con `ManagePackageVersionsCentrally` y las versiones exactas de research R-01, incluidas las del bridge. Quitar los atributos `Version` de todos los `.csproj`. Verificar con `dotnet restore Polyconecta.slnx` y `grep -rn 'Version=' --include=*.csproj .`, que no debe devolver nada.

### Suite de contrato (FR-006, CT-23)

Responsable: Alejandro Ponce; revisa Luis Alvarado Martinez. Empieza cuando el contrato está firmado (C-T006).

- [ ] C-T009 [US3] Crear `tests/PolyConecta.Contract.Tests/` como proyecto xUnit v3 **sin referencias a otros proyectos**:
  - lee `BRIDGE_URL`, `BRIDGE_CALLBACK_SECRET` y `CALLBACK_HOST` de variables de entorno (research R-09);
  - incluye un receptor de callbacks (un `WebApplication` en un puerto libre) que verifica la firma y expone los callbacks recibidos a las pruebas;
  - se agrega a `Polyconecta.slnx`.
- [ ] C-T010 [P] [US3] Una clase de pruebas por comando en `tests/PolyConecta.Contract.Tests/Comandos/`. Cada una prueba:
  - la carga válida: `202`, y luego un callback `CONFIRMED` con folio e `id_erp`;
  - la carga inválida: el código de error del contrato;
  - el reenvío con la misma `idempotency_key`: `is_duplicate: true` y el mismo folio.

  Toma los ejemplos de `docs/contratos/ejemplos/`.
- [ ] C-T011 [P] [US3] `tests/PolyConecta.Contract.Tests/Sobre/` prueba:
  - `contract_version` incompatible da `VERSION_NO_SOPORTADA`;
  - faltan campos obligatorios: `400` con el modelo de errores;
  - `GET /api/v1/transactions/{id}` devuelve lo mismo que el callback;
  - las lecturas de la sección 6 cumplen su forma y paginación.
- [ ] C-T012 [P] [US3] `tests/PolyConecta.Contract.Tests/Simulado/` agrupa las pruebas con el rasgo `Simulado`, que se omiten contra el bridge real. Cubren error forzado, demora y callback perdido (recuperado por `GET /transactions/{id}`), usando `PUT /admin/simulated/faults`.

---

## L1 · Camino 1 · Alejandro Ponce

### 0.1 · Rotar credenciales de CONTPAQi y crear el usuario de solo lectura del conector (5 oct, 4 h)

- [ ] L1-T001 [US8] Crear `tools/sdk-lab/scripts/New-BridgeReadOnlyLogin.sql`. Recibe el nombre del login y la contraseña como parámetros de `sqlcmd` (`$(Login)`, `$(Password)`), nunca escritos en el archivo. Crea el login y le da `db_datareader` en `CompacWAdmin` y en las bases de empresa que lee el bridge. Revisar el archivo con `git diff`: no debe contener ningún secreto.
- [ ] L1-T002 [US8] En el VPS:
  - crear el login con el script;
  - rotar la contraseña de `sa` con Sistemas;
  - cambiar `BridgeConfig__SqlConnectionString` del entorno del administrador al login de solo lectura.

  Verificar que la contraseña anterior de `sa` falla y que `GET /api/v1/catalogs/warehouses` responde. Guardar la evidencia, sin secretos, en `tools/sdk-lab/evidence/F0/0.1.md`.

### 0.4 · Migrar el conector a .NET 10 y verificarlo contra el laboratorio (6 – 7 oct, 8 h)

- [ ] L1-T003 [US5] Pasar `tools/sdk-lab/src/SdkLab.csproj` a `net10.0` (`win-x86`, `x86`), con `Microsoft.Data.SqlClient` 7.1.1. Ajustar `Encrypt` y `TrustServerCertificate` en su configuración (research R-01). Generar el paquete con `tools/sdk-lab/scripts/Build-Package.sh`. Verificar con `dotnet publish -r win-x86` sin errores.
- [ ] L1-T004 [US5] Pasar `PolyConecta.Contpaq/PolyConecta.Contpaq.csproj` a `net10.0`:
  - `RuntimeIdentifier` y `PlatformTarget` solo al publicar para Windows (plan, L1);
  - adaptar `Program.cs` a Swashbuckle 10 y Scalar 2;
  - `tests/Contpaq.Bridge.Tests` a xUnit v3 y AwesomeAssertions.

  Verificar con `dotnet build` y `dotnet test tests/Contpaq.Bridge.Tests`.
- [ ] L1-T005 [US5] En el VPS, correr los bloques F y G de la matriz con `sdk-lab` en .NET 10 contra `_LAB` y comparar con la evidencia del 30 de septiembre en `tools/sdk-lab/evidence/`. Cada diferencia va a "Exploración y cambios". **Si F o G fallan**, el bridge vuelve temporalmente a .NET 8 y se registra (D-67). Evidencia: `tools/sdk-lab/evidence/F0/0.4.md`.
- [ ] L1-T006 [US5] Publicar el bridge .NET 10 `win-x86` en el VPS. Comprobar que arranca en modo `Real`, abre la sesión del SDK y responde `/health` con `mode: "Real"` y `sdk_initialized: true`. Evidencia en el mismo archivo.

### 0.7 · CONTPAQi simulado para probar PolyConecta sin el servidor real (7 – 9 oct, 16 h)

Empieza cuando el contrato está firmado (C-T006).

- [ ] L1-T007 [US3] Crear `PolyConecta.Contpaq/Core/Contract/` con el sobre `1.0`, los modelos de carga y resultado de los cinco comandos y el modelo de errores, con sus códigos como constantes, todo según el contrato firmado. En `Api/Controllers/TransactionsController.cs`:
  - exigir `contract_version` 1.x, `idempotency_key`, `correlation_id` y `callback_url`;
  - rechazar `DOCUMENT_CREATE`;
  - responder `400` con el modelo de errores.

  Verificar con pruebas en `tests/Contpaq.Bridge.Tests/Contract/`.
- [ ] L1-T008 [US3] Separar el ciclo del outbox:
  - **`Infrastructure/Outbox/OutboxWorker.cs`** (`BackgroundService`) toma la transacción pendiente, valida, llama a `ISdkGateway`, guarda el resultado y despacha el callback.
  - **`ContpaqiSdkGateway`** deja de ser `BackgroundService` y conserva la sesión y las llamadas al SDK. Se borran `_forceMockMode` y los `if` del modo simulado embebido.
  - **`DbInitializer`:** agregar las columnas `contract_version`, `variant` y `result_json`, y renombrar los estados a `CONFIRMED`, `FAILED` y `DEAD_LETTER`, migrando las filas existentes (data-model §2).
- [ ] L1-T009 [US3] Crear `IReadRepository`, con `SqlReadRepository` (real) y `SimulatedReadRepository`. El simulado lee `PolyConecta.Contpaq/Simulated/seed.json`: productos con unidad base y si llevan lote, almacenes, clientes y existencias por lote, tomados de `_LAB` (research R-05). La ruta sale de `BridgeConfig__Simulated__SeedPath`.
- [ ] L1-T010 [US3] Crear `PolyConecta.Contpaq/Core/Validation/` con las validaciones de CT-39 y D-127, que corren igual en los dos modos contra `IReadRepository`:
  - producto existe y está activo;
  - la unidad es la base;
  - almacenes y cliente existen;
  - `Σ lotes = cantidad`;
  - hay existencia por lote en el origen.

  Probar cada regla en `tests/Contpaq.Bridge.Tests/Validation/`.
- [ ] L1-T011 [US3] Crear `Infrastructure/Sdk/SimulatedSdkGateway.cs`:
  - asigna folios por concepto con la tabla `simulated_folio`;
  - devuelve `documentos[]` según el comando, por ejemplo salida y entrada en `TRASPASO`;
  - un reenvío con la misma `idempotency_key` devuelve el resultado guardado.

  Probarlo en `tests/Contpaq.Bridge.Tests/Simulated/`.
- [ ] L1-T012 [US3] Agregar `BridgeConfig__Mode` en `Program.cs`:
  - por omisión, `Real` en Windows y `Simulated` en otro sistema;
  - en `Simulated` no se exige `BridgeConfig__SqlConnectionString` ni se precarga la DLL;
  - `/health` informa `mode` y el estado real de la sesión y de SQL.

  Verificar que `BridgeConfig__Mode=Simulated dotnet run --project PolyConecta.Contpaq` arranca en macOS.
- [ ] L1-T013 [US3] Configurar los fallos simulados (FR-008):
  - las reglas en `BridgeConfig__Simulated__Faults`, por `command_type` y, opcionalmente, por `referencia_negocio`, con `error_code`, `delay_ms` y `drop_callback`;
  - la ruta `PUT /admin/simulated/faults`, que solo se registra en modo simulado.

  Probarlo en `tests/Contpaq.Bridge.Tests/Simulated/`.
- [ ] L1-T014 [US3] Firmar los callbacks en `Infrastructure/Webhooks/WebhookDispatcher.cs`:
  - la cabecera `X-Bridge-Signature` con HMAC-SHA256 y el secreto de `BridgeConfig__CallbackSecret`;
  - reintentos con espera creciente, registrados en la tabla `callback_attempts`;
  - `GET /api/v1/transactions/{id}` devuelve el mismo cuerpo que el callback.
- [ ] L1-T015 [US3] Correr la suite de contrato completa contra el simulador en macOS (`BRIDGE_URL=http://localhost:5005 dotnet test tests/PolyConecta.Contract.Tests`). Debe pasar al 100 % (SC-002).

### 0.9 · Inicio de sesión automático del administrador y arranque del conector en el servidor (9 oct, 4 h)

- [ ] L1-T016 [US8] Crear `tools/sdk-lab/scripts/Set-BridgeAutostart.ps1`, que configura dos cosas en el VPS:
  - el inicio de sesión automático del administrador, con el mecanismo probado en S-04;
  - la tarea programada "al iniciar sesión", que levanta el bridge con sus variables de entorno.

  El script no contiene secretos; la contraseña se pide al correrlo.
- [ ] L1-T017 [US8] Revertir el usuario de prueba `polyconecta-bridge` en el VPS (A-19) y dejarlo registrado en `tools/sdk-lab/evidence/F0/0.9.md`.
- [ ] L1-T018 [US8] Reiniciar el VPS sin intervención y medir. En menos de 5 minutos, `GET /health` debe responder y `GET /api/v1/catalogs/warehouses` debe leer con el login de solo lectura (SC-006). La evidencia, con la hora de reinicio y la de respuesta, va en `0.9.md`.

---

## L2 · Camino 2 · Luis Alvarado Martinez

### 0.3 · Migrar la solución a .NET 10 con SQL Server y la capa de casos de uso (5 – 6 oct, 7 h)

Empieza después de C-T008.

- [ ] L2-T001 [US2] Pasar `PolyConecta.Domain`, `PolyConecta.Infrastructure`, `PolyConecta.Api` y los proyectos de `tests/` (salvo `Contpaq.Bridge.Tests`, que es de L1) a `net10.0`. `PolyConecta.Presentation` se queda en `net8.0` (exploración, excepción a CT-04). Verificar con `dotnet build Polyconecta.slnx` sin advertencias de versión.
- [ ] L2-T002 [US2] En `PolyConecta.Infrastructure`:
  - quitar Npgsql, MediatR, FluentValidation y EF InMemory;
  - agregar `Microsoft.EntityFrameworkCore.SqlServer` y `.Design`;
  - `PolyDbContext` con SQL Server y el esquema de cada configuración (CT-12, research R-03).

  Verificar con `dotnet build` y `dotnet list package`, que ya no debe mostrar los paquetes quitados.
- [ ] L2-T003 [US2] Crear `PolyConecta.Application/` y agregarlo a `Polyconecta.slnx`, con referencia a `Domain`:
  - en `Common/`: `IUseCase<TRequest, TResult>`, `LoggingDecorator`, `TransactionDecorator`, `ValidationDecorator`, `IValidator<T>`, `IUnitOfWork`, `ICurrentUser` (en F0 devuelve `"sistema"`) e `IClock`;
  - `AddApplication()` registra los decoradores a mano (research R-02).

  Agregar las pruebas del orden de los decoradores en `tests/PolyConecta.Application.Tests/Common/`.
- [ ] L2-T004 [US2] En `PolyConecta.Api/Program.cs`:
  - quitar `UseInMemoryDatabase` y `EnsureCreated`;
  - registrar SQL Server con `ConnectionStrings__PolyConecta`, `AddApplication()` y la infraestructura;
  - adaptar Swashbuckle 10.

  Verificar que la API arranca contra una base migrada con el login `polyconecta_app`.
- [ ] L2-T005 [US2] Crear `scripts/sql/logins-desarrollo.sql`, con los logins `polyconecta_app` (lectura, escritura y `EXECUTE`) y `polyconecta_migraciones` (`db_owner`) y las contraseñas como parámetros de `sqlcmd`. Generar la migración `F0_Base` en `PolyConecta.Infrastructure/Persistence/Migrations/`. Verificar con `dotnet ef database update` sobre un SQL Server 2022 vacío, usando `ConnectionStrings__PolyConectaMigraciones` (US-2, escenario 2).
- [ ] L2-T006 [US2] Pasar las pruebas a xUnit v3 y AwesomeAssertions:
  - `tests/PolyConecta.Domain.Tests` (8 pruebas);
  - `tests/PolyConecta.IntegrationTests` (6), ahora contra SQL Server con Testcontainers: un contenedor por corrida (`AssemblyFixture`) y una base migrada por clase (research R-07);
  - crear `tests/PolyConecta.Application.Tests` con la misma infraestructura.

  Verificar que las 14 pruebas de L2 pasan con `dotnet test`. Si alguna se reemplaza, anotarlo en la exploración.
- [ ] L2-T007 [US2] En `scripts/run.sh`:
  - aceptar el runtime 10 para la API y el bridge, y exigir también el 8 para el prototipo;
  - quitar la suposición de EF InMemory.

  Verificar que `./run.sh` levanta todo y que Blazor sigue en `:9000`.

### 0.5 · Crear la aplicación web: estructura, navegación y estilo (6 – 8 oct, 9 h)

Son las fases 0 y 2A de la spec 001, ajustadas por su research R-04.

- [ ] L2-T008 [US6] Crear `PolyConecta.Web/` con `npx @angular/cli@22.2.1 new`:
  - componentes standalone;
  - versiones exactas en `package.json`, sin `^` ni `~` (CT-36);
  - `.nvmrc` con 24.16.0;
  - `typescript` 6.0.3.

  Verificar con `npm ci && npm run build`.
- [ ] L2-T009 [US6] Estilos:
  - copiar `PolyConecta.Presentation/wwwroot/css/app.css` sin cambios a `PolyConecta.Web/src/styles/app.css` (`diff` vacío);
  - en `src/index.html`, cargar Inter, Bootstrap 5.3.2 y Bootstrap Icons 1.11.3 por las mismas URL que `App.razor`, y aplicar el mismo `font-family` al `body`.
- [ ] L2-T010 [US6] Crear el layout:
  - `MainLayout` y `OdooTopbar` en `PolyConecta.Web/src/app/shared/`, con los menús de `Components/Shell/OdooTopbar.razor` (Ventas, Inventario con Operaciones, Fabricación y Calidad);
  - en `app.routes.ts`, una ruta y una página vacía por módulo, en `src/app/features/<modulo>/` (CT-09).
- [ ] L2-T011 [US6] Crear en `PolyConecta.Web/src/app/core/models/` los tipos que usan los componentes (`ProductRef`, `LotBalance`, `LotAllocation`, `StockOperationLine` y `ProductionLot`, según el data-model de la spec 001) y `src/app/core/state/ui-view-state.ts`.
- [ ] L2-T012 [P] [US6] Crear en `PolyConecta.Web/src/app/shared/` los otros 10 componentes, con las mismas entradas, salidas y marcado que su `.razor`: `OdooBreadcrumb`, `OdooSearchPanel`, `OdooViewSwitcher`, `OdooPager`, `OdooSmartButtons`, `OdooStatusPipeline`, `OdooLineCapture`, `OdooChatterDrawer`, `LotPickerModal` y `LotQuantityPickerModal`. `PocSalesOrderForm` no va aquí: queda en la spec 001.
- [ ] L2-T013 [US6] Instalar `vitest` 5.0.3 y `jsdom` 30.1.2 exactos y configurar `ng test` sin modo observador. Escribir una prueba de render por componente en su `.spec.ts`. Verificar con `npm test`.
- [ ] L2-T014 [US6] Comprobar en el navegador el layout, la tipografía, los colores y la barra superior contra el prototipo en `:9000` (US-6, escenario 1). Guardar las capturas lado a lado en el PR.

### 0.6 · Base común: auditoría, bitácora de estados, folios y cola de envíos a CONTPAQi (7 – 9 oct, 11 h)

El modelo está en [data-model.md](data-model.md). Las piezas del contrato empiezan cuando está firmado (C-T006).

- [ ] L2-T015 [US4] Crear en `PolyConecta.Domain/Common/` `AuditableEntity`, `ArchivableEntity` (con `Archive()` y `Restore()`), `IStatefulDocument<TState>`, `TransicionInvalidaException` y `SyncState`, con las transiciones de data-model §1. Borrar `Domain/ValueObjects/Folio.cs` y sus usos. Pruebas en `tests/PolyConecta.Domain.Tests/Common/`: las transiciones válidas e inválidas del `SyncState`, el archivado, y que un callback sobre algo `Confirmado` no cambia nada.
- [ ] L2-T016 [US4] Crear en `PolyConecta.Domain/Plataforma/` `StateTransitionLog`, `ReferenceSequence` y el nuevo `OutboxMessage`, y borrar `Domain/Entities/OutboxMessage.cs`.
- [ ] L2-T017 [US4] En `PolyConecta.Infrastructure`:
  - las configuraciones del esquema `plt`;
  - un interceptor de `SaveChanges` que llena la auditoría con `IClock` e `ICurrentUser` y convierte los eventos de transición en `StateTransitionLog`;
  - el filtro global de archivado.

  Generar la migración `F0_Plataforma`. Pruebas en `tests/PolyConecta.Application.Tests/Plataforma/`: quién y cuándo, archivar sin borrar, y transición registrada con origen, destino, usuario, fecha y nota (US-4, escenarios 1 y 2).
- [ ] L2-T018 [US4] Crear `IReferenceSequenceService` en `Application/Plataforma/Folios/` y su implementación en `Infrastructure/Plataforma/`, con `UPDLOCK, ROWLOCK` en la transacción, prefijo con `{yyyy}`, relleno y reinicio anual o mensual. Probarlo: 50 peticiones en paralelo dan 50 folios distintos y consecutivos; dos tipos usan secuencias independientes; un tipo inexistente da error (escenario 6).
- [ ] L2-T019 [US4] Crear `IBridgeSyncService.EncolarAsync(documento, comando, variante, carga, transición)` en `Application/Plataforma/Erp/`, con su implementación:
  - escribe el `OutboxMessage` en el mismo `DbContext`;
  - arma la `idempotency_key` (`{tipo}:{id}:{transición}`), el `correlation_id` y las `LockKeys` de la carga;
  - pasa el `SyncState` a `Pendiente`.

  Probar que, si falla el guardado, no queda ningún mensaje (escenario 3, CT-20).
- [ ] L2-T020 [US4] Crear en `PolyConecta.Infrastructure/Erp/`:
  - `BridgeHttpClient`, que arma el sobre del contrato y propaga el `correlation_id` (CT-31);
  - `ICommandPayloadTranslator` y su implementación de `TRASPASO` para el documento de prueba;
  - la verificación de la firma (`BridgeSignature`).

  Probar la firma con los vectores de `docs/contratos/ejemplos/`.
- [ ] L2-T021 [US4] Crear `PolyConecta.Infrastructure/Erp/BridgeDispatcher.cs` según research R-04:
  - orden por `Sequence`;
  - espera por llave y paso a `Bloqueado` (D-95);
  - reintentos de red con espera de 2ⁿ, hasta 5 intentos configurables;
  - consulta de `GET /transactions/{id}` si el callback no llega en `Erp__CallbackTimeout`;
  - un solo despachador con `sp_getapplock`.

  Probarlo con un bridge falso en `tests/PolyConecta.Application.Tests/Erp/`: tres comandos salen en orden (escenario 4), un `Error` solo bloquea a los que comparten llave, y al agotar los reintentos el documento queda en `Error` (escenario 5).
- [ ] L2-T022 [US4] Crear los casos de uso `ConfirmarSincronizacion` y `ReintentarSincronizacion` en `Application/Plataforma/Erp/`, y `PolyConecta.Api/Controllers/Plataforma/BridgeCallbackController.cs` según [contracts/callback-api.md](contracts/callback-api.md). Probar en `tests/PolyConecta.IntegrationTests/BridgeCallbackTests.cs`:
  - firma válida, inválida y vencida;
  - callback repetido;
  - `FAILED` con bloqueo de llaves;
  - reintentar un `Error`.
- [ ] L2-T023 [US4] Crear `DocumentoDePrueba` en `tests/PolyConecta.Application.Tests/Ciclo/`: hereda los mixins, tiene dos estados y encola un `TRASPASO` al confirmar. La prueba `CicloCompleto`:
  1. lee `BRIDGE_URL`;
  2. crea el documento con folio y confirma la transición;
  3. espera el callback y comprueba el folio y el id ERP en `erp_*` y `Confirmado`;
  4. con un fallo forzado, comprueba `Error` y el reintento hasta `Confirmado`.

  En CI corre en el trabajo `contrato` (SC-004).

### 0.8 · Integración continua con pruebas y SQL Server (8 – 9 oct, 7 h)

- [ ] L2-T024 [US7] Crear `.github/workflows/ci.yml` según research R-08, con tres trabajos:
  - **`dotnet`:** build y pruebas de dominio, aplicación e integración con Testcontainers;
  - **`contrato`:** publica el bridge, lo levanta con `BridgeConfig__Mode=Simulated` y corre `tests/PolyConecta.Contract.Tests` y `CicloCompleto`;
  - **`web`:** `npm ci`, build y pruebas en `PolyConecta.Web`.

  Secretos y contraseñas de prueba se generan en el propio trabajo; ninguno queda en el archivo (CT-29). Verificar con un push a la rama: los tres trabajos en verde.
- [ ] L2-T025 [US7] Pedir a un administrador del repositorio que proteja `main`, con PR obligatorio y los checks `dotnet`, `contrato` y `web` requeridos. Puede hacerse con `gh api repos/INNATOS-SYSTEMS/Polyconecta/branches/main/protection`. Verificar con un PR que tenga una prueba rota a propósito: queda en rojo y no se puede integrar; al corregirla, en verde (SC-007).
- [ ] L2-T026 [P] [US2] Actualizar `docs/diseno/05-arquitectura-tecnica.md` §3 a §5 con la nueva solución: proyectos, .NET 10, SQL Server, Application, bridge con modo simulado, CI y cómo correrlo. Marcar resuelta la deuda 1, 2 y 5 de §4 (SC-008). Actualizar el `README.md` de la raíz.

---

## Cierre de la fase (los dos líderes)

- [ ] C-T013 Correr [quickstart.md](quickstart.md) completo y anotar el resultado de cada sección en "Exploración y cambios".
- [ ] C-T014 Cerrar la spec:
  - integrar en `docs/diseno/` lo que cambió (contrato, base común, decisiones de research);
  - actualizar el tablero de `docs/ROADMAP.md` con la fecha y el commit (CT-35);
  - abrir el PR de `002-construccion-tecnica` a `main` y hacer el merge con la CI en verde (CT-27, CT-44);
  - borrar la carpeta de la spec y la rama.

  La spec 001 espera este merge (su research R-05).

---

## Dependencias y orden

| Tarea del plan | Depende de | Bloquea |
| :--- | :--- | :--- |
| 0.2 (C-T001 a C-T007) | — | 0.7, la suite, y las piezas del contrato en 0.6 (L2-T019 a L2-T023) |
| C-T008 (versiones) | — | 0.3 y 0.4 |
| 0.1 (L1-T001, L1-T002) | VPS y Sistemas | 0.9 |
| 0.3 (L2-T001 a L2-T007) | C-T008 | 0.6, 0.8 y la fase 2B de la spec 001 |
| 0.4 (L1-T003 a L1-T006) | C-T008, VPS | 0.9 |
| 0.5 (L2-T008 a L2-T014) | — (no depende de .NET) | Spec 001 |
| 0.6 (L2-T015 a L2-T023) | 0.3; contrato firmado para L2-T019 en adelante; simulador para L2-T023 | 0.8 (`CicloCompleto`) |
| 0.7 (L1-T007 a L1-T015) y suite (C-T009 a C-T012) | Contrato firmado, L1-T004 | 0.8, L2-T023 |
| 0.8 (L2-T024 a L2-T026) | 0.3, 0.5, 0.7 y la suite | Cierre |
| 0.9 (L1-T016 a L1-T018) | 0.1, 0.4 | Cierre |

### En paralelo

- **5 de octubre:** la sesión 1 del contrato (C-T001), C-T008 y luego 0.3, y 0.1 en el VPS.
- **6 de octubre:** la sesión 2 (C-T002 a C-T006), 0.4 en el VPS y 0.5, que no depende de nada .NET.
- **Dentro de 0.5:** L2-T012, componentes en archivos distintos.
- **Dentro de 0.6:** L2-T015 a L2-T018 no dependen del contrato y avanzan mientras se firma.
- **Dentro de la suite:** C-T010 a C-T012.

## Estrategia

1. **Primero el contrato y las versiones.** Son lo único que bloquea a los dos caminos.
2. **El primer resultado verificable es US-2** (.NET 10 y SQL Server), que no depende del contrato.
3. **Con el contrato firmado,** 0.7 y las piezas de 0.6 que lo usan avanzan en paralelo, y se encuentran en `CicloCompleto` (SC-004).
4. **La CI se monta al final**, cuando hay algo que correr, y desde ahí protege `main`.
5. **Lo del VPS** (0.1, 0.4 y 0.9) avanza en paralelo y se cierra con su evidencia.
