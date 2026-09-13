# Registra (o elimina con -Unregister) la tarea de Windows que arranca la API y el worker
# de DailyTime al iniciar sesión.
#
# La tarea NO tiene horario propio: las horas de captura se definen en la web
# (Portales > Ejecución automática) y el worker programa el despertar del equipo para cada una.
#
# Uso:
#   .\scripts\register-scheduled-task.ps1
#   .\scripts\register-scheduled-task.ps1 -Unregister

param(
    [switch]$Unregister
)

$ErrorActionPreference = "Stop"
$taskName = "DailyTime - Servicios de captura"

if ($Unregister) {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
    Write-Host "Tarea '$taskName' eliminada."
    return
}

$script = Join-Path $PSScriptRoot "start-scraping-services.ps1"
$user = "$env:USERDOMAIN\$env:USERNAME"

$action = New-ScheduledTaskAction -Execute "powershell.exe" `
    -Argument "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$script`""
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $user
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit ([TimeSpan]::Zero)
# Interactive: el scraper abre Chrome con ventana y necesita la sesión del usuario.
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited

Register-ScheduledTask `
    -TaskName $taskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Principal $principal `
    -Description "Arranca la API y el worker de DailyTime al iniciar sesión (captura automática de portales)." `
    -Force | Out-Null

Write-Host "Tarea '$taskName' registrada: arranca API + worker al iniciar sesión." -ForegroundColor Green
Write-Host ""
Write-Host "Para que el equipo despierte a las horas de captura, habilita los temporizadores de reactivación:" -ForegroundColor Yellow
Write-Host "  Opciones de energía > Cambiar la configuración del plan > Cambiar la configuración avanzada"
Write-Host "  > Suspender > Permitir temporizadores de reactivación > Habilitar (con batería y con corriente)"
Write-Host ""
Write-Host "Si aparece 'Acceso denegado', ejecuta este script desde PowerShell como administrador."
