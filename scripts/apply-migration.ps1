# Uso: .\apply-migration.ps1 -Script add-scrape-schedule.sql
#      .\apply-migration.ps1 -Script add-scrape-schedule.sql -Server "EQUIPO\SQLEXPRESS"
param(
    [string]$Script = "add-job-offer-sort-order.sql",
    # Instancia concreta (Windows auth). Si se omite, prueba Docker y las instancias conocidas.
    [string]$Server
)

$scriptPath = Join-Path $PSScriptRoot $Script
if (-not (Test-Path $scriptPath)) {
    throw "No existe el script de migración: $scriptPath"
}
# Para Docker el SQL va por -Q: el prefijo evita que un comentario inicial "--" se lea como opción de sqlcmd.
$sqlScript = "SET NOCOUNT ON;`n" + (Get-Content -Raw $scriptPath).Replace("`r`nGO", "").Replace("`nGO", "")

if ($Server) {
    Write-Host "Aplicando $Script en $Server..."
    & sqlcmd -S $Server -E -C -d DailyTime -i $scriptPath -b
    if ($LASTEXITCODE -ne 0) { throw "Falló la migración en $Server (exit=$LASTEXITCODE)." }
    Write-Host "Migración ejecutada exitosamente en $Server"
    return
}

Write-Host "Aplicando $Script. Verificando conexiones a base de datos..."

# Check Docker container
try {
    $dockerOut = & docker ps --filter "name=dailytime-db" --format "{{.Names}}" 2>&1
    if ($dockerOut -match "dailytime-db") {
        Write-Host "Contenedor Docker dailytime-db detectado. Ejecutando migración..."
        & docker exec -i dailytime-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "DailyTime_Str0ng!" -C -d DailyTime -Q "$sqlScript"
        Write-Host "Migración ejecutada en Docker."
    }
} catch {
    Write-Host "Docker no disponible: $_"
}

# Check sqlcmd on local instances
$instances = @(
    @{ Server = "LENOVO-PROJECT\SQLEXPRESS04"; Auth = "Windows" },
    @{ Server = "(localdb)\MSSQLLocalDB"; Auth = "Windows" },
    @{ Server = "localhost,1433"; Auth = "Sql"; User = "sa"; Pass = "DailyTime_Str0ng!" }
)

foreach ($inst in $instances) {
    Write-Host "Intentando conectar a $($inst.Server)..."
    try {
        if ($inst.Auth -eq "Sql") {
            & sqlcmd -S $inst.Server -U $inst.User -P $inst.Pass -C -d DailyTime -i $scriptPath -b
        } else {
            & sqlcmd -S $inst.Server -E -C -d DailyTime -i $scriptPath -b
        }
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Migración ejecutada exitosamente en $($inst.Server)"
        }
    } catch {
        Write-Host "No se pudo conectar a $($inst.Server)"
    }
}

# Check .NET EF Core / EnsureCreated if API is run
Write-Host "Verificación finalizada."
