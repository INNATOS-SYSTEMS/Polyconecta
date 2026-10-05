# Implementation Plan: Construcción técnica (F0)

> **Secciones por líder (CT-34, D-120).** Lo común va primero y lo acuerdan los dos líderes. Después, la sección **L1** la edita solo Alejandro Ponce y la **L2** solo Luis Alvarado Martinez.

**Branch**: `002-construccion-tecnica` | **Date**: 2026-10-05 | **Spec**: [spec.md](spec.md)

**Status**: Plan completo. **La spec sigue en borrador**: este plan se escribió antes de que la firmen los dos líderes y se ajusta si la ratificación cambia algo.

---

## Común

### Summary

F0 deja lista la base sobre la que construyen las fases F1 a F8:

- el contrato `bridge-v1` firmado y su suite de pruebas por HTTP;
- la solución en .NET 10 con SQL Server 2022 y un proyecto `PolyConecta.Application`;
- la base común de PolyConecta: mixins, bitácora de estados, folios, outbox transaccional, despachador y callback;
- el bridge con un modo simulado que corre en macOS y en CI;
- la aplicación Angular base;
- la CI en GitHub Actions;
- el servidor del conector seguro y con arranque automático.

**Enfoque técnico:**
- El contrato se cierra primero (0.2), y cada camino trabaja contra él.
- En el bridge, el ciclo del outbox se separa del gateway del SDK. Así el modo simulado cambia solo los adaptadores de escritura y lectura (D-122).
- En PolyConecta, cada caso de uso es un servicio con decoradores propios (D-72). El outbox se escribe en la misma transacción que el negocio, y un despachador en `Infrastructure/Erp/` envía los comandos de uno en uno (CT-41).

Los detalles y las alternativas están en [research.md](research.md).

### Technical Context

**Language/Version**: .NET 10 (SDK 10.0.300, fijado en `global.json`) y C#. Angular 22.2.1 con TypeScript 6.0.3 y Node 24.16.0 (CT-04, D-67 a D-69).

**Primary Dependencies**: ASP.NET Core 10, EF Core 10.0.12 con el proveedor de SQL Server, SignalR, `Microsoft.Data.SqlClient` 7.1.1 y `Microsoft.Data.Sqlite` 10.0.12. En el bridge: Dapper 2.1.89, Serilog.AspNetCore 10.0.0 y `MGWServicios.dll` en modo real. Todo exacto en `Directory.Packages.props` (CT-36, research R-01).

**Storage**: SQL Server 2022 para PolyConecta (CT-05), con un esquema por módulo; en F0 solo se crea `plt` y lo que ya existe (research R-03). SQLite para el outbox del bridge.

**Testing**:
- xUnit v3 4.0.1 con AwesomeAssertions 9.6.0 (D-71, D-72).
- Pruebas de aplicación contra SQL Server 2022 en contenedor (Testcontainers, research R-07).
- La suite de contrato por HTTP (CT-23).
- Las pruebas de Angular con Vitest.

**Target Platform**:
- API y web: macOS y Linux en desarrollo y CI. Producción por definir (H-01).
- Bridge real: Windows `win-x86` en el VPS de CONTPAQi (D-115).
- Bridge simulado: cualquier sistema.

**Project Type**: servicio web (API), SPA y un proceso puente (bridge).

**Performance Goals**: lo que piden CT-41 y el SC-006:
- el despachador manda un movimiento en segundos;
- el bridge vuelve en menos de 5 minutos tras reiniciar el VPS.

**Constraints**:
- Nada de Npgsql, EF InMemory en producción ni MediatR (D-72).
- Ningún secreto en el repositorio (CT-29).
- La API se conecta con un login sin DDL y las migraciones con otro (CT-30).
- El bridge real solo carga la DLL en Windows y en modo `Real`.

**Scale/Scope**: 9 tareas del plan (0.1 a 0.9), 8 historias, 5 comandos del contrato y 7 proyectos .NET más uno nuevo de aplicación, uno de pruebas de aplicación y uno de suite de contrato.

