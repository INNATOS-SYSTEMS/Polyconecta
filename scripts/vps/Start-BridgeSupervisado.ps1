<#
.SYNOPSIS
  Supervisor del bridge publicado: lo relanza cuando sale solo (L1-T011, D-115).

.DESCRIPTION
  El bridge sale a propósito en dos casos y no se puede quedar caído:
    - código 3: una llamada al SDK no regresó en BridgeConfig__Sdk__TimeoutSegundos (L1-T003);
    - código 0: reinicio diario de BridgeConfig__ReinicioDiario (L1-T004).
  La tarea programada "PolyConecta-Bridge" solo lo lanzaba y terminaba, así que el Programador de
  tareas no tenía nada que reiniciar. Esta tarea ahora corre este supervisor, que sigue vivo mientras
  el bridge corre.

  Lanza Contpaq.Bridge.exe, espera a que termine y anota cada salida (fecha, hora y código) en
  <carpeta del bridge>\logs\supervisor-AAAAMMDD.log.
    - Código 0 o 3: lo relanza a los 5 segundos.
    - Otro código: lo relanza con espera creciente (5 s, 15 s, 60 s, 120 s, 240 s, tope de 5 min). Si
      falla 5 veces en menos de 10 minutos, deja de intentar y lo anota en el log (sale con código 1).
  Solo corre un supervisor a la vez: si ya hay uno, el nuevo termina sin hacer nada.

  Se corre en la sesión del administrador (el SDK la necesita, S-02). Lee las variables de usuario
  BridgeConfig__* del registro antes de cada arranque, para que un cambio de configuración se tome en
  el siguiente relanzamiento sin cerrar sesión. No guarda ni muestra secretos.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\scripts\vps\Start-BridgeSupervisado.ps1
#>
[CmdletBinding()]
param(
    [string]$Exe = 'C:\PolyConecta\bridge\Contpaq.Bridge.exe'
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\_comun.ps1"

$carpeta = Split-Path $Exe -Parent
$carpetaLogs = Join-Path $carpeta 'logs'
New-Item -ItemType Directory -Force $carpetaLogs | Out-Null

function Write-LogSupervisor([string]$mensaje) {
    $ahora = Get-Date
    $ruta = Join-Path $carpetaLogs ('supervisor-{0}.log' -f $ahora.ToString('yyyyMMdd'))
    Add-Content -Path $ruta -Value ('{0} {1}' -f $ahora.ToString('yyyy-MM-dd HH:mm:ss'), $mensaje) -Encoding UTF8
}

# Un solo supervisor a la vez (en esta sesión).
$candado = New-Object System.Threading.Mutex($false, 'Local\PolyConecta-BridgeSupervisor')
if (-not $candado.WaitOne(0)) {
    Write-LogSupervisor "Ya hay un supervisor corriendo: este (PID $PID) termina sin hacer nada."
    exit 0
}

if (-not (Test-Path $Exe)) {
    Write-LogSupervisor "No está $Exe: el supervisor termina. Corre Publish-Bridge.ps1."
    $candado.ReleaseMutex()
    exit 1
}

$esperas = @(5, 15, 60, 120, 240, 300)   # segundos, tope de 5 min
$fallos = New-Object System.Collections.Generic.List[datetime]
Write-LogSupervisor "Supervisor iniciado (PID $PID) para $Exe."

try {
    while ($true) {
        # Las variables de usuario se leen del registro aunque esta sesión sea anterior a su cambio.
        foreach ($k in [Environment]::GetEnvironmentVariables('User').Keys) {
            if ($k -like 'BridgeConfig__*') { [Environment]::SetEnvironmentVariable($k, [Environment]::GetEnvironmentVariable($k, 'User'), 'Process') }
        }

        Write-LogSupervisor 'Arrancando el bridge.'
        $proceso = Start-Process -FilePath $Exe -WorkingDirectory $carpeta -WindowStyle Hidden -Wait -PassThru
        $codigo = $proceso.ExitCode

        if ($codigo -eq 0) {
            $fallos.Clear()
            Write-LogSupervisor 'El bridge salió con código 0 (reinicio diario). Se relanza en 5 s.'
            Start-Sleep -Seconds 5
        } elseif ($codigo -eq 3) {
            $fallos.Clear()
            Write-LogSupervisor 'El bridge salió con código 3 (tiempo límite de una llamada al SDK). Se relanza en 5 s.'
            Start-Sleep -Seconds 5
        } else {
            $ahora = Get-Date
            $fallos.Add($ahora)
            $fallos.RemoveAll([Predicate[datetime]]{ param($t) ($ahora - $t).TotalMinutes -ge 10 }) | Out-Null
            if ($fallos.Count -ge 5) {
                Write-LogSupervisor "El bridge salió con código $codigo y ya son $($fallos.Count) fallas en menos de 10 minutos: el supervisor deja de intentar. Revisa logs\bridge-*.log y relanza la tarea."
                exit 1
            }
            $espera = $esperas[[Math]::Min($fallos.Count - 1, $esperas.Count - 1)]
            Write-LogSupervisor "El bridge salió con código $codigo (falla $($fallos.Count) de 5 en 10 min). Se relanza en $espera s."
            Start-Sleep -Seconds $espera
        }
    }
} finally {
    $candado.ReleaseMutex()
}
