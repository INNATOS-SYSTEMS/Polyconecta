<#
.SYNOPSIS
  Prepara el VPS para correr y depurar el bridge (PolyConecta.Contpaq) en modo debug, contra
  CONTPAQi real (D-130). Se corre una vez, y otra vez si algo cambia; no instala nada por su cuenta.

.DESCRIPTION
  Comprueba, en orden, lo que el bridge necesita para abrir el SDK de CONTPAQi:
    1. Sesión interactiva de Windows (no SSH ni servicio): el SDK solo funciona ahí (S-02, D-115).
    2. SDK de .NET 10 (x64) para compilar y runtimes de .NET 10 y ASP.NET Core 10 x86 para correr:
       MGW_SDK.dll es de 32 bits.
    3. El SDK de CONTPAQi (MGW_SDK.dll y MGWServicios.dll) y la carpeta de la empresa.
    4. Variables de entorno de usuario del administrador:
       - BridgeConfig__SqlConnectionString (login de solo lectura, L1-T002): solo se comprueba.
       - BridgeConfig__CallbackSecret: si falta, se genera una al azar y se guarda.
       - BridgeConfig__Conceptos__* y BridgeConfig__Monedas__*: si faltan, el bridge usa los códigos de
         ejemplo de appsettings.json, que NO son los de CONTPAQi (D-121).
    5. El puerto del bridge libre.

  Nunca imprime secretos: de las variables solo dice si existen.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\scripts\vps\Initialize-BridgeDebug.ps1
#>
[CmdletBinding()]
param(
    [int]$Puerto = 5005
)

$ErrorActionPreference = 'Stop'
$script:fallas = 0
$script:avisos = 0

function Ok([string]$m)    { Write-Host "  [ok]    $m" -ForegroundColor Green }
function Aviso([string]$m) { Write-Host "  [aviso] $m" -ForegroundColor Yellow; $script:avisos++ }
function Falla([string]$m) { Write-Host "  [falta] $m" -ForegroundColor Red; $script:fallas++ }
function Seccion([string]$m) { Write-Host ''; Write-Host $m -ForegroundColor Cyan }

function Get-UserVar([string]$nombre) { [Environment]::GetEnvironmentVariable($nombre, 'User') }

# Valor efectivo de una clave de BridgeConfig: variable de entorno (proceso o usuario) o appsettings.json.
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$appsettings = Get-Content (Join-Path $repo 'PolyConecta.Contpaq\appsettings.json') -Raw | ConvertFrom-Json
function Get-BridgeValue([string]$clave) {
    $var = 'BridgeConfig__' + $clave
    $v = [Environment]::GetEnvironmentVariable($var, 'Process')
    if (-not $v) { $v = Get-UserVar $var }
    if (-not $v) { $v = $appsettings.BridgeConfig.$clave }
    return $v
}

Write-Host '================================================================='
Write-Host ' Bridge en modo debug en el VPS · comprobación del entorno'
Write-Host '================================================================='

# ------------------------------------------------------------------ 1. sesión
Seccion '1. Sesión de Windows'
if ($env:SSH_CONNECTION -or $env:SSH_CLIENT) {
    Falla 'Esta consola viene de SSH. El SDK de CONTPAQi solo abre la empresa en una sesión iniciada: abre Escritorio remoto y corre aquí (S-02).'
} elseif ((Get-Process -Id $PID).SessionId -eq 0) {
    Falla 'Esta consola corre en la sesión 0 (servicios). El SDK necesita la sesión interactiva del administrador (D-115).'
} else {
    Ok ("Sesión interactiva {0} de {1}" -f (Get-Process -Id $PID).SessionId, $env:USERNAME)
}

# ------------------------------------------------------------------ 2. .NET
Seccion '2. .NET 10'
$sdks = & dotnet --list-sdks 2>$null
if ($sdks -match '^10\.') { Ok ('SDK de .NET 10: ' + (($sdks -match '^10\.') | Select-Object -Last 1)) }
else { Falla 'No está el SDK de .NET 10 (x64). Instálalo desde https://dotnet.microsoft.com/download/dotnet/10.0' }

$dotnet86 = Join-Path ${env:ProgramFiles(x86)} 'dotnet\dotnet.exe'
if (Test-Path $dotnet86) {
    $rt86 = & $dotnet86 --list-runtimes 2>$null
    if ($rt86 -match '^Microsoft\.NETCore\.App 10\.') { Ok '.NET Runtime 10 x86' } else { Falla 'Falta el .NET Runtime 10 x86.' }
    if ($rt86 -match '^Microsoft\.AspNetCore\.App 10\.') { Ok 'ASP.NET Core Runtime 10 x86' } else { Falla 'Falta el ASP.NET Core Runtime 10 x86.' }
} else {
    Falla ("No hay .NET x86 en {0}. Instala el SDK de .NET 10 x86 (trae los dos runtimes): https://dotnet.microsoft.com/download/dotnet/10.0" -f $dotnet86)
}