### Constitution Check

*Se revisa antes de investigar y otra vez después del diseño.*

| Principio o regla | Cómo se cumple | Antes | Después |
| :--- | :--- | :---: | :---: |
| I · CONTPAQi es el sistema de registro | PolyConecta guarda en sus columnas `erp_*` el folio y el id que confirma el bridge (FR-020) | ✅ | ✅ |
| II · Toda escritura a CONTPAQi va por outbox y bridge | Outbox transaccional en la API (FR-018), despachador (FR-019) y bridge único usuario del SDK | ✅ | ✅ |
| VII · Respaldo técnico | Cada comando del contrato cita su decisión o prueba de la matriz (FR-002). T-17 sigue abierta y se resuelve en la sesión | ⚠️ | ⚠️ |
| VIII · Realidad de CONTPAQi | El simulador valida con las mismas reglas que el real (CT-39), y su catálogo semilla sale de la empresa `_LAB` (research R-05) | ✅ | ✅ |
| X · Documentos libres | No aplica a F0: no hay documentos de negocio. La base común no exige origen | ✅ | ✅ |
| CT-04 · Una sola versión de .NET | Todos en .NET 10. El bridge migra después de que pasen F y G en el laboratorio (D-67); si fallan, se queda en .NET 8 y se registra | ✅ | ✅ |
| CT-06 · Esquema solo por migraciones | `dotnet ef migrations` versionadas; en CI la base se crea desde cero | ✅ | ✅ |
| CT-07, CT-08 · Capas y casos de uso | Nuevo `PolyConecta.Application`; los controladores solo traducen | ✅ | ✅ |
| CT-20 · Outbox en la misma transacción | Decorador de transacción alrededor del caso de uso; el outbox va en el mismo `DbContext` | ✅ | ✅ |
| CT-21, D-122 · Simulador = mismo bridge | `BridgeConfig__Mode=Simulated\|Real` cambia dos adaptadores | ✅ | ✅ |
| CT-23 · Suite de contrato | `tests/PolyConecta.Contract.Tests`, solo HTTP | ✅ | ✅ |
| CT-27 · CI | GitHub Actions; la protección de `main` exige el check (research R-08) | ✅ | ✅ |
| CT-29, CT-30 · Secretos y privilegios | Variables de entorno y `user-secrets`; logins separados | ✅ | ✅ |
| CT-36 · Versiones exactas | `global.json`, `Directory.Packages.props`, `.nvmrc` y `package.json` exactos | ✅ | ✅ |
| CT-43 · Exploración dentro de la spec | Los cambios del plan están en "Exploración y cambios" | ✅ | ✅ |
| CT-44 · Una rama por spec | Los dos líderes trabajan en `002-construccion-tecnica`; la regla de autonomía 1 se alinea (research R-10) | ⚠️ | ✅ |

**Lo que queda en ⚠️**: T-17 no se puede cerrar con lo que hay en `docs/contpaq/`. Es una decisión de los líderes en la sesión del contrato, con la regla de costo de D-79 ya fijada. No bloquea el plan: el contrato puede firmarse con `TRASPASO` y `CIERRE_PRODUCCION` completos salvo el campo de costo, que se agrega como opcional (cambio compatible, CT-22).

### Contrato

El contrato vive en [docs/contratos/bridge-v1.md](../../../docs/contratos/bridge-v1.md) y no se duplica aquí. Esta fase lo cierra (0.2) y lo usa completo. Lo que el plan propone llevar a la sesión de los líderes:

