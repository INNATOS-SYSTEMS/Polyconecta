<#
.SYNOPSIS
  Deja el bridge arrancando solo después de un reinicio (L1-T016, D-115).

.DESCRIPTION
  1. Inicio de sesión automático del administrador con Autologon de Sysinternals, el mecanismo
     probado en S-04: guarda la contraseña cifrada como secreto LSA, no en el registro en claro.
  2. Tarea programada "PolyConecta-Bridge", "al iniciar sesión" de ese usuario, con LogonType
     Interactive: levanta C:\PolyConecta\bridge\Contpaq.Bridge.exe en su sesión, donde el SDK abre
     la empresa y donde están sus variables de entorno (BridgeConfig__*).
  3. Evidencia, sin secretos, en tools\sdk-lab\evidence\F0\0.9.md.

  Requiere una consola de PowerShell abierta como administrador y el bridge ya publicado
  (Publish-Bridge.ps1). La contraseña se pide al correrlo y no se guarda en ningún archivo.

  Autologon recibe la contraseña como argumento, así que aparece un instante en la línea de comandos
  de ese proceso mientras corre. No queda en el historial de PowerShell ni en disco.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\scripts\vps\Set-BridgeAutostart.ps1
#>
[CmdletBinding()]
param(
    [string]$Usuario = $env:USERNAME,
    [string]$AutologonExe = (Join-Path $env:ProgramData 'Sysinternals\Autologon64.exe'),
    [string]$NombreTarea = 'PolyConecta-Bridge',
    [int]$RetrasoSegundos = 30
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\_comun.ps1"

Write-Host '================================================================='
Write-Host ' Arranque automático del bridge (L1-T016)'
Write-Host '================================================================='

Paso '1. Comprobaciones'
if (-not (Test-Administrador)) { Falla 'Abre PowerShell como administrador ("Ejecutar como administrador").'; exit 1 }
$exe = Join-Path $CarpetaBridge 'Contpaq.Bridge.exe'
if (-not (Test-Path $exe)) { Falla "No está $exe. Corre antes Publish-Bridge.ps1 (L1-T006)."; exit 1 }
if (-not (Get-LocalUser -Name $Usuario -ErrorAction SilentlyContinue)) { Falla "No existe el usuario local $Usuario."; exit 1 }
Ok "Bridge publicado en $CarpetaBridge"
Ok "Usuario del inicio de sesión automático: $Usuario"
if ($Usuario -ne 'Administrator') { Aviso 'D-115 indica la sesión del administrador: solo ahí abre la sesión de Contabilidad (S-04).' }

if (-not (Test-Path $AutologonExe)) {
    Aviso "No está Autologon en $AutologonExe."
    $r = Read-Host 'Descargarlo de https://live.sysinternals.com/Autologon64.exe? (s/n)'
    if ($r -ne 's') { Falla 'Descárgalo y vuelve a correr con -AutologonExe <ruta>.'; exit 1 }
    New-Item -ItemType Directory -Force (Split-Path $AutologonExe) | Out-Null
    Invoke-WebRequest -Uri 'https://live.sysinternals.com/Autologon64.exe' -OutFile $AutologonExe -UseBasicParsing
    $firma = Get-AuthenticodeSignature $AutologonExe
    if ($firma.Status -ne 'Valid' -or $firma.SignerCertificate.Subject -notlike '*Microsoft*') {
        Remove-Item $AutologonExe -Force
        Falla 'La firma de Autologon64.exe no es válida de Microsoft: se borró. Descárgalo a mano.'; exit 1
    }
    Ok 'Autologon descargado y con firma válida de Microsoft.'
} else { Ok "Autologon: $AutologonExe" }

Paso '2. Inicio de sesión automático'
$s = Read-Host ("Contraseña de Windows de {0}" -f $Usuario) -AsSecureString
$plano = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))
& $AutologonExe /accepteula $Usuario $env:COMPUTERNAME $plano | Out-Null
Remove-Variable plano, s
Start-Sleep -Seconds 2
$wl = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
if ($wl.AutoAdminLogon -eq '1' -and $wl.DefaultUserName -eq $Usuario) {
    Ok ("AutoAdminLogon=1 para {0}\{1}" -f $wl.DefaultDomainName, $wl.DefaultUserName)
} else {
    Falla ("El registro no quedó como se esperaba: AutoAdminLogon={0}, DefaultUserName={1}." -f $wl.AutoAdminLogon, $wl.DefaultUserName); exit 1
}
if ($wl.DefaultPassword) { Aviso 'Hay una DefaultPassword en claro en Winlogon (de una configuración anterior). Bórrala: Autologon usa el secreto LSA.' }

Paso '3. Tarea "al iniciar sesión"'
if (Get-ScheduledTask -TaskName $NombreTarea -ErrorAction SilentlyContinue) {
    Unregister-ScheduledTask -TaskName $NombreTarea -Confirm:$false
    Aviso "La tarea $NombreTarea ya existía: se reemplazó."
}
$cuenta = "$env:COMPUTERNAME\$Usuario"
$accion = New-ScheduledTaskAction -Execute $exe -WorkingDirectory $CarpetaBridge
$disparo = New-ScheduledTaskTrigger -AtLogOn -User $cuenta
$disparo.Delay = ('PT{0}S' -f $RetrasoSegundos)
$principal = New-ScheduledTaskPrincipal -UserId $cuenta -LogonType Interactive -RunLevel Highest
$ajustes = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -MultipleInstances IgnoreNew -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
Register-ScheduledTask -TaskName $NombreTarea -Action $accion -Trigger $disparo -Principal $principal -Settings $ajustes `
    -Description 'Levanta el bridge de CONTPAQi (PolyConecta.Contpaq) en la sesión del administrador (D-115).' | Out-Null
$t = Get-ScheduledTask -TaskName $NombreTarea
Ok ("Tarea {0}: {1}, al iniciar sesión de {2} con {3} s de espera" -f $NombreTarea, $t.State, $cuenta, $RetrasoSegundos)

$viejas = @(Get-ScheduledTask -ErrorAction SilentlyContinue | Where-Object { $_.TaskName -ne $NombreTarea -and $_.TaskName -match 'bridge|sdklab' })
foreach ($v in $viejas) { Aviso ("Otra tarea relacionada sigue registrada: {0} ({1}). La de S-04 se quita en L1-T017." -f $v.TaskName, $v.Principal.UserId) }

Add-Evidencia '0.9.md' 'Arranque automático (L1-T016)' @(
    ("Inicio de sesión automático: {0}\{1}, AutoAdminLogon={2}, contraseña como secreto LSA (Autologon de Sysinternals)" -f $wl.DefaultDomainName, $wl.DefaultUserName, $wl.AutoAdminLogon),
    ("Tarea: {0}, estado {1}, disparo al iniciar sesión de {2} (+{3} s), LogonType Interactive, RunLevel Highest" -f $NombreTarea, $t.State, $cuenta, $RetrasoSegundos),
    ("Acción: {0} (carpeta {1}); reintentos: 3 cada minuto; sin límite de tiempo" -f $exe, $CarpetaBridge)
)
Write-Host ''
Write-Host 'Siguiente: Remove-BridgeTestUser.ps1 (L1-T017) y después Measure-BridgeRestart.ps1 (L1-T018).'
