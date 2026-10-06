# Funciones compartidas por los scripts de scripts\vps (D-130). Se cargan con: . "$PSScriptRoot\_comun.ps1"

$script:Repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$script:CarpetaBridge = 'C:\PolyConecta\bridge'
$script:Evidencia = Join-Path $script:Repo 'tools\sdk-lab\evidence\F0'

function Ok([string]$m)    { Write-Host "  [ok]    $m" -ForegroundColor Green }
function Aviso([string]$m) { Write-Host "  [aviso] $m" -ForegroundColor Yellow }
function Falla([string]$m) { Write-Host "  [falta] $m" -ForegroundColor Red }
function Paso([string]$m)  { Write-Host ''; Write-Host $m -ForegroundColor Cyan }

function Test-Administrador {
    $p = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Llama al bridge y devuelve el objeto, o $null si no responde.
function Invoke-Bridge([string]$ruta, [int]$Puerto = 5005) {
    try { return Invoke-RestMethod -Uri ("http://localhost:{0}{1}" -f $Puerto, $ruta) -TimeoutSec 15 }
    catch { return $null }
}

# Agrega una sección al archivo de evidencia (carpeta ignorada por git: es local del VPS).
function Add-Evidencia([string]$archivo, [string]$titulo, [string[]]$lineas) {
    New-Item -ItemType Directory -Force $script:Evidencia | Out-Null
    $ruta = Join-Path $script:Evidencia $archivo
    $texto = @('', ("## {0} · {1}" -f $titulo, (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')), '', '```text') + $lineas + @('```')
    Add-Content -Path $ruta -Value $texto -Encoding UTF8
    Write-Host ("  Evidencia agregada a {0}" -f $ruta)
}