| Punto | Propuesta | Fuente |
| :--- | :--- | :--- |
| Unidad de la línea | Con D-127, la unidad de cada línea es la **unidad base** del producto en CONTPAQi. `UNIDAD_NO_ADMITIDA` pasa a "la unidad no es la base del producto" | D-127 |
| Referencia de reconciliación (§2 ❓) | Los primeros 20 caracteres de `base32(SHA-256(idempotency_key))` | research R-06 |
| `SDK_ERROR` (§4 ❓) | `retryable: false` por omisión. El bridge marca reintentables solo los códigos que la matriz demostró transitorios | research R-06 |
| Lecturas `concepts` e `invoices` (§6 ❓) | Fuera del contrato `1.0`, en la lista de rutas de operación (§7) | research R-06 |
| Costo (T-17) | Campo opcional en el resultado; la regla la decide la sesión | research R-06 |

### Project Structure

#### Documentación (esta spec)

```text
.specify/features/002-construccion-tecnica/
├── spec.md          # común
├── plan.md          # este archivo: Común, L1, L2
├── research.md      # decisiones R-01 a R-11
├── data-model.md    # base común de PolyConecta y tablas nuevas del bridge
├── quickstart.md    # cómo verificar F0
├── contracts/
│   └── callback-api.md   # endpoint de PolyConecta que recibe el callback
└── tasks.md         # Común, L1, L2
```

El contrato del bridge no está en `contracts/`: vive en `docs/contratos/` porque lo comparten todas las fases.

#### Código

```text
global.json                               # SDK 10.0.300 (común)
Directory.Packages.props                  # versiones NuGet exactas (común)
Directory.Build.props                     # ya existe

docs/contratos/
├── bridge-v1.md                          # 0.2 · firmado
├── bridge-v1.openapi.yaml                # 0.2 · al final
└── ejemplos/                             # una carga válida y una inválida por comando, y callbacks

PolyConecta.Domain/
├── Common/                               # AuditableEntity, ArchivableEntity, IStatefulDocument, SyncState
└── Plataforma/                           # StateTransitionLog, ReferenceSequence, OutboxMessage (CT-09)

PolyConecta.Application/                  # NUEVO (CT-08)
├── Common/                               # IUseCase<,>, decoradores, IUnitOfWork, ICurrentUser, IClock
└── Plataforma/
    ├── Erp/                              # IBridgeSyncService, ConfirmarSincronizacion, ReintentarSincronizacion
    └── Folios/                           # IReferenceSequenceService

PolyConecta.Infrastructure/
├── Persistence/                          # PolyDbContext (SQL Server), configuraciones por esquema, Migrations/
├── Plataforma/                           # ReferenceSequenceService, StateTransitionLogger
└── Erp/                                  # BridgeDispatcher, BridgeHttpClient, firma del callback, traductores

PolyConecta.Api/
├── Controllers/Plataforma/BridgeCallbackController.cs
└── Program.cs                            # SQL Server, Application, despachador

PolyConecta.Contpaq/                      # bridge (L1)
├── Core/Contract/                        # sobre, comandos y errores de bridge-v1
├── Core/Validation/                      # validaciones de CT-39 por encima del gateway
├── Infrastructure/Outbox/OutboxWorker.cs # ciclo del outbox, separado del gateway
├── Infrastructure/Sdk/                   # ContpaqiSdkGateway (real) y SimulatedSdkGateway
└── Infrastructure/Persistence/           # SqlReadRepository (real) y SimulatedReadRepository

PolyConecta.Web/                          # 0.5 · Angular (fases 0 y 2A de la spec 001)

tests/
├── PolyConecta.Domain.Tests/             # migra a xUnit v3
├── PolyConecta.Application.Tests/        # NUEVO · contra SQL Server (Testcontainers)
├── PolyConecta.IntegrationTests/         # migra; API contra SQL Server
├── PolyConecta.Contract.Tests/           # NUEVO · solo HTTP (CT-23)
└── Contpaq.Bridge.Tests/                 # migra a xUnit v3

tools/sdk-lab/                            # .NET 10 win-x86 (L1)
.github/workflows/ci.yml                  # 0.8
scripts/sql/logins-desarrollo.sql         # logins de API y migraciones en local (CT-30)
```

