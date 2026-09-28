<#
 Restaura el laboratorio a la línea base. DESTRUCTIVO para la base de laboratorio (y solo para ella).
 Cierra CONTPAQi antes. El agente NO debe correrlo sin autorización explícita del usuario.
#>
param([switch]$Yes)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\_sql.ps1"
$e = Get-Content "$PSScriptRoot\lab.env.json" -Raw | ConvertFrom-Json
if ($e.LabDatabase -notmatch '_LAB$') { throw "Solo se restaura una base *_LAB." }
if (-not $Yes) { if ((Read-Host "Restaurar $($e.LabDatabase) a la línea base? (escribe SI)") -ne 'SI') { return } }
$sa = Get-Credential -UserName 'sa' -Message 'Credencial SQL (no se guarda)'
$bak = Join-Path $e.BackupDir "$($e.LabDatabase).baseline.bak"
if (-not (Test-Path $bak)) { throw "No existe la línea base $bak. Corre New-Baseline.ps1." }
Invoke-Sa -Server $e.SqlServer -Database 'master' -Cred $sa -Sql @"
ALTER DATABASE [$($e.LabDatabase)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$($e.LabDatabase)] FROM DISK = N'$bak' WITH REPLACE, RECOVERY;
ALTER DATABASE [$($e.LabDatabase)] SET MULTI_USER;
"@
Write-Host "Laboratorio restaurado. Verifica: .\sdklab.exe guard"
