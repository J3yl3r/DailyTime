# Siembra catálogos + experiencias laborales desde el CV (documentos/).
# Datos embebidos del DOCX optimizado (Siempre Net / GSE).
#
# Uso (API levantada):
#   powershell -ExecutionPolicy Bypass -File .\scripts\seed-work-experiences-from-cv.ps1

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Web

$apiBase = "https://localhost:5110/api"

# Confiar en certificado de desarrollo localhost
if (-not ([System.Management.Automation.PSTypeName]"TrustAllCertsPolicy").Type) {
    Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest req, int problem) { return true; }
}
"@
}
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

function ConvertTo-JsonString {
    param([string]$Value)
    if ($null -eq $Value) { return "null" }
    return '"' + [System.Web.HttpUtility]::JavaScriptStringEncode($Value) + '"'
}

function Invoke-ApiJson {
    param(
        [string]$Method,
        [string]$Uri,
        [string]$Body = $null
    )

    $temp = Join-Path $env:TEMP "dailytime-seed-payload.json"
    $out = Join-Path $env:TEMP "dailytime-seed-response.json"
    if ($null -ne $Body) {
        [System.IO.File]::WriteAllText($temp, $Body, (New-Object System.Text.UTF8Encoding($false)))
        $status = curl.exe -sk -S --max-time 30 -X $Method $Uri `
            -H "Content-Type: application/json; charset=utf-8" `
            --data-binary "@$temp" -o $out -w "%{http_code}"
    }
    else {
        $status = curl.exe -sk -S --max-time 30 -X $Method $Uri -o $out -w "%{http_code}"
    }

    if ($status -notin @("200", "201")) {
        Write-Host ("{0} {1} -> HTTP {2}" -f $Method, $Uri, $status) -ForegroundColor Red
        if (Test-Path $out) { Get-Content $out -Raw }
        throw "Falló $Method $Uri"
    }

    if (-not (Test-Path $out) -or (Get-Item $out).Length -eq 0) { return $null }
    return (Get-Content $out -Raw -Encoding UTF8 | ConvertFrom-Json)
}

function Get-CatalogItems {
    param([string]$Kind)
    $res = Invoke-ApiJson -Method "GET" -Uri "$apiBase/career/$Kind"
    return @($res.data)
}

function Ensure-CatalogItem {
    param(
        [string]$Kind,
        [string]$Name,
        [string]$Description = $null
    )

    $items = Get-CatalogItems -Kind $Kind
    $existing = $items | Where-Object { $_.name -eq $Name } | Select-Object -First 1
    if ($existing) {
        Write-Host ("  = {0}: {1} (id={2})" -f $Kind, $Name, $existing.id) -ForegroundColor DarkGray
        return [int]$existing.id
    }

    $body = '{' +
        '"name":' + (ConvertTo-JsonString $Name) + ',' +
        '"description":' + (ConvertTo-JsonString $Description) + ',' +
        '"color":null,' +
        '"sortOrder":null,' +
        '"isActive":true}'

    $res = Invoke-ApiJson -Method "POST" -Uri "$apiBase/career/$Kind" -Body $body
    Write-Host ("  + {0}: {1} (id={2})" -f $Kind, $Name, $res.data.id) -ForegroundColor Green
    return [int]$res.data.id
}

function Ensure-WorkExperience {
    param(
        [int]$CompanyId,
        [int]$PositionId,
        [int]$LocationId,
        [int]$FieldId,
        [string]$StartDate,
        [string]$EndDate,
        [bool]$IsCurrent,
        [string]$Summary,
        [string]$Achievements,
        [int[]]$TechnologyIds
    )

    $all = Invoke-ApiJson -Method "GET" -Uri "$apiBase/work-experiences"
    $existing = @($all.data) | Where-Object {
        $_.companyId -eq $CompanyId -and
        $_.positionId -eq $PositionId -and
        $_.startDate -eq $StartDate
    } | Select-Object -First 1

    $techJson = "[" + (($TechnologyIds | ForEach-Object { "$_" }) -join ",") + "]"
    $endJson = if ($IsCurrent -or [string]::IsNullOrWhiteSpace($EndDate)) { "null" } else { ConvertTo-JsonString $EndDate }

    $body = '{' +
        '"companyId":' + $CompanyId + ',' +
        '"positionId":' + $PositionId + ',' +
        '"locationId":' + $LocationId + ',' +
        '"fieldId":' + $FieldId + ',' +
        '"startDate":' + (ConvertTo-JsonString $StartDate) + ',' +
        '"endDate":' + $endJson + ',' +
        '"isCurrent":' + ($(if ($IsCurrent) { "true" } else { "false" })) + ',' +
        '"summary":' + (ConvertTo-JsonString $Summary) + ',' +
        '"achievements":' + (ConvertTo-JsonString $Achievements) + ',' +
        '"technologyIds":' + $techJson + '}'

    if ($existing) {
        $res = Invoke-ApiJson -Method "PUT" -Uri "$apiBase/work-experiences/$($existing.id)" -Body $body
        Write-Host ("  ~ Experiencia actualizada: {0} / {1} (id={2})" -f $res.data.companyName, $res.data.positionName, $res.data.id) -ForegroundColor Cyan
        return [int]$res.data.id
    }

    $res = Invoke-ApiJson -Method "POST" -Uri "$apiBase/work-experiences" -Body $body
    Write-Host ("  + Experiencia creada: {0} / {1} (id={2})" -f $res.data.companyName, $res.data.positionName, $res.data.id) -ForegroundColor Green
    return [int]$res.data.id
}

