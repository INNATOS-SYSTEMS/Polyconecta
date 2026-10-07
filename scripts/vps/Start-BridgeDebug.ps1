<#
.SYNOPSIS
  Arranca el bridge (PolyConecta.Contpaq) en modo debug en el VPS, compilado en x86 (D-130).

.DESCRIPTION
  Compila en Debug con -p:Bridge32=true (MGW_SDK.dll es de 32 bits) y lo corre en esta consola,
  con ASPNETCORE_ENVIRONMENT=Development. Los logs salen en la consola y en la carpeta logs del
  bridge. Ctrl+C lo detiene.

  Modo Real (por omisión): abre la empresa de CONTPAQi de BridgeConfig__CompanyPath y escribe en ella.
  Modo -Simulado: no carga el SDK; sirve para la lógica del contrato que no toca CONTPAQi.

  Para depurar paso a paso: Visual Studio 2022 o posterior > Depurar > Asociar al proceso >
  Contpaq.Bridge.exe (proceso x86).

.EXAMPLE
  .\scripts\vps\Start-BridgeDebug.ps1
  .\scripts\vps\Start-BridgeDebug.ps1 -Simulado
#>
[CmdletBinding()]
param(
    [switch]$Simulado,
    [int]$Puerto = 5005
)

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')

if ($env:SSH_CONNECTION -or $env:SSH_CLIENT) {
    if (-not $Simulado) {
        Write-Host 'Esta consola viene de SSH: el SDK de CONTPAQi no abre la empresa fuera de una sesión iniciada (S-02). Abre Escritorio remoto, o usa -Simulado.' -ForegroundColor Red
        exit 1
    }
}

# Las variables de usuario que dejó Initialize-BridgeDebug.ps1 se leen aunque esta consola sea anterior.
foreach ($k in [Environment]::GetEnvironmentVariables('User').Keys) {
    if ($k -like 'BridgeConfig__*' -and -not [Environment]::GetEnvironmentVariable($k, 'Process')) {
        [Environment]::SetEnvironmentVariable($k, [Environment]::GetEnvironmentVariable($k, 'User'), 'Process')
    }
}

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:BridgeConfig__DashboardPort = "$Puerto"
if ($Simulado) { $env:BridgeConfig__Mode = 'Simulated' } else { $env:BridgeConfig__Mode = 'Real' }

Write-Host '================================================================='
Write-Host (" Bridge en modo {0} · debug · x86 · http://localhost:{1}" -f $env:BridgeConfig__Mode, $Puerto)
if (-not $Simulado) {
    Write-Host (" Empresa: {0}" -f $(if ($env:BridgeConfig__CompanyPath) { $env:BridgeConfig__CompanyPath } else { 'la de appsettings.json (adPOLYEMPAQUES)' })) -ForegroundColor Yellow
    Write-Host ' Escribe en CONTPAQi real: confirma cada comando que escriba antes de mandarlo (D-130).' -ForegroundColor Yellow
}
Write-Host '   Salud:     GET  /health'
Write-Host '   Comandos:  POST /api/v1/transactions'
Write-Host '   Lecturas:  GET  /api/v1/catalogs/warehouses'
Write-Host '================================================================='

Push-Location $repo
try {
    & dotnet run --project PolyConecta.Contpaq -c Debug -p:Bridge32=true
} finally {
    Pop-Location
}
