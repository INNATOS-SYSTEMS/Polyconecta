<#
.SYNOPSIS
  Publica el bridge (.NET 10, x86) en C:\PolyConecta\bridge, lo arranca y lo verifica (L1-T006, D-130).

.DESCRIPTION
  1. Comprueba sesión interactiva y que nada más use el puerto (salvo un bridge anterior, que detiene).
  2. Publica en Release con -p:Bridge32=true a una carpeta temporal y la copia a C:\PolyConecta\bridge
     sin tocar la base local del bridge (bridge_outbox.db) ni sus logs.
  3. Arranca Contpaq.Bridge.exe sin ventana, con las variables de usuario (D-115). Con -Visible abre su
     consola, para depurar; cerrar esa ventana detiene el bridge.
  4. Verifica GET /health (mode Real, proceso x86) y GET /api/v1/catalogs/warehouses, que lee con el
     login de solo lectura (cierra la verificación pendiente de L1-T002).
  5. Sonda del SDK: el bridge abre la sesión del SDK solo cuando tiene un comando pendiente. Le manda
     un ALTA_ALMACEN de sonda; en F0 ningún comando escribe en CONTPAQi (responde SDK_ERROR con motivo
     COMANDO_NO_IMPLEMENTADO), pero antes abre la sesión (fInicializaSDK y fAbreEmpresa). Pasa si la
     respuesta es exactamente ese motivo y /health queda con sdk_initialized true. Si el comando ya
     estuviera implementado, la sonda lo detecta y avisa. Usa -SinSonda para omitirla.
  6. Escribe la evidencia, sin secretos, en tools\sdk-lab\evidence\F0\0.4.md.

  Antes, corre Initialize-BridgeDebug.ps1 y resuelve lo que marque [falta].

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\scripts\vps\Publish-Bridge.ps1
#>
[CmdletBinding()]
param(
    [int]$Puerto = 5005,
    [switch]$SinArrancar,
    [switch]$SinSonda,
    [switch]$Visible
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
if ($Visible) { Start-Process -FilePath $exe -WorkingDirectory $CarpetaBridge } else { Start-BridgeOculto }
Write-Host '  Esperando a que responda (hasta 2 minutos; abrir la sesión del SDK tarda unos segundos)...'
$salud = $null
for ($i = 0; $i -lt 40 -and -not $salud; $i++) { Start-Sleep -Seconds 3; $salud = Invoke-Bridge '/health' $Puerto }
if (-not $salud) { Falla 'El bridge no respondió /health. Revisa su ventana y la carpeta logs.'; exit 1 }

Paso '4. Verificar'
$lineas = @("Commit: $commit", "Carpeta: $CarpetaBridge", ("Arquitectura: {0}" -f $salud.worker_architecture))
$lineas += ('/health: ' + ($salud | ConvertTo-Json -Compress))
if ($salud.mode -eq 'Real') { Ok 'mode: Real' } else { Falla ("mode: {0} (se esperaba Real)" -f $salud.mode) }
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

if (-not $SinSonda) {
    Paso '5. Sonda del SDK (abre la empresa; en F0 no escribe)'
    $clave = 'dev:sonda-sdk:' + (Get-Date -Format 'yyyyMMddHHmmss')
    $sonda = @{
        contract_version = '1.0'; command_type = 'ALTA_ALMACEN'; variant = $null
        idempotency_key = $clave; correlation_id = [guid]::NewGuid().ToString()
        client_app_id = 'polyconecta'; callback_url = 'http://localhost:9/sonda'
        payload = @{ codigo = 'DEV-SONDA'; nombre = 'DEV sonda del SDK (no se crea)'; fecha_alta = (Get-Date -Format 'yyyy-MM-dd') }
    } | ConvertTo-Json -Depth 4
    try {
        $acuse = Invoke-RestMethod -Method Post -Uri ("http://localhost:{0}/api/v1/transactions" -f $Puerto) -ContentType 'application/json' -Body $sonda -TimeoutSec 15
    } catch { $acuse = $null }
    if (-not $acuse) {
        Falla 'El bridge no aceptó la sonda.'
        $lineas += 'Sonda: no aceptada'
    } else {
        $tx = $null
        for ($i = 0; $i -lt 30; $i++) {
            Start-Sleep -Seconds 2
            $tx = Invoke-Bridge ("/api/v1/transactions/{0}" -f $acuse.transaction_id) $Puerto
            if ($tx -and $tx.status -ne 'PENDING' -and $tx.status -ne 'PROCESSING') { break }
        }
        $motivo = $null
        if ($tx -and $tx.error -and $tx.error.detail) { $motivo = $tx.error.detail.motivo }
        $salud = Invoke-Bridge '/health' $Puerto
        if ($motivo -eq 'COMANDO_NO_IMPLEMENTADO' -and $salud.sdk_initialized -eq $true) {
            Ok 'El SDK abrió la empresa (sdk_initialized: true) y la sonda no escribió: COMANDO_NO_IMPLEMENTADO.'
        } elseif ($tx -and $tx.status -eq 'CONFIRMED') {
            Falla 'La sonda se CONFIRMÓ: ALTA_ALMACEN ya escribe en CONTPAQi. Revisa si se creó el almacén DEV-SONDA y deja de usar esta sonda.'
        } elseif (-not $tx -or $tx.status -eq 'PENDING') {
            Falla 'La sonda sigue pendiente: el SDK no pudo abrir la empresa. Busca fInicializaSDK, fAbreEmpresa o ARCHITECTURE en C:\PolyConecta\bridge\logs.'
        } else {
            Falla ("Resultado inesperado de la sonda: estado {0}, código {1}, motivo {2}." -f $tx.status, $tx.error.code, $motivo)
        }
        $lineas += ("Sonda {0}: estado {1}, código {2}, motivo {3}" -f $acuse.transaction_id, $tx.status, $tx.error.code, $motivo)
        $lineas += ('/health tras la sonda: ' + ($salud | ConvertTo-Json -Compress))
        try { Invoke-RestMethod -Method Delete -Uri ("http://localhost:{0}/api/v1/transactions/{1}" -f $Puerto, $acuse.transaction_id) -TimeoutSec 15 | Out-Null } catch { }
    }
}

Add-Evidencia '0.4.md' 'Publicación del bridge .NET 10 x86 (L1-T006)' $lineas
Write-Host ''
Write-Host ''
Write-Host 'El bridge corre sin ventana. Log en vivo:  Get-Content C:\PolyConecta\bridge\logs\bridge-*.log -Wait -Tail 50'
Write-Host 'Detenerlo:                              Stop-Process -Name Contpaq.Bridge'
Write-Host 'Siguiente: Set-BridgeAutostart.ps1 (L1-T016), para que arranque solo al reiniciar.'
