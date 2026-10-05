# Investigación: Construcción técnica (F0)

Decisiones de la fase 0 del plan. Las versiones se consultaron en NuGet y npm el 2026-10-05. El estado del código es el de `main` en `2104b4b`.

---

## R-01 · Versiones exactas

**Decisión** (`Directory.Packages.props` y `global.json`):

| Paquete | Versión | Dónde |
| :--- | :--- | :--- |
| SDK .NET (`global.json`, `rollForward: latestPatch`) | 10.0.300 | Todo. Es el que está instalado |
| `Microsoft.EntityFrameworkCore.SqlServer`, `.Design` | 10.0.12 | Infrastructure |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.12 | Pruebas de integración |
| `Microsoft.Data.SqlClient` | 7.1.1 | Bridge, `sdk-lab` |
| `Microsoft.Data.Sqlite` | 10.0.12 | Bridge |
| `Dapper` | 2.1.89 | Bridge |
| `Serilog.AspNetCore` | 10.0.0 | Bridge |
| `Swashbuckle.AspNetCore` | 10.2.3 | API y bridge |
| `Scalar.AspNetCore` | 2.17.13 | Bridge |
| `xunit.v3` / `xunit.runner.visualstudio` | 4.0.1 / 4.0.0 | Todas las pruebas (D-71) |
| `Microsoft.NET.Test.Sdk` | 18.10.1 | Todas las pruebas |
| `AwesomeAssertions` | 9.6.0 | Todas las pruebas (D-72) |
| `Testcontainers.MsSql` | 4.15.0 | Pruebas de aplicación e integración (R-07) |
| `Moq` | 4.21.0 | `Contpaq.Bridge.Tests`, que ya lo usa |

Se quitan `Npgsql.EntityFrameworkCore.PostgreSQL`, `MediatR`, `FluentValidation` (no se usa en ningún archivo) y `Microsoft.EntityFrameworkCore.InMemory` (D-72, FR-012).

**Avisos de la migración:**
- Swashbuckle 10 y Scalar 2 cambian de API respecto a 6.x y 1.x: la configuración de `Program.cs` del bridge y de la API se ajusta.
- `Microsoft.Data.SqlClient` 7 cifra por omisión: la cadena de conexión del bridge debe declarar `Encrypt` y `TrustServerCertificate` como lo pida el servidor de CONTPAQi. Se prueba en 0.4.

**Excepción**: `PolyConecta.Presentation` se queda en `net8.0`. D-60 prohíbe modificar el prototipo, y la spec 001 lo necesita corriendo para la paridad. El SDK 10 lo compila; `run.sh` debe encontrar los runtimes de ASP.NET Core 8 y 10. La excepción termina cuando se retire el prototipo.

**Alternativas**: quedarse en las versiones 8.x de los paquetes: incumple CT-04.

---

## R-02 · Casos de uso y decoradores (D-72)

**Decisión**:
- Cada caso de uso implementa `IUseCase<TRequest, TResult>` en `PolyConecta.Application/<Modulo>/`.
- `AddApplication()` registra cada uno envuelto a mano en tres decoradores, de fuera hacia dentro: `LoggingDecorator` (`correlation_id`, duración), `TransactionDecorator` (abre la transacción del `DbContext` y confirma al final) y `ValidationDecorator` (llama a un `IValidator<TRequest>` propio si existe).
- `Application` define los puertos (`IUnitOfWork`, `IBridgeSyncService`, `IReferenceSequenceService`, `ICurrentUser`, `IClock`) e `Infrastructure` los implementa.

**Por qué**: D-72 pide decoradores propios sin MediatR. Hacerlo a mano evita otra dependencia (Scrutor).

**Alternativas**:
- Scrutor para decorar: dependencia nueva sin necesidad, con pocos casos de uso en F0.
- Filtros de ASP.NET: atan la transacción a HTTP, y el despachador también ejecuta casos de uso.

---

## R-03 · Persistencia y migraciones

