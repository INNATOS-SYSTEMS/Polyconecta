# Laboratorio SDK CONTPAQi — instrucciones para el agente

> **Retirado (D-130, 2026-10-06).** El desarrollo y las pruebas contra el SDK real se hacen ahora con el bridge en modo debug en el VPS: sigue [`PolyConecta.Contpaq/AGENTS.md`](../../PolyConecta.Contpaq/AGENTS.md). Esta guía queda como registro de cómo se corrió la matriz del SDK (30-sep y 1-oct).

Estás en una estación de trabajo Windows (VPS) con CONTPAQi Comercial Premium + `MGW_SDK.dll`.
Tu trabajo: ejecutar la **matriz de pruebas** (`matrix/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md`) y registrar
evidencia. **No construyes producto ni modificas el repositorio de PolyConecta aquí.**

## Única herramienta: `sdklab.exe`

Toda interacción con el SDK y la base pasa por `sdklab` (ver `sdklab --help`). Salida: un objeto JSON
`{ok, command, result|error}`. No importes ni invoques `MGW_SDK.dll` por otro medio, no abras la BD con
otra herramienta y no uses la UI de CONTPAQi salvo en pasos marcados como MANUAL.

## Reglas duras

1. **Solo empresa de laboratorio.** `sdklab` lo impone (`GUARD_REFUSED`, código de salida 3). Si lo ves, **detente y avisa**; nunca intentes evadirlo editando `lab.config.json`, la propiedad `POLYCONECTA_LAB` ni credenciales.
2. **`rc=0` no es evidencia.** Cada prueba que escribe se verifica con `snapshot` antes/después + `diff` (y `sql` si hace falta). Se han visto funciones que devuelven éxito sin persistir.
3. **Nada es reversible salvo `Reset-Lab.ps1`.** Antes de cada bloque que escribe (B en adelante), confirma con el usuario que hay línea base y corre `snapshot base-<ID>`. No ejecutes `Reset-Lab.ps1` sin que el usuario lo autorice.
4. **Toda referencia de documento empieza con `LAB`** (`sdklab run` lo exige) para poder localizar lo creado.
5. **No inventes.** Si un código, concepto, SKU o lote no lo obtuviste con `sql`, no lo supongas. Si algo no se puede determinar, el resultado es "no concluyente", no "pasa".
6. **Solo pasos de la matriz.** Si ves un hallazgo fuera de ella, anótalo en `evidence/HALLAZGOS.md`; no lo persigas.

## Flujo por prueba

1. `sdklab guard` (una vez por sesión) y `sdklab env` (registra versión de SDK y ejecutable → A-01).
2. Prepara el experimento: consultas `sql` para descubrir códigos reales; `specs/*.json` para escribir.
3. `sdklab snapshot antes-<ID> <SKU...>` → `sdklab run specs/<ID>.json` → `sdklab snapshot despues-<ID> <SKU...>` → `sdklab diff antes-<ID> despues-<ID>`.
4. Compara contra el **criterio de aceptación** de la matriz y registra en `evidence/RESULTADOS.md`:

   `| ID | fecha | resultado ✅/⚠️/❌/❓ | rc y mensaje | evidencia (archivo de snapshot / línea de log) | notas |`

   `❓` = no concluyente. Un ❌ en el bloque B es **punto de control**: detente y reporta al usuario antes de seguir.

## Orden (de la matriz)

A-01…A-05 → B-01…B-05 (punto de control) → C-02 y C-03 primero → resto de C → D → E → F (solo lectura, puede ir en paralelo desde el inicio) → G.

## Pasos MANUALES (no hay función de SDK)

- **A-03/A-04 — alta del almacén WIP**: la referencia del SDK no documenta ninguna función de alta de almacenes (solo `fAltaProducto`, `fAltaCteProv`, `fAltaValorClasif`, `fAltaDireccion`, …). Debe darse de alta por la UI de CONTPAQi en la empresa de laboratorio. Pídeselo al usuario y verifica después con `sql` sobre `admAlmacenes`.

## Pistas del esquema (verifícalas, no las tomes por hechas)

- `admConceptos.CIDDOCUMENTODE = 34` es "Traspasos"; `admMovimientos.CTIPOTRASPASO`: 2 = origen, 3 = destino → sugiere **dos movimientos por traspaso** (B-05).
- `tMovimiento` tiene un solo `aCodAlmacen`; el bridge actual crea un movimiento por lote (`ContpaqiSdkGateway.cs` ~L520).
- G-01 ya se sospecha en el código del bridge: tras un fallo posterior a `fAltaDocumento` queda un documento huérfano. `sdklab run` marca `orphanDocumentLeftBehind`.
