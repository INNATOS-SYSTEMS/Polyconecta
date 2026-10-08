# Scripts del VPS

Para el bridge de CONTPAQi en el servidor Windows (D-115, D-130). Se corren en la **sesión de Escritorio remoto del administrador**, nunca por SSH: el SDK solo abre la empresa en una sesión iniciada (S-02).

Primero actualiza el repositorio en el VPS a la rama de la spec en curso. La evidencia que dejan los scripts va a `tools/sdk-lab/evidence/F0/`, que git ignora: queda solo en el VPS.

## Orden para dejar el bridge funcionando (F0)

| Paso | Script | Tarea | Consola |
| :---: | :--- | :--- | :--- |
| 1 | `Initialize-BridgeDebug.ps1` | Comprueba el entorno: sesión, .NET 10 x86, SDK de CONTPAQi, empresa, variables y puerto. Repítelo hasta que no marque `[falta]` | Normal |
| 2 | `Publish-Bridge.ps1` | L1-T006: publica en `C:\PolyConecta\bridge` en x86, lo arranca y verifica `/health` y `catalogs/warehouses` | Normal |
| 3 | `Set-BridgeAutostart.ps1` | L1-T016 y L1-T011: inicio de sesión automático del administrador (Autologon de Sysinternals) y tarea `PolyConecta-Bridge` al iniciar sesión, que corre el supervisor `Start-BridgeSupervisado.ps1`. Vuelve a correrlo para reinstalar la tarea | **Administrador** |
| 4 | `Remove-BridgeTestUser.ps1` | L1-T017: quita el usuario de prueba `polyconecta-bridge` y su tarea de S-04. Se niega si el inicio de sesión automático todavía apunta a él | **Administrador** |
| 5 | `Measure-BridgeRestart.ps1 -Reiniciar` | L1-T018: anota la hora y reinicia. No inicies sesión a mano | **Administrador** |
| 6 | `Measure-BridgeRestart.ps1` | L1-T018: al reconectar, mide arranque de Windows → bridge escuchando (máximo 5 min) y verifica las dos llamadas | Normal |

Para correr un `.ps1` descargado: `powershell -ExecutionPolicy Bypass -File .\scripts\vps\<script>.ps1`.

## Desarrollo

| Script | Para qué |
| :--- | :--- |
| `Start-BridgeSupervisado.ps1` | Supervisor del bridge publicado (L1-T011); lo corre la tarea `PolyConecta-Bridge`. Parámetro `-Exe` (por omisión `C:\PolyConecta\bridge\Contpaq.Bridge.exe`) |
| `Start-BridgeDebug.ps1` | Corre el bridge en Debug y x86 en esa consola, en modo Real o con `-Simulado`. Ver `PolyConecta.Contpaq/AGENTS.md` |

El bridge publicado corre **sin ventana** (6-oct: cerrar su consola por error lo detenía) y **bajo un supervisor** (L1-T011). El bridge sale solo con código 3 (una llamada al SDK no regresó en `BridgeConfig__Sdk__TimeoutSegundos`) y con código 0 (reinicio diario de `BridgeConfig__ReinicioDiario`); el supervisor lo relanza a los 5 segundos. Con cualquier otro código lo relanza con espera creciente (5 s, 15 s, 60 s, 120 s, 240 s, tope de 5 min) y, si falla 5 veces en menos de 10 minutos, deja de intentar y lo anota. Solo corre un supervisor a la vez.

```powershell
Get-Content C:\PolyConecta\bridge\logs\bridge-*.log -Wait -Tail 50       # log en vivo del bridge
Get-Content C:\PolyConecta\bridge\logs\supervisor-*.log -Tail 20         # salidas del bridge (fecha, hora y código) y relanzamientos
Stop-ScheduledTask PolyConecta-Bridge; Stop-Process -Name Contpaq.Bridge   # detener supervisor y bridge, por ejemplo antes de depurar
Start-ScheduledTask PolyConecta-Bridge                                      # volver a levantarlos
```

Detener solo el bridge (`Stop-Process`) no sirve: el supervisor lo relanza. `Publish-Bridge.ps1` detiene primero el supervisor y el bridge, y al terminar arranca la tarea. `Publish-Bridge.ps1 -Visible` lo arranca con su consola y sin supervisor, para depurar. Detén el bridge publicado antes de usar `Start-BridgeDebug.ps1`: los dos usan el puerto 9030 (D-128).

## Notas

- Ningún script guarda secretos. Las contraseñas se piden al correrlos.
- `scripts/deploy.sh` publicaba en IIS, que ya no se usa en el VPS: queda obsoleto (spec 002, exploración del 6-oct).
- `_comun.ps1` tiene las funciones compartidas; no se corre solo.