**Decisión**:
- `PolyDbContext` con SQL Server. Cada configuración declara su esquema (CT-12). En F0 se crea `plt` con la base común, y las entidades que ya existen se migran a su esquema (`inv` o `prd`) sin rediseñarlas.
- La primera migración se llama `F0_Base`.
- **Logins (CT-30):**
  - `polyconecta_app`: `db_datareader`, `db_datawriter` y `EXECUTE`;
  - `polyconecta_migraciones`: `db_owner`.

  En local los crea `scripts/sql/logins-desarrollo.sql`; en CI, el trabajo de pruebas.
- **Cadenas por variable de entorno:** `ConnectionStrings__PolyConecta` y `ConnectionStrings__PolyConectaMigraciones`. En desarrollo van en `user-secrets` (CT-29).
- **Control de concurrencia:** `rowversion` en los documentos.

**Alternativas**: un solo login para todo, que incumple CT-30.

---

## R-04 · Despachador, orden y reintentos

**Decisión**:
- **`OutboxMessage`** guarda:
  - `sequence` (`IDENTITY`), que define el orden (CT-41);
  - `lock_keys`, la lista de `producto:<código>` y `almacen:<código>` de la carga, que alimenta D-95;
  - su estado: `Pendiente`, `Enviado`, `Confirmado`, `Error` o `Bloqueado`.
- **`BridgeDispatcher`:**
  1. Toma el `Pendiente` de menor secuencia que no comparta llaves con uno `Enviado` o `Error`. Los que sí comparten pasan a `Bloqueado`.
  2. Lo envía. Si el bridge responde `202`, queda `Enviado` con `bridge_transaction_id`.
  3. No envía otro comando que comparta llaves hasta recibir el callback terminal del anterior.
  4. Si la red falla o responde `5xx`, reintenta con espera de 2ⁿ segundos y un tope configurable (5 intentos por omisión). Al agotarlos, el documento queda en `Error` (CT-20).
  5. Si el callback no llega en `Erp__CallbackTimeout` (2 minutos por omisión), consulta `GET /api/v1/transactions/{id}`.
- **Reintentar un `Error`:** el caso de uso `ReintentarSincronizacion` lo regresa a `Pendiente` con la misma `idempotency_key` y libera a los `Bloqueado` que dependían de él.
- **Un solo despachador:** se garantiza con `sp_getapplock` en SQL Server, para no enviar dos veces si hay dos instancias de la API.

**Por qué**: "uno a la vez" (CT-41) y "un error detiene solo a los que comparten llave" (D-95) solo son compatibles si la espera es por llave.

**Alternativas**:
- Esperar el estado terminal de cualquier comando: serializa todo y vuelve lenta la planta.
- Enviar sin esperar: un error llega tarde y los comandos posteriores ya se enviaron.

---

## R-05 · Bridge simulado

**Decisión**: ver el plan, sección L1. Hay tres puntos con hallazgo en el código:
- Hoy el modo simulado vive dentro de `ContpaqiSdkGateway` (`_forceMockMode`, línea 110), que además es el `BackgroundService` con el bucle del outbox (`RunWorkerLoop`). Se separan en `OutboxWorker`, `ISdkGateway` real y simulado, y `IReadRepository` real y simulado.
- `Program.cs` falla si no hay `BridgeConfig:SqlConnectionString`. En modo simulado ese requisito se omite.
- `/health` devuelve `sdk_initialized = true` siempre. Debe reflejar el estado real y el modo (`mode: "Simulated"`).

La semilla del simulador es un JSON versionado en `PolyConecta.Contpaq/Simulated/seed.json`, con productos, unidades base, almacenes, clientes y lotes tomados de `_LAB` (Principio VIII). No lleva datos de la empresa de producción.

---

## R-06 · Puntos abiertos del contrato

Son propuestas para la sesión de los líderes. Ninguna se aplica sin su firma (CT-22).

