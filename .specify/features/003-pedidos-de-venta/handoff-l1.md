# Handoff L1 (spec 003)

Archivo temporal: se borra al cerrar la fase.

## Dónde está

- Rama: `003-pedidos-de-venta-l1` (base `4ba929e` de `003-pedidos-de-venta`).
- Worktree: `/Users/emilio/Development/Sandbox/Polyconecta/.claude/worktrees/agent-a1428a14b041ac060`.
- Último commit: el `WIP L1: handoff` que contiene este archivo; antes, `d2a6275` (C-T004).

## Tareas

| Tarea | Estado | Commit |
| :--- | :--- | :--- |
| C-T005 (semilla 1.1 y agentes en el simulador) | Hecha, marcada `[x]` | `afdc9a1` |
| C-T006 (`PUT`/`DELETE /admin/simulated/catalog`) | Hecha, marcada `[x]` | `106ea2f` |
| C-T004 (suite de contrato de lecturas) | Hecha, marcada `[x]`; 47 pruebas pasan contra el simulador en :9031 | `d2a6275` |
| L1-T006 y L1-T007 (lecturas reales 1.1 y existencias de varios productos) | **Código escrito y en el commit WIP; sin marcar `[x]` en `tasks.md`.** Pruebas locales en verde; el SQL real solo se verifica en el VPS | WIP |
| L1-T010 (`agente` opcional) | **Casi hecha**: ya están `agente` en `CargaAltaPedido`, la validación `AGENTE_NO_EXISTE`, la revisión de agente vacío en el lector y `VersionActual = "1.1"`. **Falta** mover `docs/contratos/ejemplos/pendientes-1.1/*.json` a `ejemplos/` (borrar la carpeta y su `LEEME.md`), agregar el ejemplo inválido a `ComandosTests` de la suite de contrato, una prueba en `tests/Contpaq.Bridge.Tests` y correr ambas suites | pendiente |
| L1-T001 a L1-T004 (sesión permanente, vigilante, reinicio diario) | No empezadas | pendiente |
| L1-T005, L1-T008, L1-T009 | Del usuario en el VPS; no se intentan | — |

L1-T006 / L1-T007, lo que contienen: `Infrastructure/Persistence/SqlLecturas.cs` (todo el SQL), `SqlContractReadRepository.cs` reescrito (paginación por id, clasificación `BridgeConfig__Clasificacion__Productos`, moneda ISO con `ConfiguracionConceptos.CodigoIso`, domicilios en una consulta por página, agentes, existencias en lotes de 100 por lote F-02 y por producto F-01), cableado en `Program.cs`, cursor inválido a `400` en `LecturasController`, y `tests/Contpaq.Bridge.Tests/Persistence/SqlLecturasTests.cs` (revisa cada tabla y columna contra `Referencia_BD_CONTPAQi.md`).

## Pruebas en el último punto

`dotnet test --project tests/Contpaq.Bridge.Tests`: 63 correctas, 0 fallidas, 0 omitidas. La suite de contrato (47) pasó antes de L1-T006; no se volvió a correr después de los cambios de cursor y paginación del simulador (conviene repetirla). `dotnet test Polyconecta.slnx` completo no se ha corrido.

Para correr el simulador: desde `PolyConecta.Contpaq`, con `BridgeConfig__Mode=Simulated BridgeConfig__DashboardPort=9031 BridgeConfig__CallbackSecret=secreto-l1 BridgeConfig__SqliteConnectionString="Data Source=<ruta temporal>"` y `dotnet bin/Debug/net10.0/Contpaq.Bridge.dll`. Debe correr con ese directorio de trabajo para que lea `appsettings.json` (si no, `VARIANTE_SIN_CONCEPTO`). El puerto sale de `BridgeConfig__DashboardPort`, no de `ASPNETCORE_URLS`. Ya está detenido.

## Decisiones y supuestos (aún sin registrar en "Exploración y cambios" de `spec.md`)

- Cursor de productos, clientes y agentes = último `id_erp`, en el real y en el simulador (como en los ejemplos del contrato).
- `modified_since` en productos y clientes responde `501` también en el simulador (contrato §6).
- Sin la bandera `BridgeConfig__Contrato__Expone11`: el `1.1` está aprobado, así que se expone siempre. `/health` reporta `1.1`.
- Clientes reales filtrados a `CTIPOCLIENTE IN (1, 2)` (los proveedores puros no son clientes). Supuesto a verificar en el VPS.
- Existencias reales: se devuelven filas con cantidad distinta de cero (antes solo mayor que cero); lotes solo con `CNUMEROLOTE` no vacío; F-01 usa el ejercicio que contiene hoy, o el de mayor número. Supuesto a verificar en L1-T009.
- La columna del código de la clasificación es `CCODIGOVALORCLASIFICACION`; la referencia la parte en dos renglones y la prueba la compara sin espacios.
- Agregar al registrar: la fila de "Exploración y cambios" de `spec.md` (fecha 2026-10-08, camino L1) con lo anterior.

## Siguiente paso

1. Correr la suite de contrato contra el simulador en :9031 y `dotnet test Polyconecta.slnx`; si pasan, marcar L1-T006 y L1-T007 con "pendiente de verificar en el VPS" y hacer su commit.
2. Terminar L1-T010 (ver arriba) y su commit.
3. L1-T001 a L1-T004: extraer `ISdkNativo` en `Infrastructure/Sdk/ContpaqiSdkGateway.cs`, separar `IniciarSdk` de `AbrirEmpresa`/`CerrarEmpresa`, vigilante de tiempo límite, reinicio diario, pruebas con `ISdkNativo` falso y `TimeProvider` falso.
4. Registrar la exploración en `spec.md` y escribir el reporte final con los pasos del VPS (quickstart §6).

## Bloqueos y dudas

- Sin paquetes nuevos hasta ahora (`Directory.Packages.props` intacto).
- El SQL de lecturas reales (clasificación, domicilios, agentes, F-01) no se puede probar fuera del VPS.
