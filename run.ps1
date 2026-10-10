# Delega en scripts/run.ps1 (equivalente de run.sh para PowerShell).
& (Join-Path $PSScriptRoot 'scripts/run.ps1') @args
exit $LASTEXITCODE
