# ==============================================================================
# PolyConecta - Compila, prueba y levanta la solución (equivalente de scripts/run.sh para PowerShell)
# ==============================================================================
# Uso (Windows PowerShell 5.1 o PowerShell 7):
#   .\run.ps1                  # Inicia PolyConecta.Web (9000) + API (9020)
#   .\run.ps1 --with-bridge    # Además levanta el bridge de CONTPAQi (9030), en modo simulado
#   .\run.ps1 --solo-web       # Solo PolyConecta.Web (9000): sin .NET, SQL Server, pruebas ni API
#
# Si la política de ejecución lo impide: powershell -ExecutionPolicy Bypass -File .\run.ps1
#
# El bridge arranca simulado igual que con run.sh fuera de Windows (D-122). Para usar el SDK real de
# CONTPAQi, define BridgeConfig__Mode=Real antes de correrlo.
# ==============================================================================

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

$EsWindows = $env:OS -eq 'Windows_NT'
$WebPort = '9000'
$ApiPort = '9020'
$BridgePort = '9030'
$WithBridge = $false
$SoloWeb = $false

foreach ($arg in $args) {
    switch -Regex ($arg) {
        '^--?(with-bridge|bridge)$' { $WithBridge = $true }
        '^--?(solo-web|web)$' { $SoloWeb = $true }
        '^--?(with-angular|angular)$' { } # PolyConecta.Web ya se levanta siempre; se acepta para no romper a quien lo usaba.
        '^[0-9]+$' { $ApiPort = $arg }
    }
}

# Corre un comando nativo y detiene el script si falla (como set -e en run.sh).
function Invoke-Paso {
    param([string]$Exe, [string[]]$Argumentos)
    & $Exe @Argumentos
    if ($LASTEXITCODE -ne 0) { throw "Falló: $Exe $($Argumentos -join ' ') (código $LASTEXITCODE)" }
}

# Corre un comando nativo sin salida y dice si terminó bien.
function Test-Comando {
    param([string]$Exe, [string[]]$Argumentos)
    $ErrorActionPreference = 'Continue'
    try { & $Exe @Argumentos *> $null; return $LASTEXITCODE -eq 0 } catch { return $false }
}

function New-Secreto {
    param([int]$Largo)
    $letras = [char[]]'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'
    $bytes = New-Object byte[] $Largo
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $rng.Dispose()
    return -join ($bytes | ForEach-Object { $letras[$_ % $letras.Length] })
}

# .env.local se comparte con run.sh: UTF-8 sin BOM y fin de línea LF, para que bash lo pueda leer.
$EnvLocal = Join-Path $RepoRoot '.env.local'
$Utf8SinBom = New-Object System.Text.UTF8Encoding $false

function Read-EnvLocal {
    $valores = @{}
    if (Test-Path $EnvLocal) {
        foreach ($linea in [System.IO.File]::ReadAllLines($EnvLocal)) {
            if ($linea -match '^\s*([A-Za-z_][A-Za-z0-9_]*)=(.*)$') { $valores[$Matches[1]] = $Matches[2].TrimEnd("`r") }
        }
    }
    return $valores
}

function Add-EnvLocal {
    param([string]$Linea)
    [System.IO.File]::AppendAllText($EnvLocal, "$Linea`n", $Utf8SinBom)
}

Write-Host '================================================================='
Write-Host 'PolyConecta Operational Suite - Build, Test & Run'
Write-Host '================================================================='
Write-Host "Repository Root: $RepoRoot"
Write-Host "PolyConecta.Web (Angular)            : $WebPort"
if ($SoloWeb) {
    Write-Host 'Modo                                : solo la capa web (--solo-web)'
} else {
    Write-Host "Swagger & REST API (PolyConecta.Api) : $ApiPort"
    if ($WithBridge) {
        Write-Host "CONTPAQi Bridge Port                : $BridgePort (Activo)"
    } else {
        Write-Host 'CONTPAQi Bridge Mode                : Desactivado (Usa --with-bridge para arrancar)'
    }
}
Write-Host '================================================================='

