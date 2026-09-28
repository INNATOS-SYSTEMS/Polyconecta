<# Toma la línea base del laboratorio (respaldo .bak con la marca POLYCONECTA_LAB ya puesta). Hacerlo ANTES de cualquier prueba que escriba. #>
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\_sql.ps1"
$e = Get-Content "$PSScriptRoot\lab.env.json" -Raw | ConvertFrom-Json
$sa = Get-Credential -UserName 'sa' -Message 'Credencial SQL (no se guarda)'
$bak = Join-Path $e.BackupDir "$($e.LabDatabase).baseline.bak"
Invoke-Sa -Server $e.SqlServer -Database 'master' -Cred $sa -Sql "BACKUP DATABASE [$($e.LabDatabase)] TO DISK = N'$bak' WITH INIT, COMPRESSION, CHECKSUM"
Write-Host "Línea base: $bak"