# ------------------------------------------------------------------ 3. CONTPAQi
Seccion '3. CONTPAQi'
$sdkPath = Get-BridgeValue 'SdkPath'
foreach ($dll in 'MGW_SDK.dll', 'MGWServicios.dll') {
    if (Test-Path (Join-Path $sdkPath $dll)) { Ok "$dll en $sdkPath" } else { Falla "No está $dll en $sdkPath (BridgeConfig__SdkPath)." }
}
$empresa = Get-BridgeValue 'CompanyPath'
if (Test-Path $empresa) { Ok "Empresa: $empresa" } else { Falla "No existe la carpeta de la empresa: $empresa (BridgeConfig__CompanyPath)." }
Aviso "En modo Real el bridge escribe en esa empresa de CONTPAQi, sin empresa de laboratorio (D-130). Confirma cada escritura antes de mandarla."

# ------------------------------------------------------------------ 4. variables de entorno
Seccion '4. Variables de entorno del usuario'
if (Get-UserVar 'BridgeConfig__SqlConnectionString') { Ok 'BridgeConfig__SqlConnectionString definida (login de solo lectura, L1-T002).' }
else { Falla 'Falta BridgeConfig__SqlConnectionString. Defínela con el login polyconecta_bridge_ro (L1-T002); sin ella el bridge no arranca.' }

if (Get-UserVar 'BridgeConfig__CallbackSecret') {
    Ok 'BridgeConfig__CallbackSecret definida.'
} else {
    $bytes = New-Object byte[] 24
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $secreto = [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', 'A').Replace('/', 'B')
    [Environment]::SetEnvironmentVariable('BridgeConfig__CallbackSecret', $secreto, 'User')
    Remove-Variable secreto, bytes
    Ok 'BridgeConfig__CallbackSecret generada y guardada. La API (Erp__CallbackSecret) y la suite de contrato (BRIDGE_CALLBACK_SECRET) deben usar la misma.'
}

$usuario = [Environment]::GetEnvironmentVariables('User').Keys
$conceptos = @($usuario | Where-Object { $_ -like 'BridgeConfig__Conceptos__*' })
$monedas = @($usuario | Where-Object { $_ -like 'BridgeConfig__Monedas__*' })
if ($conceptos.Count -gt 0) { Ok ("{0} conceptos de CONTPAQi configurados." -f $conceptos.Count) }
else { Aviso 'No hay BridgeConfig__Conceptos__*: el bridge usaría los códigos de ejemplo de appsettings.json, que no existen en CONTPAQi (D-121). Defínelos antes de mandar comandos que escriban.' }
if ($monedas.Count -gt 0) { Ok ("{0} monedas configuradas." -f $monedas.Count) }
else { Aviso 'No hay BridgeConfig__Monedas__*: el bridge usaría MXN=1 y USD=2 de ejemplo. Confírmalos contra admMonedas.' }

# ------------------------------------------------------------------ 5. puerto
Seccion '5. Puerto'
$ocupado = Get-NetTCPConnection -LocalPort $Puerto -State Listen -ErrorAction SilentlyContinue
if ($ocupado) { Falla ("El puerto {0} ya está en uso (proceso {1})." -f $Puerto, $ocupado[0].OwningProcess) } else { Ok "Puerto $Puerto libre." }

# ------------------------------------------------------------------ resumen
Write-Host ''
Write-Host '================================================================='
if ($script:fallas -gt 0) {
    Write-Host (" {0} cosa(s) por resolver y {1} aviso(s). Corrige lo marcado [falta] y vuelve a correr." -f $script:fallas, $script:avisos) -ForegroundColor Red
    Write-Host ' Las variables de usuario nuevas las ve una consola abierta después de crearlas.'
    exit 1
}
Write-Host (" Listo, con {0} aviso(s). Abre una consola nueva y arranca el bridge:" -f $script:avisos) -ForegroundColor Green
Write-Host '   .\scripts\vps\Start-BridgeDebug.ps1            (modo Real, contra CONTPAQi)'
Write-Host '   .\scripts\vps\Start-BridgeDebug.ps1 -Simulado  (sin SDK, para lógica que no toca CONTPAQi)'
Write-Host '================================================================='
