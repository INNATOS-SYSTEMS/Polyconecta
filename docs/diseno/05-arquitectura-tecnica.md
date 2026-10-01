# Arquitectura técnica

Cómo está armada la solución, qué hace hoy cada proyecto y qué deuda técnica hay que resolver antes de construir. Las reglas que gobiernan todo esto están en la [constitución](../../.specify/memory/constitution.md).

## 1. Principios que condicionan la arquitectura

| Principio | Consecuencia técnica |
| :--- | :--- |
| I · CONTPAQi es el sistema de registro | PolyConecta no factura ni guarda saldos contables oficiales; los refleja. No se despliega Odoo: solo se imita su experiencia. |
| II · Sincronización asíncrona con outbox | Toda escritura a CONTPAQi pasa por un outbox y un bridge x86 de un solo hilo con el SDK de Comercial, `MGWServicios.dll` (D-88, constitución 1.6.1). **Prohibido** hacer `INSERT` o `UPDATE` directo en tablas `adm*`. La planta nunca espera un bloqueo del ERP. |
| III · Catálogos | La MP es un catálogo único entre plantas; el PT puede tener código por cliente. |
| IV · Balance de masa y hard-stop | Tolerancia configurable, auditoría obligatoria por corrida y ningún movimiento sin liberación de Calidad. |
| VI · Alcance de Fase 1 | No hay handheld, terminal ni escáner: el Planner captura en la aplicación web. Montemorelos, el intercompany, el peletizado y la báscula IoT quedan para la Fase 2. |
| VII · Respaldo técnico | Toda decisión de SDK o BD se respalda en `docs/contpaq/` o en documentación oficial verificada. |
| VIII · Realidad de CONTPAQi | El modelo se contrasta con las tablas `adm*` reales antes de ratificarse. |
| IX · Odoo 19 como referencia de UX | Kanban, lista y formulario, pipeline de estado, smart buttons y chatter. |

## 2. Solución

`Polyconecta.slnx` · .NET 8 (el formato `.slnx` requiere SDK 9+ con el runtime ASP.NET Core 8 instalado).

```mermaid
flowchart LR
    UI["Presentación Angular<br/>objetivo (D-48)"]
    PROTO["PolyConecta.Presentation<br/>prototipo Blazor · :9000"]
    API["PolyConecta.Api<br/>ASP.NET Core · :9020"]
    INF["PolyConecta.Infrastructure<br/>EF Core · SQL Server · outbox"]
    DOM["PolyConecta.Domain<br/>sin dependencias"]
    BR["PolyConecta.Contpaq<br/>bridge x86 · :5005"]
    ERP[("CONTPAQi Premium<br/>SQL Server + SDK")]

    UI -. "objetivo: REST /api/v1" .-> API
    PROTO -. "referencia de UX" .-> UI
    API --> INF --> DOM
    API --> DOM
    INF -. "objetivo: outbox → bridge" .-> BR
    BR -- "SDK: escribe documentos" --> ERP
    BR -- "SQL NOLOCK: lee catálogos y existencias" --> ERP
```

Las flechas punteadas son el diseño objetivo y **todavía no existen**.

## 3. Estado real de cada proyecto