# Step 0: PolyConecta.Web necesita la versión de Node fijada en .nvmrc (spec 001, L2-T067).
Write-Host 'Step 0: Checking Node for PolyConecta.Web...'
$NodeRequerido = (Get-Content (Join-Path $RepoRoot 'PolyConecta.Web/.nvmrc') -Raw).Trim().TrimStart('v')
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "Error: PolyConecta.Web requiere Node $NodeRequerido (PolyConecta.Web/.nvmrc) y no hay 'node' en PATH."
    exit 1
}
$NodeActual = (& node --version).Trim().TrimStart('v')
if ($NodeActual -ne $NodeRequerido) {
    Write-Host "Error: PolyConecta.Web requiere Node $NodeRequerido (PolyConecta.Web/.nvmrc) y está $NodeActual. Usa 'nvm use $NodeRequerido'."
    exit 1
}
$Npm = if ($EsWindows) { 'npm.cmd' } else { 'npm' }
$WebDir = Join-Path $RepoRoot 'PolyConecta.Web'
if (-not (Test-Path (Join-Path $WebDir 'node_modules'))) {
    Write-Host '   Instalando dependencias de PolyConecta.Web (npm ci)...'
    Push-Location $WebDir
    try { Invoke-Paso $Npm @('ci') } finally { Pop-Location }
}
Write-Host "   Node $NodeActual"

# npm se arranca con Start-Process: así el "--" de "npm start -- --port" llega intacto en cualquier versión.
$NpmStart = @('start', '--', '--port', $WebPort)

# --solo-web: la réplica no necesita la API (el chatter queda "Sin conexión en vivo" y agrega en local).
if ($SoloWeb) {
    Write-Host '================================================================='
    Write-Host "PolyConecta.Web (Angular) : http://localhost:$WebPort"
    Write-Host 'Press Ctrl+C to stop the server.'
    Write-Host '================================================================='
    $web = Start-Process -FilePath $Npm -ArgumentList $NpmStart -WorkingDirectory $WebDir -NoNewWindow -PassThru
    $web.WaitForExit()
    exit $web.ExitCode
}

# Step 1: Locate a .NET installation that can build and run the solution.
Write-Host 'Step 1: Checking .NET SDK environment...'

# La solución corre en .NET 10 (CT-04). El prototipo Blazor sigue en net8.0 (D-60), pero el script ya
# no lo levanta: el runtime 8 solo hace falta para las pruebas de paridad de PolyConecta.Web.
function Test-Runtimes {
    param([string]$Candidato)
    $ErrorActionPreference = 'Continue'
    try { $salida = & $Candidato --list-runtimes 2>$null } catch { return $false }
    return @($salida | Where-Object { $_ -match '^Microsoft\.AspNetCore\.App 10\.' }).Count -gt 0
}

$candidatos = @()
$enPath = Get-Command dotnet -ErrorAction SilentlyContinue
if ($enPath) { $candidatos += $enPath.Source }
if ($EsWindows) {
    $candidatos += (Join-Path $env:ProgramFiles 'dotnet\dotnet.exe')
    $candidatos += (Join-Path $HOME '.dotnet\dotnet.exe')
} else {
    $candidatos += '/usr/local/share/dotnet/dotnet', '/usr/share/dotnet/dotnet', (Join-Path $HOME '.dotnet/dotnet')
}

$Dotnet = $null
foreach ($c in $candidatos) {
    if ($c -and (Test-Path $c) -and (Test-Runtimes $c)) { $Dotnet = $c; break }
}

if (-not $Dotnet) {
    Write-Host 'Error: No se encontró una instalación de .NET con el runtime de ASP.NET Core 10.x.'
    if ($enPath) {
        Write-Host "   'dotnet' en PATH: $($enPath.Source)"
        Write-Host '   Runtimes disponibles ahí:'
        & $enPath.Source --list-runtimes 2>$null | ForEach-Object { Write-Host "     $_" }
    }
    Write-Host '   Instala el SDK 10 (global.json).'
    exit 1
}

# Antepone la instalación elegida al PATH para que los procesos hijos
# (testhost, dotnet run) usen esta misma instalación de forma consistente.
$env:DOTNET_ROOT = Split-Path -Parent $Dotnet
$env:PATH = "$($env:DOTNET_ROOT)$([System.IO.Path]::PathSeparator)$($env:PATH)"

Write-Host "   Using .NET installation: $($env:DOTNET_ROOT)"
Write-Host "   Found .NET SDK version: $(& $Dotnet --version)"

