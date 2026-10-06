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

`Polyconecta.slnx` · **.NET 10** (SDK 10.0.300 en `global.json`), versiones exactas en `Directory.Packages.props` (CT-36). La única excepción es `PolyConecta.Presentation`, que sigue en .NET 8 porque el prototipo no se modifica (D-60).

```mermaid
flowchart LR
    WEB["PolyConecta.Web<br/>Angular 22 · :9000"]
    PROTO["PolyConecta.Presentation<br/>prototipo Blazor · .NET 8<br/>solo en pruebas de paridad · :9010"]
    API["PolyConecta.Api<br/>ASP.NET Core 10 · :9200"]
    APP["PolyConecta.Application<br/>casos de uso y puertos"]
    INF["PolyConecta.Infrastructure<br/>EF Core · SQL Server 2022 · outbox · despachador"]
    DOM["PolyConecta.Domain<br/>sin dependencias"]
    BR["PolyConecta.Contpaq<br/>bridge · :9030<br/>Real (win-x86) o Simulated"]
    ERP[("CONTPAQi Comercial<br/>SQL Server + SDK")]

    WEB -. "REST /api/v1 (desde F1)" .-> API
    WEB -- "chatter en vivo · SignalR /hubs/chatter" --> API
    PROTO -. "referencia de UX" .-> WEB
    API --> APP --> DOM
    INF --> APP
    INF -- "contrato bridge-v1 (HTTP)" --> BR
    BR -- "callback firmado" --> API
    BR -- "SDK: escribe documentos (modo real)" --> ERP
    BR -- "SQL de solo lectura" --> ERP
```

La flecha punteada de la web a la API es de F1 en adelante. Hasta entonces, `PolyConecta.Web` es la **réplica 1:1 del prototipo** (spec 001): las mismas pantallas, datos semilla y reglas, con el estado en memoria del navegador; solo usa la API para el chatter en vivo, por el hub `ChatterHub` en `/hubs/chatter` con su propia política de CORS para `:9000` (D-58). El prototipo Blazor se conserva como referencia de comportamiento y la réplica se compara contra él con pruebas de paridad (D-60).

## 3. Estado real de cada proyecto

| Proyecto | Qué hace hoy | Destino |
| :--- | :--- | :--- |
| `PolyConecta.Domain` | Base común (04 §1): `AuditableEntity`, `ArchivableEntity`, `DocumentoConEstado` con transiciones nombradas y `SyncState` (CT-15). En `Plataforma/`: `StateTransitionLog`, `ReferenceSequence` y `OutboxMessage`. Las entidades previas a F0 siguen en `Entities/` sin rediseñar | Modelo de 04 por fase |
| `PolyConecta.Application` | Casos de uso con decoradores propios (registro → validación → transacción, D-72). Puertos: `IUnitOfWork`, `IClock`, `ICurrentUser` (es "sistema" hasta F1), `IReferenceSequenceService` e `IBridgeSyncService`. Casos de uso `ConfirmarSincronizacion` y `ReintentarSincronizacion` | Un caso de uso por acción de negocio |
| `PolyConecta.Infrastructure` | `PolyDbContext` en **SQL Server 2022**, un esquema por módulo (`plt`, `inv`, `prd`, `cal`) y migraciones (`F0_Base`, `F0_Plataforma`). Interceptor de auditoría y bitácora, filtro de archivado, folios con `UPDLOCK`. En `Erp/`: traductores al contrato, cliente HTTP del bridge, verificación de firma y **despachador** (orden, espera por llave, bloqueo por error, reintentos y consulta si no llega el callback) | Persistencia y traductores de cada fase |
| `PolyConecta.Api` | Controladores previos a F0 (`Orders`, `Rolls`, `RawMaterials`, `Locations`) sobre SQL Server; callback del bridge (`/api/v1/plataforma/bridge/callbacks`) y reintento manual (`/api/v1/plataforma/outbox/{id}/reintentar`); `X-Correlation-ID` (CT-31); hub del chatter `/hubs/chatter` (SignalR, D-58) | Contrato por caso de uso, autenticación (F1) |
| `PolyConecta.Web` | Angular 22 · **réplica 1:1 del prototipo** (spec 001): las 18 páginas en 19 rutas, el estado de `Presentation/Services` portado a servicios con signals sobre colecciones con su documento semilla, "Nuevo" con creación libre en 9 documentos (D-59, Principio X) y chatter en vivo por la API. Pruebas: Vitest, paridad visual, guiones de escenario, auditor de botones y chatter con Playwright (ver `PolyConecta.Web/README.md`) | Conectarse a la API desde F1 |
| `PolyConecta.Presentation` | **Prototipo navegable** en Blazor Server, sin cambios (D-60). Es la referencia contra la que corren las pruebas de paridad de `PolyConecta.Web` | Referencia hasta conectar Angular a la API |
| `PolyConecta.Contpaq` | Bridge con el contrato `bridge-v1`: sobre 1.0, validaciones de CT-39 y D-127, `OutboxWorker` en un hilo STA, callback firmado, lecturas del contrato. **Modo simulado** (`BridgeConfig__Mode=Simulated`, por omisión fuera de Windows) con catálogo semilla, folios por concepto y fallos configurables. En **modo real** los comandos se implementan en su fase (2.4, 3.1, 3.2, 5.1, 6.1) | Comandos reales con ejecución por pasos (CT-38) |
| `tests/` | Dominio, aplicación e integración contra SQL Server con Testcontainers; bridge; **suite de contrato** por HTTP (CT-23) y ciclo completo, que necesitan `BRIDGE_URL` | Una prueba por regla (CT-28) |
| `tools/sdk-lab` | Laboratorio de la matriz del SDK, ya en .NET 10 (`win-x86`). Falta repetir F y G en el VPS (tarea 0.4) | Suite de contrato contra el bridge real |