| Proyecto | Qué hace hoy | Destino |
| :--- | :--- | :--- |
| `PolyConecta.Domain` | Entidades Odoo-native parciales (ver [04-modelo-de-dominio.md §5](04-modelo-de-dominio.md)), `MassBalanceService`, interfaces de repositorio | Modelo objetivo completo, con mixins, estados cerrados y servicios de dominio |
| `PolyConecta.Infrastructure` | `PolyDbContext` con configuraciones, repositorios, `OutboxPublisher`, `IUnitOfWork`. Referencia Npgsql, que no se usa y hay que cambiar por el proveedor de SQL Server | **SQL Server** (D-49) con migraciones, outbox transaccional y semillas de catálogos, incluida la reserva inicial de almacenes en CONTPAQi (D-43) |
| `PolyConecta.Api` | 4 controladores (`Orders`, `Rolls`, `RawMaterials`, `Locations`) sobre **EF InMemory**, Swagger y `ProblemDetailsMiddleware` | Contrato por caso de uso, autenticación, paginación y filtros declarativos |
| `PolyConecta.Presentation` | **Prototipo navegable** en Blazor Server: shell Odoo, 18 páginas, chatter en vivo por SignalR. Todo el estado vive en memoria (`OperationalFlowState`, `StockOperationState`, `InventoryState`); **no llama a la API** y simula los folios de CONTPAQi | Se sustituye por **Angular** (D-48): primero una réplica 1:1 (spec `.specify/features/001-angular-presentation/`) y después la conexión a la API |
| `PolyConecta.Contpaq` | Bridge x86 con outbox SQLite, gateway del SDK con circuit breaker, lecturas SQL, webhooks, DLQ y dashboard. El interop ya usa `MGWServicios.dll` y las estructuras corregidas (H-1, H-4 de la matriz) | Traspaso como par Salida + Entrada con N lotes (D-79, D-82), ejecución por pasos con reconciliación (D-80), validación y verificación (D-81), sesión con usuario (D-88), idempotencia y DLQ recuperable |
| `tests/` | `PolyConecta.Domain.Tests` (8), `PolyConecta.IntegrationTests` (6, API en memoria) y `Contpaq.Bridge.Tests` (3). Las 17 pasan y `run.sh` las ejecuta | CI en cada PR |
| `tools/sdk-lab` | Laboratorio para ejecutar la matriz de pruebas contra un CONTPAQi real (Windows). Ejecutó la matriz el 30-sep-2026 (35 de 36 pruebas) en .NET 8 | Suite de contrato contra el bridge real (CT-23) |

## 4. Deuda técnica conocida

1. **Credencial expuesta en el historial.** Hasta el 28-sep, `PolyConecta.Contpaq/appsettings.json` versionaba la contraseña de `sa`. Ya no está en el código: el bridge exige la variable de entorno `BridgeConfig__SqlConnectionString` y en desarrollo usa `dotnet user-secrets`. **Falta rotar la contraseña** y crear el login de solo lectura del bridge. El historial no se reescribe (D-51).
2. **Sin persistencia real.** La API corre sobre `UseInMemoryDatabase`; el destino es SQL Server.
3. **La UI no usa el backend.** El prototipo duplica en `Presentation/Services` la lógica que debería vivir en el dominio.
4. **Gateway del SDK** (`ContpaqiSdkGateway.cs`), contra lo que la matriz demostró:
   - Deja un documento huérfano cuando falla el movimiento (G-01) y un documento a medias si el proceso se interrumpe (G-04). Falta la ejecución por pasos con reconciliación (D-80).
   - Manda un solo lote por movimiento, por la cantidad completa (C-02). Falta mandar N capas (D-82).
   - Intenta el traspaso con un solo movimiento, que CONTPAQi rechaza (B-02). Falta el par Salida + Entrada con la regla de costo (D-79).
   - Usa `fInicializaSDK` sin iniciar sesión (H-3). Falta el doble inicio de sesión y la sesión de larga duración (D-91, D-108); `sdklab` ya los implementa.
   - No valida existencia ni verifica lo registrado (C-07, D-05). Falta CT-39.
5. **Sin CI** ni gestión de secretos.
6. **Supuestos del SDK**: la [matriz](../contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md) verificó WIP como almacén, lotes múltiples y fraccionados, devolución parcial y parcialidades (D-79 a D-87). El bloque S (1-oct) resolvió la lentitud y los bloqueos (D-108, D-109), el alta de almacenes (D-110) y el registro de la producción (D-111). Siguen abiertos la frescura de la lectura (T-06) y qué hacer con el pedido en CONTPAQi (P-22).
7. **Bridge como proceso interactivo**: el SDK no funciona como servicio de Windows (S-03, D-108). Falta probar el inicio de sesión automático con reinicio (H-03, S-04).

## 5. Cómo correrlo

```bash
./run.sh                 # compila, corre pruebas y levanta Presentation (:9000) + API (:9020)
./run.sh --with-bridge   # además levanta el bridge de CONTPAQi (:5005)
```

`run.sh` delega en `scripts/run.sh`. Otros scripts:

| Script | Uso |
| :--- | :--- |
| `scripts/build.sh` | Empaqueta el bridge para Windows x86 |
| `scripts/deploy.sh` | Despliega el bridge al VPS Windows |
| `scripts/screenshots.sh` | Recorre el prototipo con Playwright y guarda capturas en `docs/screenshots/` |