# Step 1b: SQL Server de PolyConecta (CT-05). Si no hay ConnectionStrings__PolyConecta en el
# entorno, se levanta (o reutiliza) un SQL Server 2022 local en Docker con los logins de CT-30.
# Las contraseñas locales se generan la primera vez en .env.local, que git ignora (CT-29).
$SqlContainer = 'polyconecta-sql'
$SqlPort = '14333'
if (-not $env:ConnectionStrings__PolyConecta) {
    Write-Host "Step 1b: SQL Server local en Docker ($SqlContainer, puerto $SqlPort)..."
    if (-not (Test-Comando 'docker' @('info'))) {
        Write-Host 'Error: Docker no está en marcha y no hay ConnectionStrings__PolyConecta en el entorno.'
        exit 1
    }
    if (-not (Test-Path $EnvLocal)) {
        [System.IO.File]::WriteAllText($EnvLocal, '', $Utf8SinBom)
        Add-EnvLocal "LOCAL_SA_PASSWORD=Sa_$(New-Secreto 20)1!"
        Add-EnvLocal "LOCAL_APP_PASSWORD=App_$(New-Secreto 20)1!"
        Add-EnvLocal "LOCAL_MIGRACIONES_PASSWORD=Mig_$(New-Secreto 20)1!"
    }
    $local = Read-EnvLocal
    # Contraseña del Administrador inicial que siembra la API (L2-T003, quickstart de F1).
    if (-not $local.ContainsKey('LOCAL_ADMIN_PASSWORD')) { Add-EnvLocal "LOCAL_ADMIN_PASSWORD=Adm_$(New-Secreto 20)1!" }
    # Contraseña de los usuarios de ejemplo de R1 (DatosR1, solo en desarrollo; L2-T033).
    if (-not $local.ContainsKey('LOCAL_R1_PASSWORD')) { Add-EnvLocal "LOCAL_R1_PASSWORD=R1_$(New-Secreto 16)1!" }
    $local = Read-EnvLocal
    $env:Seguridad__AdministradorInicial__Contrasena = $local['LOCAL_ADMIN_PASSWORD']
    $env:Seguridad__DatosR1__Contrasena = $local['LOCAL_R1_PASSWORD']

    $enMarcha = @(& docker ps --format '{{.Names}}')
    if ($enMarcha -notcontains $SqlContainer) {
        $todos = @(& docker ps -a --format '{{.Names}}')
        if ($todos -contains $SqlContainer) {
            Invoke-Paso 'docker' @('start', $SqlContainer) | Out-Null
        } else {
            Invoke-Paso 'docker' @('run', '-d', '--name', $SqlContainer, '-e', 'ACCEPT_EULA=Y', '-e', "MSSQL_SA_PASSWORD=$($local['LOCAL_SA_PASSWORD'])",
                '-p', "${SqlPort}:1433", 'mcr.microsoft.com/mssql/server:2022-latest') | Out-Null
        }
    }
    $sqlcmd = @('exec', $SqlContainer, '/opt/mssql-tools18/bin/sqlcmd', '-C', '-S', 'localhost', '-U', 'sa', '-P', $local['LOCAL_SA_PASSWORD'])
    for ($i = 0; $i -lt 60; $i++) {
        if (Test-Comando 'docker' ($sqlcmd + @('-Q', 'SELECT 1'))) { break }
        Start-Sleep -Seconds 2
    }
    Invoke-Paso 'docker' @('cp', 'scripts/sql/logins-desarrollo.sql', "${SqlContainer}:/tmp/logins-desarrollo.sql")
    Invoke-Paso 'docker' ($sqlcmd + @('-b', '-i', '/tmp/logins-desarrollo.sql', '-v', 'Base=PolyConecta',
        "AppPassword=$($local['LOCAL_APP_PASSWORD'])", "MigracionesPassword=$($local['LOCAL_MIGRACIONES_PASSWORD'])")) | Out-Null
    $env:ConnectionStrings__PolyConecta = "Server=localhost,$SqlPort;Database=PolyConecta;User Id=polyconecta_app;Password=$($local['LOCAL_APP_PASSWORD']);TrustServerCertificate=true"
    $env:ConnectionStrings__PolyConectaMigraciones = "Server=localhost,$SqlPort;Database=PolyConecta;User Id=polyconecta_migraciones;Password=$($local['LOCAL_MIGRACIONES_PASSWORD']);TrustServerCertificate=true"
}