**Decisión de estructura.** Se conservan los proyectos actuales y se agregan `PolyConecta.Application` y dos de pruebas. Los nombres de módulo de CT-09 aparecen desde F0 con `Plataforma`, que es donde vive la base común. Las entidades parciales que ya existen en `Domain` no se mueven en F0: las rehace cada fase con el modelo de 04.

### Dependencias entre caminos

| Necesita | De | Para |
| :--- | :--- | :--- |
| Contrato firmado (0.2) | Común | 0.6 (traductores y callback), 0.7 (simulador), suite de contrato |
| `global.json` y `Directory.Packages.props` | Común, primera tarea de 0.3 | Todos los proyectos .NET, incluido el bridge |
| Bridge simulado (0.7) | L1 | 0.6 (prueba de ciclo completo, SC-004) y 0.8 (suite en CI) |
| Migración a .NET 10 (0.3) | L2 | 0.8 y la 2B de la spec 001 |

---

## L1 · Camino 1 · Alejandro Ponce

### Decisiones de diseño

- **Separar el ciclo del outbox (0.7, D-122).** `ContpaqiSdkGateway` deja de ser `BackgroundService`. Un `OutboxWorker` nuevo hace el bucle: toma la transacción pendiente, valida (CT-39), llama al gateway (`ISdkGateway`), registra el resultado y despacha el callback. El gateway real conserva la sesión y las llamadas al SDK. Los `if` del modo simulado embebido desaparecen.
- **Modo por configuración.**
  - `BridgeConfig__Mode=Simulated|Real`, con valor por omisión `Real` en Windows y `Simulated` fuera de Windows.
  - En `Simulated` no se exige `BridgeConfig__SqlConnectionString` ni se precarga la DLL.
  - `SimulatedReadRepository` lee su catálogo de `BridgeConfig__Simulated__SeedPath` (JSON con productos, unidades base, almacenes, clientes y existencias por lote). La semilla de ejemplo sale de la empresa `_LAB`.
- **Fallos simulados (FR-008).** `BridgeConfig__Simulated__Faults` lista reglas por `command_type` y, opcionalmente, por `referencia_negocio`: `error_code`, `delay_ms` y `drop_callback`. Se lee al arrancar y también por `PUT /admin/simulated/faults`, que solo existe en modo simulado, para que la suite cambie el escenario sin reiniciar.
- **Folios simulados.** Tabla `simulated_folio` en el SQLite del bridge, con un contador por concepto. Un reenvío con la misma `idempotency_key` devuelve el folio guardado.
- **Validaciones CT-39 en `Core/Validation/`.** Producto existe y activo, unidad base (D-127), almacenes, cliente, `Σ lotes = cantidad`, existencia por lote en el origen. Corren igual en los dos modos, contra `IReadRepository`.
- **Contrato en el código.** `Core/Contract/` con el sobre `1.0`, los cinco comandos y el modelo de errores. `TransactionsController` acepta solo `contract_version` 1.x. El `DOCUMENT_CREATE` genérico se retira (regla 1 del contrato).
- **Callback firmado.** `WebhookDispatcher` agrega `X-Bridge-Signature` (HMAC-SHA256) y reintenta con espera creciente. El secreto llega por `BridgeConfig__CallbackSecret`.
- **.NET 10 (0.4).** El bridge y `sdk-lab` pasan a `net10.0`. `RuntimeIdentifier=win-x86` y `PlatformTarget=x86` se aplican solo al publicar para Windows, para que el modo simulado compile y corra en Linux y macOS. Se repiten F y G en `_LAB` y se compara con la evidencia del 30 de septiembre.
- **Servidor (0.1, 0.9).** Las tareas del VPS siguen guiones versionados en `tools/sdk-lab/scripts/` sin secretos:
  - crear el login de solo lectura del bridge;
  - rotar `sa` con Sistemas;
  - revertir `polyconecta-bridge`;
  - inicio de sesión automático del administrador y tarea "al iniciar sesión", con el mecanismo probado en S-04.

---

