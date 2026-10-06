<#
.SYNOPSIS
  Publica el bridge (.NET 10, x86) en C:\PolyConecta\bridge, lo arranca y lo verifica (L1-T006, D-130).

.DESCRIPTION
  1. Comprueba sesión interactiva y que nada más use el puerto (salvo un bridge anterior, que detiene).
  2. Publica en Release con -p:Bridge32=true a una carpeta temporal y la copia a C:\PolyConecta\bridge
     sin tocar la base local del bridge (bridge_outbox.db) ni sus logs.
  3. Arranca Contpaq.Bridge.exe en una ventana aparte, con las variables de usuario (D-115).
  4. Verifica GET /health (mode Real, sdk_initialized true) y GET /api/v1/catalogs/warehouses, que
     lee con el login de solo lectura (cierra la verificación pendiente de L1-T002).
  5. Escribe la evidencia, sin secretos, en tools\sdk-lab\evidence\F0\0.4.md.

  Antes, corre Initialize-BridgeDebug.ps1 y resuelve lo que marque [falta].

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\scripts\vps\Publish-Bridge.ps1
#>
[CmdletBinding()]
param(
    [int]$Puerto = 5005,
    [switch]$SinArrancar
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\_comun.ps1"

Write-Host '================================================================='
Write-Host ' Publicar el bridge en el VPS (L1-T006)'
Write-Host '================================================================='

Paso '1. Comprobaciones'
if ($env:SSH_CONNECTION -or $env:SSH_CLIENT) { Falla 'Consola de SSH: abre Escritorio remoto. El SDK necesita la sesión iniciada (S-02).'; exit 1 }
Ok ("Sesión interactiva de {0}" -f $env:USERNAME)

$anterior = Get-Process -Name 'Contpaq.Bridge' -ErrorAction SilentlyContinue
$ocupado = Get-NetTCPConnection -LocalPort $Puerto -State Listen -ErrorAction SilentlyContinue
if ($ocupado -and -not ($anterior | Where-Object { $_.Id -eq $ocupado[0].OwningProcess })) {
    Falla ("El puerto {0} lo usa otro proceso ({1}), no el bridge." -f $Puerto, $ocupado[0].OwningProcess); exit 1
}
if ($anterior) {
    Aviso ("Hay un bridge corriendo (PID {0}): se detiene para reemplazarlo." -f (($anterior | ForEach-Object Id) -join ', '))
    $anterior | Stop-Process -Force
    Start-Sleep -Seconds 2
}

Paso '2. Publicar (Release, x86)'
$temporal = Join-Path $env:TEMP ('bridge-publish-' + [guid]::NewGuid().ToString('N'))
Push-Location $Repo
try {
    $commit = (& git rev-parse --short HEAD 2>$null)
    & dotnet publish PolyConecta.Contpaq -c Release -p:Bridge32=true -o $temporal --nologo
    if ($LASTEXITCODE -ne 0) { Falla 'dotnet publish falló.'; exit 1 }
} finally { Pop-Location }

New-Item -ItemType Directory -Force $CarpetaBridge | Out-Null
# /XF y /XD conservan la base local del bridge y sus logs entre publicaciones.
& robocopy $temporal $CarpetaBridge /MIR /NFL /NDL /NJH /NJS /NP /XF 'bridge_outbox.db*' /XD 'logs' | Out-Null
if ($LASTEXITCODE -ge 8) { Falla "robocopy falló (código $LASTEXITCODE)."; exit 1 }
Remove-Item -Recurse -Force $temporal
$exe = Join-Path $CarpetaBridge 'Contpaq.Bridge.exe'
Ok ("Publicado en {0} (commit {1})" -f $CarpetaBridge, $commit)

if ($SinArrancar) { Write-Host ''; Write-Host 'Publicado sin arrancar (-SinArrancar).'; exit 0 }

Paso '3. Arrancar'
# Las variables de usuario se leen aunque esta consola sea anterior a Initialize-BridgeDebug.ps1.
foreach ($k in [Environment]::GetEnvironmentVariables('User').Keys) {
    if ($k -like 'BridgeConfig__*') { [Environment]::SetEnvironmentVariable($k, [Environment]::GetEnvironmentVariable($k, 'User'), 'Process') }
}
$env:BridgeConfig__DashboardPort = "$Puerto"
Start-Process -FilePath $exe -WorkingDirectory $CarpetaBridge
Write-Host '  Esperando a que responda (hasta 2 minutos; abrir la sesión del SDK tarda unos segundos)...'
$salud = $null
for ($i = 0; $i -lt 40 -and -not $salud; $i++) { Start-Sleep -Seconds 3; $salud = Invoke-Bridge '/health' $Puerto }
if (-not $salud) { Falla 'El bridge no respondió /health. Revisa su ventana y la carpeta logs.'; exit 1 }

Paso '4. Verificar'
$lineas = @("Commit: $commit", "Carpeta: $CarpetaBridge", ("Arquitectura: {0}" -f $salud.worker_architecture))
$lineas += ('/health: ' + ($salud | ConvertTo-Json -Compress))
if ($salud.mode -eq 'Real') { Ok 'mode: Real' } else { Falla ("mode: {0} (se esperaba Real)" -f $salud.mode) }
if ($salud.sdk_initialized -eq $true) { Ok 'sdk_initialized: true' } else { Falla 'sdk_initialized no es true: el SDK no abrió la empresa.' }
if ($salud.worker_architecture -eq 'x86') { Ok 'Proceso de 32 bits' } else { Aviso ("worker_architecture: {0}" -f $salud.worker_architecture) }

$almacenes = Invoke-Bridge '/api/v1/catalogs/warehouses' $Puerto
if ($null -ne $almacenes) {
    $lista = @($almacenes)
    if ($almacenes.PSObject.Properties.Name -contains 'items') { $lista = @($almacenes.items) }
    Ok ("catalogs/warehouses responde: {0} almacenes leídos con el login de solo lectura" -f $lista.Count)
    $lineas += ("catalogs/warehouses: {0} almacenes; primeros: {1}" -f $lista.Count, (($lista | Select-Object -First 3 | ConvertTo-Json -Compress -Depth 3)))
} else {
    Falla 'catalogs/warehouses no respondió. Revisa BridgeConfig__SqlConnectionString (L1-T002).'
    $lineas += 'catalogs/warehouses: sin respuesta'
}

Add-Evidencia '0.4.md' 'Publicación del bridge .NET 10 x86 (L1-T006)' $lineas
Write-Host ''
Write-Host 'Siguiente: Set-BridgeAutostart.ps1 (L1-T016), para que arranque solo al reiniciar.'
