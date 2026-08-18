# Abre Chrome con remote debugging en un perfil DEDICADO.
# Chrome moderno bloquea --remote-debugging-port en el User Data por defecto.
# Por eso usamos una carpeta aparte (clon) y no el perfil principal "vivo".

$ErrorActionPreference = "Stop"
$chrome = "C:\Program Files\Google\Chrome\Application\chrome.exe"
$port = 9222
$sourceUserData = "$env:LOCALAPPDATA\Google\Chrome\User Data"
$debugUserData = "$env:LOCALAPPDATA\DailyTime\chrome-debug-profile"
$profileName = "Default"

if (-not (Test-Path $chrome)) {
    throw "No se encontró Chrome en: $chrome"
}

function Copy-ChromeProfileLite {
    param(
        [string]$SourceRoot,
        [string]$DestRoot,
        [string]$Profile
    )

    $skipDirs = @(
        "Cache", "Code Cache", "GPUCache", "GrShaderCache", "ShaderCache",
        "DawnCache", "Crashpad", "Crash Reports", "blob_storage",
        "BrowserMetrics", "Media Cache", "optimization_guide_prediction_model_downloads"
    )
    $skipFiles = @("SingletonCookie", "SingletonLock", "SingletonSocket", "DevToolsActivePort", "lockfile")

    if (Test-Path $DestRoot) {
        Write-Host "Reutilizando perfil debug existente: $DestRoot" -ForegroundColor Cyan
        return
    }

    Write-Host "Creando perfil debug (clon ligero) desde tu perfil $Profile..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $DestRoot -Force | Out-Null

    foreach ($rootFile in @("Local State", "First Run", "Last Version")) {
        $src = Join-Path $SourceRoot $rootFile
        if (Test-Path $src) {
            Copy-Item $src (Join-Path $DestRoot $rootFile) -Force -ErrorAction SilentlyContinue
        }
    }

    $srcProfile = Join-Path $SourceRoot $Profile
    $dstProfile = Join-Path $DestRoot $Profile
    if (-not (Test-Path $srcProfile)) {
        throw "No existe el perfil fuente: $srcProfile"
    }

    robocopy $srcProfile $dstProfile /E /XD $skipDirs /XF $skipFiles /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "Falló la copia del perfil (robocopy exit=$LASTEXITCODE)"
    }
}

# Cerrar Chrome del perfil debug si quedó abierto
$running = Get-CimInstance Win32_Process -Filter "Name = 'chrome.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -like "*chrome-debug-profile*" -or $_.CommandLine -like "*remote-debugging-port=$port*" }
if ($running) {
    Write-Host "Cerrando instancias previas de Chrome debug..." -ForegroundColor Yellow
    $running | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 2
}

Copy-ChromeProfileLite -SourceRoot $sourceUserData -DestRoot $debugUserData -Profile $profileName

Write-Host "Abriendo Chrome debug en puerto $port..." -ForegroundColor Cyan
Start-Process -FilePath $chrome -ArgumentList @(
    "--remote-debugging-port=$port",
    "--remote-allow-origins=*",
    "--user-data-dir=`"$debugUserData`"",
    "--profile-directory=$profileName",
    "--no-first-run",
    "--no-default-browser-check"
)

$ok = $false
for ($i = 1; $i -le 15; $i++) {
    Start-Sleep -Seconds 1
    try {
        $info = Invoke-RestMethod -Uri "http://127.0.0.1:$port/json/version" -TimeoutSec 2
        Write-Host "OK. CDP activo en http://127.0.0.1:$port" -ForegroundColor Green
        Write-Host "Browser: $($info.Browser)"
        Write-Host "WebSocket: $($info.webSocketDebuggerUrl)"
        Write-Host ""
        Write-Host "Deja ESTA ventana abierta y lanza Capturar Indeed." -ForegroundColor Green
        Write-Host "Si pide login/Cloudflare, resuélvelo aquí una vez; se guarda en el perfil debug." -ForegroundColor Green
        $ok = $true
        break
    }
    catch {
        Write-Host "Esperando CDP... ($i/15)"
    }
}

if (-not $ok) {
    throw "Chrome abrió, pero el puerto $port no respondió. Revisa firewall o vuelve a ejecutar el script."
}
