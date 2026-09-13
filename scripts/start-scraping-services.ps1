# Arranca en segundo plano la API y el worker de DailyTime si no están corriendo.
# Lo ejecuta la tarea de Windows registrada con register-scheduled-task.ps1 al iniciar sesión.
# Las horas de captura NO se definen aquí: se configuran en la web (Portales > Ejecución automática)
# y el worker programa el despertar del equipo para cada una.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$logsDir = Join-Path $root "logs\scheduled"
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null

function Test-Port([int]$Port) {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $task = $client.ConnectAsync("127.0.0.1", $Port)
        return ($task.Wait(500) -and $client.Connected)
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function Start-DotnetService([string]$Name, [string]$Folder, [int[]]$Ports) {
    foreach ($port in $Ports) {
        if (Test-Port $port) {
            Write-Host "$Name ya está corriendo (puerto $port)."
            return
        }
    }

    $log = Join-Path $logsDir "$Name.log"
    Write-Host "Iniciando $Name (log: $log)..."

    # cmd /c con redirección: sin ventana y el proceso sigue vivo cuando este script termina.
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "cmd.exe"
    $psi.Arguments = "/c dotnet run --launch-profile https > `"$log`" 2>&1"
    $psi.WorkingDirectory = Join-Path $root $Folder
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    [System.Diagnostics.Process]::Start($psi) | Out-Null
}

Start-DotnetService -Name "api" -Folder "dailyTimeApi" -Ports @(5110, 5100)
Start-DotnetService -Name "worker" -Folder "dailyTimeWorker" -Ports @(5510, 5500)