## 4. Deuda técnica conocida

1. **Credencial en el historial.** La contraseña de `sa` estuvo versionada hasta el 28-sep; el bridge ya la toma de `BridgeConfig__SqlConnectionString`. **Falta rotarla** y crear el login de solo lectura (tarea 0.1, en el VPS). El historial no se reescribe (D-51).
2. ~~Sin persistencia real~~. Resuelta en F0: SQL Server 2022 con migraciones y logins separados (CT-30).
3. **La UI no usa el backend.** El prototipo duplica en `Presentation/Services` la lógica que debería vivir en el dominio, y la réplica en Angular la porta tal cual (`src/app/core/state/`) para tener paridad. Se reemplaza por la API desde F1, caso de uso por caso de uso.
4. **Gateway real del SDK.** El bridge real todavía no implementa los comandos del contrato; la ejecución por pasos con reconciliación (D-80, CT-38), los N lotes por movimiento (D-82), el par Salida + Entrada (D-79), la sesión de larga duración con doble inicio de sesión (D-91, D-108) y la verificación posterior (CT-39) llegan con cada comando.
5. ~~Sin CI~~. Resuelta en F0: GitHub Actions con los trabajos `dotnet`, `contrato` y `web` (CT-27). Falta que un administrador proteja `main` con esos checks.
6. **Supuestos del SDK**: la [matriz](../contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md) verificó WIP como almacén, lotes múltiples y fraccionados, devolución parcial y parcialidades (D-79 a D-87). Siguen abiertos la frescura de la lectura (T-06) y el costo de la entrada (T-17).
7. **Bridge como proceso interactivo**: el SDK no funciona como servicio de Windows (S-03, D-108). Corre en la sesión del administrador con inicio de sesión automático (D-115; tarea 0.9).

## 5. Cómo correrlo

Requisitos: SDK de .NET 10 (`global.json`), Docker y Node 24.16 (`PolyConecta.Web/.nvmrc`). El runtime de ASP.NET Core 8 solo hace falta para las pruebas de paridad, que levantan el prototipo en `:9010`; `run.sh` ya no lo levanta (D-128).

```bash
./run.sh                 # compila, prueba y levanta PolyConecta.Web (:9000) + API (:9200); comprueba Node 24.16 y corre npm ci si falta
./run.sh --with-bridge   # además levanta el bridge (:9030), en modo simulado fuera de Windows, conectado a la API
cd PolyConecta.Web && npm ci && npm start   # solo la aplicación Angular (:9000)
```

Sin `ConnectionStrings__PolyConecta`, `run.sh` levanta un SQL Server 2022 local en Docker (`polyconecta-sql`, puerto 14333), crea los logins de `scripts/sql/logins-desarrollo.sql` con contraseñas generadas en `.env.local` y aplica las migraciones con `dotnet ef` (herramienta local en `dotnet-tools.json`).

La suite de contrato y el ciclo completo corren contra un bridge levantado:

```bash
BRIDGE_URL=http://localhost:9030 BRIDGE_CALLBACK_SECRET=<el de BridgeConfig__CallbackSecret> \
  dotnet test --project tests/PolyConecta.Contract.Tests
```

| Script | Uso |
| :--- | :--- |
| `scripts/build.sh` | Empaqueta el bridge para Windows x86 |
| `scripts/deploy.sh` | Despliega el bridge al VPS Windows |
| `scripts/screenshots.sh` | Recorre el prototipo con Playwright y guarda capturas en `docs/screenshots/` |
| `scripts/sql/logins-desarrollo.sql` | Base y logins de PolyConecta para desarrollo y CI (CT-30) |