## L2 · Camino 2 · Luis Alvarado Martinez

### Decisiones de diseño

- **Migración (0.3).**
  - `global.json` y `Directory.Packages.props` primero.
  - Se quitan Npgsql, MediatR, FluentValidation (sin uso) y EF InMemory.
  - `PolyDbContext` usa SQL Server con `ConnectionStrings__PolyConecta`; las migraciones usan `ConnectionStrings__PolyConectaMigraciones`.
  - Las 17 pruebas migran a xUnit v3 y AwesomeAssertions. Las de integración corren contra SQL Server en contenedor.
- **Application (0.3).** `IUseCase<TRequest, TResult>` con decoradores escritos a mano en `AddApplication()`: validación → transacción → registro. No se agrega Scrutor (research R-02). Los cuatro controladores actuales pasan a llamar casos de uso cuando toquen lógica; en F0 basta con que la API arranque contra SQL Server.
- **Base común (0.6)**, con el modelo en [data-model.md](data-model.md):
  - mixins y `SyncState` como tipo propio;
  - `StateTransitionLog` escrito por el método de transición del documento;
  - `IReferenceSequenceService` sobre una tabla con bloqueo `UPDLOCK, ROWLOCK` dentro de la transacción;
  - `OutboxMessage` con secuencia, llaves de bloqueo (D-95) y estado.
- **Despachador (0.6, research R-04).**
  - `BridgeDispatcher` (`BackgroundService` en `Infrastructure/Erp/`) envía un comando a la vez en orden de secuencia.
  - Espera el estado terminal del anterior antes del siguiente, salvo que no compartan llaves.
  - Reintenta con espera creciente hasta 5 intentos (configurable). Un `Error` bloquea solo a los posteriores que comparten llave.
  - Si el callback no llega en el tiempo configurado, consulta `GET /api/v1/transactions/{id}`.
- **Callback (0.6).** `POST /api/v1/plataforma/bridge/callbacks` verifica la firma y la antigüedad (5 minutos). Es idempotente: un callback repetido o tardío no cambia un documento ya confirmado. Llama al caso de uso `ConfirmarSincronizacion`. El contrato del endpoint está en [contracts/callback-api.md](contracts/callback-api.md).
- **Documento de prueba (SC-004).** `DocumentoDePrueba` vive solo en `tests/PolyConecta.Application.Tests`. Hereda los mixins, tiene dos estados y escribe un `TRASPASO` al confirmar. Recorre el ciclo contra el simulador.
- **Angular (0.5).** Las fases 0 y 2A de la spec 001, ajustadas por su research R-04:
  - el proyecto `PolyConecta.Web` con versiones exactas;
  - `app.css` copiado sin cambios y los mismos CDN que el prototipo;
  - el layout y la barra superior;
  - **12 componentes**, porque `PocSalesOrderForm` se queda en la 001;
  - los tipos que usan esos componentes;
  - Vitest y jsdom.
- **CI (0.8).** `.github/workflows/ci.yml` con tres trabajos en Ubuntu:
  - `dotnet`: build y pruebas de dominio, aplicación e integración con Testcontainers;
  - `contrato`: levanta el bridge en `Simulated` y corre la suite;
  - `web`: `npm ci`, build y pruebas.

  La regla de protección de `main` exige los tres checks (SC-007).

---

## Complexity Tracking

| Desviación | Por qué hace falta | Alternativa más simple descartada |
| :--- | :--- | :--- |
| Ruta de administración de fallos (`PUT /admin/simulated/faults`) en el bridge | La suite de contrato necesita provocar errores y demoras sin reiniciar el proceso (FR-008) | Solo variables de entorno: obliga a reiniciar el bridge por escenario y alarga la suite |
| Testcontainers, dependencia nueva | Las pruebas de aplicación necesitan SQL Server 2022 real igual en local y en CI (CT-27) | Un servicio de GitHub Actions: solo sirve en CI y deja sin base a quien corre las pruebas en local |