Write-Host "=== Seed CV -> WorkExperiences ===" -ForegroundColor Yellow
Write-Host "API: $apiBase" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Catálogos..." -ForegroundColor Yellow
$companySiempre = Ensure-CatalogItem -Kind "companies" -Name "Siempre Net" -Description "Experiencia actual (CV)"
$companyGse = Ensure-CatalogItem -Kind "companies" -Name "GSE" -Description "Experiencia previa (CV)"

$posLider = Ensure-CatalogItem -Kind "positions" -Name "Líder de Proyecto"
$posDevWeb = Ensure-CatalogItem -Kind "positions" -Name "Desarrollador Web"

$locColombia = Ensure-CatalogItem -Kind "locations" -Name "Colombia"
$fieldSoft = Ensure-CatalogItem -Kind "fields" -Name "Desarrollo de software" -Description "Ingeniería / software"

$techNames = @(
    ".NET",
    "ASP.NET Core",
    "C#",
    "React",
    "TypeScript",
    "JavaScript",
    "SQL Server",
    "Azure",
    "Azure DevOps",
    "Git",
    "Tailwind CSS",
    "Redux",
    "Zustand",
    "React Query",
    "Zod",
    "MediatR",
    "MongoDB",
    "HTML",
    "CSS"
)

$techIds = @{}
foreach ($name in $techNames) {
    $techIds[$name] = Ensure-CatalogItem -Kind "technologies" -Name $name
}

Write-Host ""
Write-Host "Experiencias..." -ForegroundColor Yellow

$siempreTech = @(
    $techIds["React"],
    $techIds["TypeScript"],
    $techIds["Zustand"],
    $techIds["React Query"],
    $techIds["Zod"],
    $techIds["ASP.NET Core"],
    $techIds[".NET"],
    $techIds["MediatR"],
    $techIds["SQL Server"],
    $techIds["Git"]
) | Select-Object -Unique

Ensure-WorkExperience `
    -CompanyId $companySiempre `
    -PositionId $posLider `
    -LocationId $locColombia `
    -FieldId $fieldSoft `
    -StartDate "2025-01-01" `
    -EndDate $null `
    -IsCurrent $true `
    -Summary "Líder de Proyecto en Siempre Net. Desarrollo full-stack con React + TypeScript y ASP.NET Core (Vertical Slice, MediatR, Fluent API), SQL Server y principios SOLID." `
    -Achievements @"
- Desarrollo de nuevas funcionalidades con React + TypeScript, Zustand, React Query y Zod.
- Implementación de backend modular con ASP.NET Core, arquitectura Vertical Slice, MediatR y Fluent API.
- Diseño de bases de datos en SQL Server.
- Aplicación de principios SOLID y separación de responsabilidades.
"@ `
    -TechnologyIds $siempreTech

$gseTech = @(
    $techIds["React"],
    $techIds["Redux"],
    $techIds["TypeScript"],
    $techIds["Tailwind CSS"],
    $techIds["JavaScript"],
    $techIds["HTML"],
    $techIds["CSS"],
    $techIds["Git"]
) | Select-Object -Unique

Ensure-WorkExperience `
    -CompanyId $companyGse `
    -PositionId $posDevWeb `
    -LocationId $locColombia `
    -FieldId $fieldSoft `
    -StartDate "2022-01-01" `
    -EndDate "2025-01-01" `
    -IsCurrent $false `
    -Summary "Desarrollador Web en GSE. Interfaces responsivas con React, Redux, TypeScript y Tailwind CSS; componentes reutilizables con Atomic Design y foco en rendimiento frontend." `
    -Achievements @"
- Creación de interfaces responsivas con React, Redux, TypeScript y Tailwind CSS.
- Implementación de componentes reutilizables con Atomic Design.
- Optimización de rendimiento en frontend.
- Desarrollo de soluciones dinámicas y escalables con React y tipado estricto.
"@ `
    -TechnologyIds $gseTech

Write-Host ""
Write-Host "Listo. Revisa Experiencias en la UI." -ForegroundColor Green