## 6. Interfaz: `PolyConecta.Web`

Viene de la spec 001 (réplica del prototipo en Angular). Es la base de las pantallas de F1 en adelante (D-118).

### 6.1 Sistema de diseño (CT-24)

- **Estilo**: el `app.css` del prototipo sin cambios, Bootstrap 5.3.2, Bootstrap Icons 1.11.3 y la fuente Inter. Las clases `o_*` reproducen Odoo 19 (Principio IX).
- **Estructura de todo documento**, en este orden: panel de control (`o_control_panel`: "Nuevo", migas y smart buttons) → barra de acciones (`o_statusbar`: acción primaria, secundarias y estado) → hoja (`o_form_sheet`: etapas, título y folio, campos en dos columnas, pestañas y líneas) → chatter a la derecha. Las listas usan el mismo panel con la barra de búsqueda y el paginador.
- **Componentes compartidos** (`src/app/shared/`): `odoo-topbar`, `odoo-breadcrumb`, `odoo-search-panel` (vistas de búsqueda declarativas con facetas; filtros del mismo campo se unen con "o", de campos distintos se cruzan), `odoo-view-switcher`, `odoo-pager`, `odoo-smart-buttons`, `odoo-status-pipeline`, `odoo-line-capture`, `odoo-chatter-drawer`, `lot-picker-modal`, `lot-quantity-picker-modal` y `poc-sales-order-form`. Para el modo libre: `boton-nuevo` y `hoja-nueva`.
- **Captura de líneas**: `[Clave / Producto] [Cantidad] [Unidad] [Agregar]`. Si el producto está en el catálogo de CONTPAQi, la unidad se precarga. En modo libre la unidad es la base del producto y no se edita (D-127), y el pedido libre agrega precio unitario y moneda (D-74).
- **Smart buttons** de un documento libre: vacíos o deshabilitados, nunca con un origen falso (FR-014 de la spec 001).

### 6.2 Estado mientras no hay API

- Los servicios de `Presentation/Services` están portados a `src/app/core/state/` con signals: mutan los mismos objetos y exponen la señal `cambios`. Un `computed` que devuelve el mismo objeto mutado lleva `{ equal: () => false }`.
- Lo que en el prototipo era un documento único (pedido, traslado, recepción y entrega) es una **colección con su documento semilla**; las operaciones reciben el folio. Así "Nuevo" puede crear otro sin romper la paridad.
- El estado vive en memoria y se pierde al recargar, igual que el circuito de Blazor.

### 6.3 Modo libre ("Nuevo", D-59)

"Nuevo" abre `<lista>/nuevo`: la hoja del documento en su estado inicial, con los campos mínimos, "Guardar" y "Descartar". Al guardar se crea el documento libre y se abre su formulario. Las reglas viven en `src/app/core/state/libre/`, cada una con su prueba:

| Documento | Regla |
| :--- | :--- |
| Pedido | Cliente; líneas con cantidad, unidad, precio y moneda. Al confirmar recibe un Contpaq ID simulado y sigue las dos firmas (D-53, D-74) |
| Orden de fabricación | Sin pedido. Lotes con el folio de la OF raíz, `R001-BOL-2026-0007` (D-54). Al confirmarla genera recolección y control como una ligada |
| Recolección y devolución | Planta y líneas. Sin OF: el saldo queda sin asignar en WIP (D-55) y la devolución regresa ese saldo |
| Asignar saldo de WIP | Acción de la OF, solo cuando hay saldo sin asignar de sus componentes; nada se liga solo (FR-013 de la spec 001) |
| Control de calidad | Sobre lotes existentes en revisión; aprobar o rechazar tiene el mismo efecto que en el control ligado |
| Traslado y entrega | Solo lotes liberados por Calidad: libres en `<planta>/Stock/*`, fuera de cuarentena (hard-stop) |
| Recepción | Solo lotes en `TRANS/*` (D-56). El traslado libre deja sus lotes ahí al llegar a Hecho |

Los folios libres (`PV-2026-0001`, `QC-2026-0001`, la siguiente OF de su proceso) son de la réplica; al conectar la API los dará `IReferenceSequenceService`.

### 6.4 Verificación

`PolyConecta.Web/README.md` describe los scripts. La paridad se mide contra el prototipo corriendo: ≤ 1 % de píxeles distintos por ruta y por punto de control de cada guion, con "Nuevo" enmascarado. Las diferencias legítimas con el prototipo son tres: el pedido aplica al instante el bloqueo de líneas al autorizar (el prototipo no se vuelve a pintar), una ruta inexistente muestra "Página no encontrada" (el prototipo devuelve un 404 vacío) y "Nuevo".
