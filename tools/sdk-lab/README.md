# sdk-lab — estación de laboratorio del SDK de CONTPAQi

Paquete autocontenido para ejecutar la [matriz de pruebas del SDK](../../docs/contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md)
con un agente, en el VPS Windows donde vive CONTPAQi + `MGW_SDK.dll`.

**Qué contiene**: `sdklab.exe` (x86, autocontenido; usa el mismo `ContpaqiSdkNative.cs` que el bridge), scripts de
instalación/línea base/reset, instrucciones del agente (`AGENTS.md`, `CLAUDE.md`), plantillas de experimentos
(`specs/`), la matriz y las referencias del SDK y de la BD.

**Qué NO contiene**: credenciales, ni el agente mismo (ver "Dónde corre el agente").

## Seguridad — por qué es seguro dejar a un agente aquí

| Riesgo | Control |
| :--- | :--- |
| Escribir en la empresa viva | `sdklab` se niega si la base no termina en `_LAB`, no coincide con la ruta de empresa, está en `forbiddenDatabases` o no lleva la propiedad extendida `POLYCONECTA_LAB=1` (código de salida 3) |
| SQL destructivo | Login `sdklab_ro` con `db_datareader` únicamente; además solo se acepta un `SELECT` |
| Secretos en el repo | La contraseña del login RO vive en una variable de entorno de la máquina; el empaquetador aborta si detecta una cadena de conexión con credenciales |
| Daño irreversible en pruebas | Línea base `.bak` + `Reset-Lab.ps1`; el agente no lo corre sin autorización |
| Evidencia inventada | Cada llamada queda en `evidence/log-*.jsonl`; `AGENTS.md` prohíbe dar `rc=0` como prueba |

## Preparación (una vez, MANUAL)

1. **Crea la empresa de laboratorio** en CONTPAQi restaurando un respaldo de producción con otro nombre (`<BD>_LAB`),
   con la herramienta de CONTPAQi para crear/restaurar empresas. Esto no se puede automatizar aquí: la empresa debe quedar
   *registrada* en CONTPAQi, no solo como base en SQL Server.
2. Verifica que existe un SKU con control de lotes y existencia en dos lotes (requisito de la matriz).

## Instalación

Desde el Mac: `./scripts/Build-Package.sh` → `dist/sdklab-workstation.zip`. Cópialo al VPS
(`scp dist/sdklab-workstation.zip vps-innatos:C:/sdklab/`) y descomprímelo en `C:\sdklab`. Luego, en PowerShell como administrador:

```powershell
cd C:\sdklab
.\scripts\Install-Workstation.ps1 -SqlServer 'localhost\COMPAC01' -LabDatabase 'adPOLYEMPAQUES_LAB' -LiveDatabase 'adPOLYEMPAQUES'
# abre una sesión nueva, y:
.\sdklab.exe env        # debe mostrar process32bit=true, sdkDllPresent=true, guard.ok=true
.\scripts\New-Baseline.ps1
```

Revisa `lab.config.json` (rutas reales de SDK y empresa) antes de correr `env`.

## Dónde corre el agente

- **Opción A — Claude Code dentro del VPS**: instala Claude Code y autentícalo *ahí*; ábrelo en `C:\sdklab`
  (lee `CLAUDE.md` → `AGENTS.md`). Ventaja: sin latencia SSH. Costo: una credencial de IA viva en un servidor que también tiene datos de producción.
- **Opción B — agente en tu Mac, ejecutando por SSH** (recomendada para empezar): el agente corre
  `ssh vps-innatos "C:\sdklab\sdklab.exe <comando>"`. No hay credenciales de IA en el VPS, y el agente solo puede hacer lo que `sdklab` permite.
  Al ser sin estado y con salida JSON, funciona igual.

En ambos casos el agente lee y escribe solo `evidence/`.

## Después de correr

Copia `evidence/RESULTADOS.md` al *Registro de resultados* de la matriz en el repo y guarda `evidence/` completo como anexo.
El bloque B decide la viabilidad de WIP como almacén contable; si falla, se detiene y se reporta.

## Límites conocidos (léelos)

- **Compilado, no ejecutado contra el SDK real.** Se construyó y compiló en Mac; no había CONTPAQi disponible. El primer `env`/`sdk-open` en el VPS es la prueba real del interop.
- `fAltaMovimientoSeriesCapas_Param` no está importada (la referencia documenta sus tipos de forma dudosa; una firma stdcall equivocada corrompe la pila en x86). Se añade si la matriz la necesita.
- No hay función de SDK para dar de alta almacenes: A-03/A-04 son manuales por la UI.
- Los nombres de columnas de `snapshot` vienen de `Referencia_BD_CONTPAQi.md`; si la versión instalada difiere, el error de SQL lo dirá.
