# Aplica los ScrapeConfig de scripts/portal-configs a la API de DailyTime.
# Actualiza/crea: indeed, linkedin, computrabajo, elempleo.
#
# Uso:  powershell -ExecutionPolicy Bypass -File .\scripts\apply-portal-configs.ps1

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Web

# La API redirige http -> https, así que se usa el endpoint https directamente.
$api = "https://localhost:5110/api/job-portals"
$configDir = Join-Path $PSScriptRoot "portal-configs"

function ConvertTo-JsonString {
    param([string]$Value)
    if ($null -eq $Value) { return "null" }
    return '"' + [System.Web.HttpUtility]::JavaScriptStringEncode($Value) + '"'
}

function Send-Portal {
    param(
        [string]$Method,
        [string]$Uri,
        [string]$Name,
        [string]$Url,
        [string]$LoginUrl,
        [string]$Notes,
        [string]$ConfigFile
    )

    $config = Get-Content (Join-Path $configDir $ConfigFile) -Raw -Encoding UTF8
    $body = '{' +
        '"name":' + (ConvertTo-JsonString $Name) + ',' +
        '"url":' + (ConvertTo-JsonString $Url) + ',' +
        '"loginUrl":' + (ConvertTo-JsonString $LoginUrl) + ',' +
        '"notes":' + (ConvertTo-JsonString $Notes) + ',' +
        '"scrapeConfig":' + (ConvertTo-JsonString $config) + ',' +
        '"isActive":true}'

    $temp = Join-Path $env:TEMP "dailytime-portal-payload.json"
    [System.IO.File]::WriteAllText($temp, $body, (New-Object System.Text.UTF8Encoding($false)))

    $out = Join-Path $env:TEMP "dailytime-portal-response.json"
    $status = curl.exe -sk -S --max-time 30 -X $Method $Uri `
        -H "Content-Type: application/json; charset=utf-8" `
        --data-binary "@$temp" -o $out -w "%{http_code}"

    Write-Host ("{0} {1} -> HTTP {2}" -f $Method, $Name, $status) -ForegroundColor Cyan
    if ($status -notin @("200", "201")) {
        Get-Content $out -Raw
        throw "Falló la actualización de $Name"
    }
}

$portals = (Invoke-RestMethod -Uri $api).data

$indeed = $portals | Where-Object { $_.name -eq "indeed" } | Select-Object -First 1
if (-not $indeed) { throw "No se encontró el portal 'indeed'." }

Send-Portal -Method "PUT" -Uri "$api/$($indeed.id)" -Name "indeed" `
    -Url "https://co.indeed.com" -LoginUrl $null `
    -Notes "Multi-pais (CO/MX/ES). Requiere resolver Cloudflare en la ventana de Chrome debug." `
    -ConfigFile "indeed.json"

$linkedin = $portals | Where-Object { $_.name -eq "linkedin" } | Select-Object -First 1
$linkedinArgs = @{
    Name       = "linkedin"
    Url        = "https://www.linkedin.com/jobs"
    LoginUrl   = "https://www.linkedin.com/login"
    Notes      = "Colombia, México y España. Requiere sesion iniciada en Chrome debug (start-chrome-debug.ps1)."
    ConfigFile = "linkedin.json"
}

if ($linkedin) {
    Send-Portal -Method "PUT" -Uri "$api/$($linkedin.id)" @linkedinArgs
}
else {
    Send-Portal -Method "POST" -Uri $api @linkedinArgs
}

$computrabajo = $portals | Where-Object { $_.name -eq "computrabajo" } | Select-Object -First 1
$computrabajoArgs = @{
    Name       = "computrabajo"
    Url        = "https://co.computrabajo.com"
    LoginUrl   = $null
    Notes      = "Colombia y México. España pausada: computrabajo.es redirige al selector global."
    ConfigFile = "computrabajo.json"
}

if ($computrabajo) {
    Send-Portal -Method "PUT" -Uri "$api/$($computrabajo.id)" @computrabajoArgs
}
else {
    Send-Portal -Method "POST" -Uri $api @computrabajoArgs
}

$elempleo = $portals | Where-Object { $_.name -eq "elempleo" } | Select-Object -First 1
$elempleoArgs = @{
    Name       = "elempleo"
    Url        = "https://www.elempleo.com/co"
    LoginUrl   = $null
    Notes      = "Colombia. Keywords .NET unificadas con el resto de portales."
    ConfigFile = "elempleo.json"
}

if ($elempleo) {
    Send-Portal -Method "PUT" -Uri "$api/$($elempleo.id)" @elempleoArgs
}
else {
    Send-Portal -Method "POST" -Uri $api @elempleoArgs
}

Write-Host ""
Write-Host "Portales actuales:" -ForegroundColor Green
(Invoke-RestMethod -Uri $api).data | Select-Object id, name, isActive | Format-Table -AutoSize
