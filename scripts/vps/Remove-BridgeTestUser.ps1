<#
.SYNOPSIS
  Quita el usuario de prueba polyconecta-bridge de S-04 y su tarea (L1-T017, A-19).

.DESCRIPTION
  Va DESPUÉS de Set-BridgeAutostart.ps1: si el inicio de sesión automático todavía apunta a este
  usuario, el script se detiene, porque al borrarlo el siguiente reinicio se quedaría esperando en la
  pantalla de inicio de sesión.

  Quita, en este orden: las tareas programadas que corren como ese usuario (la de S-04 se llamaba
  PolyConecta-bridge-logon), su perfil y el usuario local. Deja la evidencia en
  tools\sdk-lab\evidence\F0\0.9.md.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\scripts\vps\Remove-BridgeTestUser.ps1
#>
[CmdletBinding()]
param(
    [string]$Usuario = 'polyconecta-bridge'
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\_comun.ps1"

Write-Host '================================================================='
Write-Host (' Quitar el usuario de prueba {0} (L1-T017)' -f $Usuario)
Write-Host '================================================================='

if (-not (Test-Administrador)) { Falla 'Abre PowerShell como administrador.'; exit 1 }

$wl = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
if ($wl.AutoAdminLogon -eq '1' -and $wl.DefaultUserName -eq $Usuario) {
    Falla "El inicio de sesión automático apunta a $Usuario. Corre antes Set-BridgeAutostart.ps1 (L1-T016)."; exit 1
}
Ok ("El inicio de sesión automático apunta a {0}, no a {1}." -f $wl.DefaultUserName, $Usuario)

$lineas = @()
$tareas = @(Get-ScheduledTask -ErrorAction SilentlyContinue | Where-Object { $_.Principal.UserId -like "*$Usuario" -or $_.TaskName -eq 'PolyConecta-bridge-logon' })
foreach ($t in $tareas) {
    Unregister-ScheduledTask -TaskName $t.TaskName -TaskPath $t.TaskPath -Confirm:$false
    Ok ("Tarea quitada: {0}{1}" -f $t.TaskPath, $t.TaskName)
    $lineas += ("Tarea quitada: {0}{1}" -f $t.TaskPath, $t.TaskName)
}
if ($tareas.Count -eq 0) { Ok 'No había tareas de ese usuario.'; $lineas += 'Sin tareas del usuario' }

if (Get-LocalUser -Name $Usuario -ErrorAction SilentlyContinue) {
    $perfil = Get-CimInstance Win32_UserProfile | Where-Object { $_.LocalPath -like "*\$Usuario" }
    if ($perfil) {
        if ($perfil.Loaded) { Falla "El perfil de $Usuario está en uso (sesión abierta). Ciérrala con 'logoff' y vuelve a correr."; exit 1 }
        $perfil | Remove-CimInstance
        Ok ("Perfil quitado: {0}" -f $perfil.LocalPath)
        $lineas += ("Perfil quitado: {0}" -f $perfil.LocalPath)
    }
    Remove-LocalUser -Name $Usuario
    Ok "Usuario $Usuario quitado."
    $lineas += "Usuario $Usuario quitado"
} else {
    Ok "El usuario $Usuario ya no existe."
    $lineas += "El usuario $Usuario ya no existía"
}

if (Get-LocalUser -Name $Usuario -ErrorAction SilentlyContinue) { Falla 'El usuario sigue existiendo.'; exit 1 }
Add-Evidencia '0.9.md' 'Usuario de prueba quitado (L1-T017, A-19)' $lineas
Write-Host ''
Write-Host 'Siguiente: Measure-BridgeRestart.ps1 -Reiniciar (L1-T018).'
