<#
 Instala la estación de laboratorio en el VPS. Ejecutar UNA vez, como administrador, desde la carpeta descomprimida.
 Requisitos previos (MANUALES): la empresa de laboratorio ya existe y está registrada en CONTPAQi
 (copia de respaldo de producción restaurada como <BD>_LAB con la herramienta de CONTPAQi para crear/restaurar empresas).
 Este script NO toca la empresa viva: se niega si -LabDatabase coincide con ella.
#>
param(
    [Parameter(Mandatory)][string]$SqlServer,          # p. ej. localhost\COMPAC01
    [Parameter(Mandatory)][string]$LabDatabase,        # p. ej. adPOLYEMPAQUES_LAB
    [Parameter(Mandatory)][string]$LiveDatabase,       # p. ej. adPOLYEMPAQUES  (solo para negarse a tocarla)
    [string]$BackupDir = 'C:\sdklab\backups'
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\_sql.ps1"
if ($LabDatabase -ieq $LiveDatabase) { throw "LabDatabase no puede ser la base viva." }
if ($LabDatabase -notmatch '_LAB$') { throw "Por convención la base de laboratorio debe terminar en _LAB." }

$root = Split-Path $PSScriptRoot -Parent
$sa = Get-Credential -UserName 'sa' -Message 'Credencial con permisos de administración en SQL Server (no se guarda)'
$roPwd = -join ((48..57 + 65..90 + 97..122) | Get-Random -Count 24 | ForEach-Object { [char]$_ })

# 1. Login de solo lectura restringido a la base de laboratorio + marca de laboratorio
Invoke-Sa -Server $SqlServer -Database 'master' -Cred $sa -Sql @"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'sdklab_ro') CREATE LOGIN sdklab_ro WITH PASSWORD = N'$roPwd', CHECK_POLICY = OFF;
ELSE ALTER LOGIN sdklab_ro WITH PASSWORD = N'$roPwd';
"@
Invoke-Sa -Server $SqlServer -Database $LabDatabase -Cred $sa -Sql @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'sdklab_ro') CREATE USER sdklab_ro FOR LOGIN sdklab_ro;
ALTER ROLE db_datareader ADD MEMBER sdklab_ro;
IF EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 0 AND name = 'POLYCONECTA_LAB') EXEC sp_updateextendedproperty @name = N'POLYCONECTA_LAB', @value = N'1';
ELSE EXEC sp_addextendedproperty @name = N'POLYCONECTA_LAB', @value = N'1';
"@

# 2. Contraseña del login RO: solo como variable de entorno de la máquina, nunca en archivo ni en el repo
try { [Environment]::SetEnvironmentVariable('SDKLAB_SQL_PASSWORD', $roPwd, 'Machine'); $scope = 'Machine' }
catch {
    # Sin consola elevada no se puede escribir a nivel máquina: se guarda a nivel de usuario.
    [Environment]::SetEnvironmentVariable('SDKLAB_SQL_PASSWORD', $roPwd, 'User'); $scope = 'User'
    Write-Warning "Consola sin privilegios de administrador: la variable quedó a nivel USUARIO ($env:USERNAME). Solo este usuario (y sus sesiones SSH) la verá."
}
$env:SDKLAB_SQL_PASSWORD = $roPwd   # disponible ya en esta misma sesión

# 3. Configuración
$cfgPath = Join-Path $root 'lab.config.json'
if (-not (Test-Path $cfgPath)) {
    $cfg = Get-Content (Join-Path $root 'lab.config.example.json') -Raw
    $cfg = $cfg -replace 'adPOLYEMPAQUES_LAB', $LabDatabase -replace 'localhost\\\\COMPAC01', ($SqlServer -replace '\\', '\\')
    Set-Content $cfgPath $cfg -Encoding UTF8
}
New-Item -ItemType Directory -Force $BackupDir | Out-Null
@{ SqlServer = $SqlServer; LabDatabase = $LabDatabase; BackupDir = $BackupDir } | ConvertTo-Json | Set-Content (Join-Path $root 'scripts\lab.env.json')

Write-Host "Listo. Abre una NUEVA sesión (para leer SDKLAB_SQL_PASSWORD) y corre:  .\sdklab.exe env"
Write-Host "Siguiente paso: .\scripts\New-Baseline.ps1  (fotografía limpia del laboratorio)."
