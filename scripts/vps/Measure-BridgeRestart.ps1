<#
.SYNOPSIS
  Mide que el bridge vuelve solo después de reiniciar el VPS (L1-T018, SC-006).

.DESCRIPTION
  Dos pasos:
    -Reiniciar  Anota la hora y reinicia el servidor. No inicies sesión a mano: el inicio de sesión
                automático y la tarea PolyConecta-Bridge deben levantar el bridge solos.
    (sin nada)  Después del reinicio, al reconectar por Escritorio remoto: lee la hora de arranque de
                Windows, la de inicio del proceso del bridge y la primera línea "Now listening" de su
                log, y verifica /health y catalogs/warehouses. Pasa si el bridge escuchaba en menos de
                5 minutos desde que arrancó Windows.

  La evidencia, con las horas, va a tools\sdk-lab\evidence\F0\0.9.md.

  Reconectar por Escritorio remoto a la consola no reinicia el bridge: la sesión es la misma que abrió
  el inicio de sesión automático.

.EXAMPLE
  .\scripts\vps\Measure-BridgeRestart.ps1 -Reiniciar
  .\scripts\vps\Measure-BridgeRestart.ps1
#>
[CmdletBinding()]
param(
    [switch]$Reiniciar,
    [int]$Puerto = 5005,
    [int]$MaximoMinutos = 5
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\_comun.ps1"
$marca = Join-Path $Evidencia 'reinicio-solicitado.txt'

if ($Reiniciar) {
    if (-not (Test-Administrador)) { Falla 'Abre PowerShell como administrador.'; exit 1 }
    if (-not (Get-ScheduledTask -TaskName 'PolyConecta-Bridge' -ErrorAction SilentlyContinue)) { Falla 'No está la tarea PolyConecta-Bridge: corre antes Set-BridgeAutostart.ps1.'; exit 1 }
    New-Item -ItemType Directory -Force $Evidencia | Out-Null
    Get-Date -Format 'o' | Set-Content $marca
    Write-Host ("Reinicio solicitado a las {0}. No inicies sesión a mano; reconecta en unos minutos y corre este script sin -Reiniciar." -f (Get-Date -Format 'HH:mm:ss')) -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    Restart-Computer -Force
    exit 0
}

Write-Host '================================================================='
Write-Host ' Medición del reinicio (L1-T018)'
Write-Host '================================================================='

$arranque = (Get-CimInstance Win32_OperatingSystem).LastBootUpTime
$solicitado = if (Test-Path $marca) { [datetime]::Parse((Get-Content $marca -Raw).Trim()) } else { $null }
$proceso = Get-Process -Name 'Contpaq.Bridge' -ErrorAction SilentlyContinue | Sort-Object StartTime | Select-Object -First 1

$escuchando = $null
$log = Get-ChildItem (Join-Path $CarpetaBridge 'logs') -Filter 'bridge-*.log' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($log) {
    foreach ($l in (Select-String -Path $log.FullName -Pattern 'Now listening' -SimpleMatch)) {
        if ($l.Line -match '^(\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2})') {
            $t = [datetime]::Parse($Matches[1])
            if ($t -ge $arranque) { $escuchando = $t; break }
        }
    }
}

$salud = Invoke-Bridge '/health' $Puerto
$almacenes = Invoke-Bridge '/api/v1/catalogs/warehouses' $Puerto

Paso 'Resultado'
$lineas = @()
if ($solicitado) { $lineas += ("Reinicio solicitado: {0:yyyy-MM-dd HH:mm:ss}" -f $solicitado) }
$lineas += ("Windows arrancó:     {0:yyyy-MM-dd HH:mm:ss}" -f $arranque)
if ($proceso) { $lineas += ("Bridge inició:       {0:yyyy-MM-dd HH:mm:ss} (PID {1})" -f $proceso.StartTime, $proceso.Id) } else { $lineas += 'Bridge: no hay proceso Contpaq.Bridge' }
if ($escuchando) { $lineas += ("Bridge escuchando:   {0:yyyy-MM-dd HH:mm:ss}" -f $escuchando) } else { $lineas += 'Bridge escuchando: no se encontró "Now listening" en el log de este arranque' }
$lineas += ('/health: ' + $(if ($salud) { $salud | ConvertTo-Json -Compress } else { 'sin respuesta' }))
$lineas += ('catalogs/warehouses: ' + $(if ($null -ne $almacenes) { 'responde' } else { 'sin respuesta' }))
$lineas | ForEach-Object { Write-Host "  $_" }

$pasa = $false
if ($escuchando) {
    $min = ($escuchando - $arranque).TotalMinutes
    $lineas += ("Tiempo desde el arranque de Windows: {0:N1} min (máximo {1})" -f $min, $MaximoMinutos)
    $pasa = $min -le $MaximoMinutos -and $salud -and $salud.mode -eq 'Real' -and $salud.sdk_initialized -eq $true -and $null -ne $almacenes
}
Write-Host ''
if ($pasa) { Ok ("SC-006 cumplido: el bridge volvió solo en {0:N1} min y lee con el login de solo lectura." -f $min) }
else { Falla 'SC-006 no se cumple todavía: revisa las líneas de arriba, la tarea PolyConecta-Bridge y el log del bridge.' }
$lineas += ('Resultado: ' + $(if ($pasa) { 'cumple SC-006' } else { 'no cumple' }))

Add-Evidencia '0.9.md' 'Reinicio medido (L1-T018)' $lineas
if (Test-Path $marca) { Remove-Item $marca }
if (-not $pasa) { exit 1 }
