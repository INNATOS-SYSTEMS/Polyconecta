# Scripts del VPS

Para el bridge de CONTPAQi en el servidor Windows (D-115, D-130). Se corren en la **sesión de Escritorio remoto del administrador**, nunca por SSH: el SDK solo abre la empresa en una sesión iniciada (S-02).

Primero actualiza el repositorio en el VPS a la rama de la spec en curso. La evidencia que dejan los scripts va a `tools/sdk-lab/evidence/F0/`, que git ignora: queda solo en el VPS.

## Orden para dejar el bridge funcionando (F0)

| Paso | Script | Tarea | Consola |
| :---: | :--- | :--- | :--- |
| 1 | `Initialize-BridgeDebug.ps1` | Comprueba el entorno: sesión, .NET 10 x86, SDK de CONTPAQi, empresa, variables y puerto. Repítelo hasta que no marque `[falta]` | Normal |
| 2 | `Publish-Bridge.ps1` | L1-T006: publica en `C:\PolyConecta\bridge` en x86, lo arranca y verifica `/health` y `catalogs/warehouses` | Normal |
| 3 | `Set-BridgeAutostart.ps1` | L1-T016: inicio de sesión automático del administrador (Autologon de Sysinternals) y tarea `PolyConecta-Bridge` al iniciar sesión | **Administrador** |
| 4 | `Remove-BridgeTestUser.ps1` | L1-T017: quita el usuario de prueba `polyconecta-bridge` y su tarea de S-04. Se niega si el inicio de sesión automático todavía apunta a él | **Administrador** |
| 5 | `Measure-BridgeRestart.ps1 -Reiniciar` | L1-T018: anota la hora y reinicia. No inicies sesión a mano | **Administrador** |
| 6 | `Measure-BridgeRestart.ps1` | L1-T018: al reconectar, mide arranque de Windows → bridge escuchando (máximo 5 min) y verifica las dos llamadas | Normal |

Para correr un `.ps1` descargado: `powershell -ExecutionPolicy Bypass -File .\scripts\vps\<script>.ps1`.

## Desarrollo

| Script | Para qué |
| :--- | :--- |
| `Start-BridgeDebug.ps1` | Corre el bridge en Debug y x86 en esa consola, en modo Real o con `-Simulado`. Ver `PolyConecta.Contpaq/AGENTS.md` |

Detén la tarea `PolyConecta-Bridge` (o el `Contpaq.Bridge.exe` publicado) antes de depurar: los dos usan el puerto 5005.

## Notas

- Ningún script guarda secretos. Las contraseñas se piden al correrlos.
- `scripts/deploy.sh` publicaba en IIS, que ya no se usa en el VPS: queda obsoleto (spec 002, exploración del 6-oct).
- `_comun.ps1` tiene las funciones compartidas; no se corre solo.