# Step 2: Build complete solution
Write-Host 'Step 2: Building full solution (Polyconecta.slnx)...'
Invoke-Paso $Dotnet @('build', 'Polyconecta.slnx', '-c', 'Debug')

# Step 2b: Aplicar migraciones con el login de migraciones (CT-06, CT-30)
if ($env:ConnectionStrings__PolyConectaMigraciones) {
    Write-Host 'Step 2b: Applying EF Core migrations...'
    Invoke-Paso $Dotnet @('tool', 'restore') | Out-Null
    Invoke-Paso $Dotnet @('ef', 'database', 'update', '--project', 'PolyConecta.Infrastructure', '--startup-project', 'PolyConecta.Api', '--no-build')
}

# Step 3: Run test suites
Write-Host 'Step 3: Running domain unit & integration test suites...'
# Solo se detallan las pruebas que fallan: las de contrato se omiten sin BRIDGE_URL y no se listan.
Invoke-Paso $Dotnet @('test', '--solution', 'Polyconecta.slnx', '--no-build', '--show-test-results', 'failed')

Write-Host '================================================================='
Write-Host 'Build & Tests Succeeded! Launching PolyConecta Solution Layers...'
Write-Host '================================================================='
Write-Host "PolyConecta.Web (Angular)      : http://localhost:$WebPort"
Write-Host "Swagger API (PolyConecta.Api)  : http://localhost:$ApiPort/swagger"
Write-Host "REST API Endpoints Base        : http://localhost:$ApiPort/api/v1"

$Procesos = @()

# Al salir (Ctrl+C incluido) se apagan los procesos en segundo plano con todos sus hijos: npm start deja
# a ng serve como proceso hijo.
function Stop-Procesos {
    Write-Host ''
    Write-Host 'Shutting down PolyConecta processes...'
    foreach ($p in $Procesos) {
        try {
            if ($p.HasExited) { continue }
            if ($EsWindows) { & taskkill /PID $p.Id /T /F *> $null } else { $p.Kill($true) }
        } catch { }
    }
}

try {
    if ($WithBridge) {
        # Simulado como run.sh fuera de Windows (D-122). La API le envía su outbox y recibe los callbacks
        # firmados con un secreto local de .env.local (CT-29).
        if ((Test-Path $EnvLocal) -and -not (Read-EnvLocal).ContainsKey('LOCAL_CALLBACK_SECRET')) {
            Add-EnvLocal "LOCAL_CALLBACK_SECRET=$(New-Secreto 32)"
        }
        $secreto = (Read-EnvLocal)['LOCAL_CALLBACK_SECRET']
        if (-not $secreto) { $secreto = 'secreto-local' }
        if (-not $env:BridgeConfig__Mode) { $env:BridgeConfig__Mode = 'Simulated' }
        $env:BridgeConfig__CallbackSecret = $secreto
        $env:BridgeConfig__DashboardPort = $BridgePort
        $env:Erp__BridgeUrl = "http://localhost:$BridgePort"
        $env:Erp__CallbackBaseUrl = "http://localhost:$ApiPort"
        $env:Erp__CallbackSecret = $secreto
        Write-Host "CONTPAQi Bridge Worker         : http://localhost:$BridgePort ($($env:BridgeConfig__Mode))"
        Write-Host '-----------------------------------------------------------------'
        Write-Host "Iniciando servicio CONTPAQi Bridge en segundo plano (Puerto $BridgePort)..."
        $Procesos += Start-Process -FilePath $Dotnet -ArgumentList @('run', '--project', 'PolyConecta.Contpaq/PolyConecta.Contpaq.csproj', '--no-build') `
            -WorkingDirectory $RepoRoot -NoNewWindow -PassThru
    }

    Write-Host '-----------------------------------------------------------------'
    Write-Host "Iniciando PolyConecta.Web en segundo plano (Puerto $WebPort)..."
    $Procesos += Start-Process -FilePath $Npm -ArgumentList $NpmStart -WorkingDirectory $WebDir -NoNewWindow -PassThru

    Write-Host '================================================================='
    Write-Host 'Press Ctrl+C to stop the servers.'
    Write-Host '================================================================='

    # Step 4: Run PolyConecta.Api server in foreground
    & $Dotnet run --project PolyConecta.Api/PolyConecta.Api.csproj --no-build --urls "http://localhost:$ApiPort"
} finally {
    Stop-Procesos
}
