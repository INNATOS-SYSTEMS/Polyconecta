# Bridge de CONTPAQi en el VPS: guía para el agente

Estás en el VPS de Windows donde corre CONTPAQi Comercial, para desarrollar, depurar y probar el bridge (`PolyConecta.Contpaq`) contra el SDK real (D-130). Esta guía sustituye a `tools/sdk-lab/`, que quedó retirado. Las reglas generales del repositorio (`AGENTS.md` de la raíz) también aplican.

## Antes de empezar

1. **Trabaja en la sesión de Escritorio remoto del administrador**, nunca por SSH. El SDK solo abre la empresa en una sesión iniciada de Windows (S-02, D-115). Lo que lances desde una consola de esa sesión, incluido el bridge, hereda la sesión.
2. **Prepara el entorno** una vez: `powershell -ExecutionPolicy Bypass -File .\scripts\vps\Initialize-BridgeDebug.ps1`. Comprueba la sesión, .NET 10 (con los runtimes x86), el SDK de CONTPAQi, la empresa, las variables de entorno y el puerto. No sigas mientras marque `[falta]`.
3. **Trabaja en la rama de la spec en curso** (`NNN-<fase>`), nunca en `main` (CT-44). Haz commit al terminar cada cambio verificado; sube la rama solo cuando el usuario lo pida.

## Arrancar el bridge

```powershell
.\scripts\vps\Start-BridgeDebug.ps1            # modo Real: abre la empresa de CONTPAQi y escribe en ella
.\scripts\vps\Start-BridgeDebug.ps1 -Simulado  # sin SDK: para lógica del contrato que no toca CONTPAQi
```

Compila en Debug y en x86 (`-p:Bridge32=true`), porque `MGW_SDK.dll` es de 32 bits. Escucha en `http://localhost:5005`. Los logs salen en la consola. Para depurar paso a paso, el usuario asocia Visual Studio al proceso `Contpaq.Bridge.exe`.

## Reglas duras

1. **No hay empresa de laboratorio.** En modo Real el bridge escribe en la empresa de CONTPAQi de `BridgeConfig__CompanyPath`, con datos reales (D-130). Por eso:
   - **Antes de mandar cualquier comando que escriba**, muestra al usuario la carga completa y espera su confirmación explícita. Escriben: `POST /api/v1/transactions`, el reintento de una transacción, `POST /api/v1/dlq/{id}/edit-and-retry` y cualquier prueba que haga alguno de estos. Las lecturas (`GET`) no necesitan confirmación.
   - Pon `"referencia_negocio": "DEV-<algo>"` en toda carga de prueba, para poder encontrar en CONTPAQi lo que se creó al depurar.
   - Si algo quedó escrito por error, detente y avisa. No intentes deshacerlo con otro comando sin que el usuario lo decida.
2. **Nunca escribas directo en las tablas `adm*`** (Principio II). Toda escritura va por el SDK, a través del bridge. Para leer usa el login de solo lectura `polyconecta_bridge_ro`; no uses `sa` (D-129).
3. **`rc=0` no es evidencia.** Una escritura se verifica leyendo `adm*` con el login de solo lectura antes y después: se han visto funciones del SDK que devuelven éxito sin guardar.
4. **No inventes códigos.** Productos, almacenes, clientes, conceptos y monedas se obtienen leyendo CONTPAQi, no se suponen. Toda afirmación sobre el SDK o las tablas `adm*` se respalda en `docs/contpaq/` o en una prueba que corriste (Principio VII). Si algo no se puede determinar, el resultado es "no concluyente".
5. **El contrato no se cambia solo.** `docs/contratos/bridge-v1.md` necesita a los dos líderes (CT-22). Si el SDK obliga a cambiarlo, regístralo en "Exploración y cambios" de la spec y avisa.
6. **Sin secretos** en el repositorio, en los logs que copies ni en la evidencia (CT-29). De una variable de entorno solo se dice si existe.

## Configuración

| Variable de usuario | Para qué |
| :--- | :--- |
| `BridgeConfig__SqlConnectionString` | Lecturas de `adm*` con `polyconecta_bridge_ro` (L1-T002) |
| `BridgeConfig__CallbackSecret` | Firma de los callbacks; la API y la suite de contrato usan la misma |
| `BridgeConfig__CompanyPath`, `BridgeConfig__SdkPath` | Empresa y SDK de CONTPAQi, si no son los de `appsettings.json` |
| `BridgeConfig__Conceptos__{COMANDO}__{VARIANTE}__{rol}` | Códigos reales de los conceptos (D-121). Los de `appsettings.json` son de ejemplo y no existen en CONTPAQi |
| `BridgeConfig__Monedas__{ISO}` | `CIDMONEDA` de cada moneda, confirmado contra `admMonedas` |

## Pruebas

| Qué | Cómo | Escribe en CONTPAQi |
| :--- | :--- | :---: |
| Pruebas del bridge | `dotnet test --project tests\Contpaq.Bridge.Tests` | No |
| Suite de contrato contra el bridge real | Con el bridge arriba: `$env:BRIDGE_URL='http://localhost:5005'`, `$env:BRIDGE_CALLBACK_SECRET` igual a `BridgeConfig__CallbackSecret`, y `dotnet test --project tests\PolyConecta.Contract.Tests`. Las pruebas de `Simulado/` se omiten solas | **Sí**: las cargas válidas crean documentos. Corre la suite solo con la confirmación del usuario y después de revisar los códigos de `docs/contratos/ejemplos/` |
| Lecturas | `GET /api/v1/catalogs/*` y `/api/v1/inventory/*` | No |

## Dónde se anota

- Hallazgos y cambios: "Exploración y cambios" de la spec en curso.
- Evidencia de las tareas del VPS: `tools/sdk-lab/evidence/F0/`.
- Hechos nuevos del SDK: `docs/contpaq/`, con la prueba que los respalda.
