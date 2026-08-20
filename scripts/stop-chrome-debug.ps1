# Cierra solo el Chrome de debug de DailyTime (puerto 9222 + perfil clon).
# No toca el Chrome normal del usuario.

$ErrorActionPreference = "SilentlyContinue"
$port = 9222

function Stop-ProcessTree {
    param([int]$ProcessId)
    if ($ProcessId -le 0) { return }
    Start-Process -FilePath "taskkill.exe" -ArgumentList "/PID", $ProcessId, "/T", "/F" -Wait -WindowStyle Hidden -ErrorAction SilentlyContinue | Out-Null
}

# 1) Proceso que escucha en el puerto CDP (más fiable).
try {
    $listenerPids = @(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique)
    foreach ($pid in $listenerPids) {
        Stop-ProcessTree -ProcessId $pid
    }
} catch {
    # Get-NetTCPConnection puede fallar sin permisos; seguimos con el fallback.
}

Start-Sleep -Milliseconds 500

# 2) Cualquier chrome.exe con el perfil debug o el puerto remoto en la línea de comandos.
$running = @(Get-CimInstance Win32_Process -Filter "Name = 'chrome.exe'" -ErrorAction SilentlyContinue |
    Where-Object {
        $_.CommandLine -and (
            $_.CommandLine -like "*chrome-debug-profile*" -or
            $_.CommandLine -like "*remote-debugging-port=$port*"
        )
    })

foreach ($proc in $running) {
    Stop-ProcessTree -ProcessId $proc.ProcessId
}

Start-Sleep -Milliseconds 500
exit 0