| Punto | Propuesta | Por qué |
| :--- | :--- | :--- |
| Unidad de la línea (D-127) | La unidad debe ser la base del producto en CONTPAQi; `UNIDAD_NO_ADMITIDA` cambia su texto a "no es la unidad base" | D-127 quitó la elección de unidad por línea |
| Referencia de 20 caracteres | `base32(SHA-256(idempotency_key))[0..20]`, sin relleno | Es determinista, cabe en `CREFERENCIA` (S-06) y no expone el id |
| `SDK_ERROR` | `retryable: false` por omisión; solo `SDK_TIMEOUT` y `SDK_SESION` se reintentan | Un error desconocido reintentado puede duplicar efectos (G-02) |
| `concepts`, `invoices` | Fuera de `1.0`, en las rutas de operación | Ninguna fase los usa y el concepto lo elige el bridge (D-121) |
| Rutas de operación (§7) | Confirmar que quedan fuera | Sistemas las usa desde el tablero del bridge |
| Costo (T-17) | Campo opcional `costo` en el resultado de `TRASPASO` y `CIERRE_PRODUCCION`; la regla la decide la sesión | Permite firmar `1.0` sin cerrar T-17 (cambio compatible) |

---

## R-07 · SQL Server en las pruebas

**Decisión**: Testcontainers (`Testcontainers.MsSql` 4.15.0), con la imagen `mcr.microsoft.com/mssql/server:2022-latest` fijada por digest en el código de pruebas.
- Un contenedor por corrida de pruebas, compartido por las clases con un `AssemblyFixture` de xUnit v3.
- Una base nueva por clase de prueba, creada con las migraciones, nunca con `EnsureCreated` (CT-06).

**Requisito local**: Docker en marcha. Hoy el demonio de Docker de esta máquina está apagado.

**Alternativas**: el servicio `mssql` de GitHub Actions, que solo existe en CI.

**Regla de autonomía 5**: Testcontainers es un paquete nuevo y se anota en "Exploración y cambios".

---

## R-08 · CI y protección de `main`

**Decisión**:
- El archivo `.github/workflows/ci.yml` corre en `pull_request` y `push` a `main`, con tres trabajos en `ubuntu-latest`:
  - **`dotnet`:** `setup-dotnet` con `global.json`, restore, build y pruebas, incluidas las de Testcontainers, porque el runner trae Docker;
  - **`contrato`:** publica el bridge, lo levanta en modo `Simulated` y corre `tests/PolyConecta.Contract.Tests` con `BRIDGE_URL`;
  - **`web`:** `setup-node` con `.nvmrc`, `npm ci`, build y pruebas.
- **SC-007:** la regla de protección de `main` exige los tres checks y un PR. La configura un administrador del repositorio con `gh api` o desde GitHub; no se puede versionar.

**Alternativas**: Azure DevOps, descartado por D-76.

---

## R-09 · Suite de contrato

**Decisión**:
- `tests/PolyConecta.Contract.Tests` es un proyecto xUnit v3 sin referencias a otros proyectos.
- Lee `BRIDGE_URL`, `BRIDGE_CALLBACK_SECRET` y `CALLBACK_HOST` de variables de entorno.
- Levanta su propio receptor de callbacks: un `WebApplication` mínimo en un puerto libre, que verifica la firma.
- Hay una clase por comando, con la carga válida y la inválida de `docs/contratos/ejemplos/`, más pruebas de idempotencia, versión y errores.
- Las pruebas que necesitan fallos simulados se marcan con el rasgo `Simulado` y se omiten contra el bridge real.

**Por qué**: es la misma suite contra los dos lados (CT-23).

---

## R-10 · Ramas

**Hallazgo**: la regla de autonomía 1 de la spec dice "ramas que salen de `002-construccion-tecnica`". CT-44 dice que los dos líderes trabajan en la rama de la spec.

**Decisión**: se alinea con CT-44. Todo va en `002-construccion-tecnica`, con un commit por tarea. La CI corre en cada push a la rama y en el PR final a `main`. Está registrado en "Exploración y cambios".

---

## R-11 · Lo que no se puede hacer fuera del VPS

0.1, 0.4 (la parte de laboratorio) y 0.9 necesitan el VPS con Windows, CONTPAQi y la empresa `_LAB`. Sus tareas dejan guiones versionados y una evidencia (salida de `sdk-lab` o captura), y se marcan hechas solo con esa evidencia (regla de autonomía 6).
