# Uso: .\apply-migration.ps1 -Script add-google-calendar.sql
#      .\apply-migration.ps1 -Script add-google-calendar.sql -Server "EQUIPO\SQLEXPRESS"
#
# Sin -Server prueba, en este orden, el contenedor Docker y las instancias locales conocidas,
# y aplica el script en todas las que respondan. Termina con código 1 si no lo aplicó en ninguna.
param(
    [string]$Script = "add-job-offer-sort-order.sql",
    # Instancia concreta (autenticación de Windows). Si se omite, prueba las conocidas.
    [string]$Server,
    [string]$Database = "DailyTime"
)

$scriptPath = Join-Path $PSScriptRoot $Script
if (-not (Test-Path $scriptPath)) {
    throw "No existe el script de migración: $scriptPath"
}

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "No se encontró sqlcmd en el PATH. Instala las herramientas de línea de comandos de SQL Server."
}

# -I activa QUOTED_IDENTIFIER, que sqlcmd deja en OFF al conectar. Sin él, crear un índice
# filtrado (los de GoogleEventId, por ejemplo) falla con el error 1934 a mitad del script.
# -l 8 acorta la espera de login: se prueban varios destinos y casi todos no existen.
$commonArgs = @("-C", "-b", "-I", "-l", "8", "-d", $Database)

# Para Docker el SQL va por -Q: el prefijo evita que un comentario inicial "--" se lea como opción de sqlcmd.
$sqlScript = "SET NOCOUNT ON;`n" + (Get-Content -Raw $scriptPath).Replace("`r`nGO", "").Replace("`nGO", "")

$aplicadas = [System.Collections.Generic.List[string]]::new()
$fallidas = [System.Collections.Generic.List[string]]::new()

<#
.SYNOPSIS
Devuelve la tubería con nombre de una instancia LocalDB, o $null si no está en marcha.

.DESCRIPTION
Hace falta porque el driver ODBC 17 no arranca una instancia de LocalDB que esté detenida:
falla con "Server is not found". Si ya está en marcha conecta sin problema, así que aquí se
intenta arrancarla y, si el estado que reporta no es de fiar, se va directo a su tubería.
#>
function Resolve-LocalDbPipe {
    param([string]$Instance)

    if (-not (Get-Command sqllocaldb -ErrorAction SilentlyContinue)) {
        Write-Host "  No se encontró sqllocaldb en el PATH."
        return $null
    }

    # Que la instancia pedida exista, antes que nada: si no, no hay ninguna tubería que
    # valga y adivinar una sería migrar una base que nadie pidió. Se comprueba contra la
    # lista de nombres porque `sqllocaldb info <instancia>` devuelve 0 aunque no exista, y
    # su mensaje de error está traducido.
    $instancias = @(& sqllocaldb info 2>$null | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    if ($instancias -notcontains $Instance) {
        Write-Host "  La instancia de LocalDB '$Instance' no existe. Disponibles: $($instancias -join ', ')"
        return $null
    }

    # Arrancarla es idempotente. Si falla, la tubería dirá la verdad de todos modos.
    & sqllocaldb start $Instance *> $null

    # La salida de `sqllocaldb info` viene traducida, así que se busca el patrón de la
    # tubería en cualquier línea en vez de una etiqueta concreta.
    $info = & sqllocaldb info $Instance 2>$null
    $encontrada = $info |
        Select-String -Pattern '\\\\\.\\pipe\\LOCALDB#\S+' |
        Select-Object -First 1
    if ($encontrada) {
        return $encontrada.Matches[0].Value
    }

    # `info` reporta la instancia como detenida cuando la arrancó otro proceso (la API, por
    # ejemplo) en lugar del usuario actual. La tubería abierta lo desmiente, pero su nombre
    # es un hash: no dice a qué instancia pertenece. Solo es atribuible sin ambigüedad si en
    # el equipo hay una única instancia y una única tubería.
    $tuberias = @([System.IO.Directory]::GetFiles("\\.\pipe\") |
        Where-Object { $_ -like "*LOCALDB#*tsql\query" })

    if ($tuberias.Count -eq 1 -and $instancias.Count -eq 1) {
        return $tuberias[0]
    }
    if ($tuberias.Count -gt 0) {
        Write-Host "  Hay $($tuberias.Count) tubería(s) de LocalDB y $($instancias.Count) instancia(s): no se puede saber cuál es '$Instance'."
        Write-Host "  Indícala a mano con -Server np:<tubería>:"
        Write-Host ("  " + ($tuberias -join "`n  "))
    }
    return $null
}

<#
.SYNOPSIS
Ejecuta el script en un servidor. Devuelve $true si sqlcmd terminó con éxito.
#>
function Invoke-Migration {
    param(
        [string]$ServerName,
        [string]$User,
        [string]$Pass
    )

    $sqlArgs = @("-S", $ServerName) + $commonArgs + @("-i", $scriptPath)
    $sqlArgs += if ($User) { @("-U", $User, "-P", $Pass) } else { @("-E") }

    & sqlcmd @sqlArgs
    return ($LASTEXITCODE -eq 0)
}

<#
.SYNOPSIS
Aplica el script en un destino, con el rodeo por tubería si es LocalDB y la vía directa falla.
#>
function Invoke-Target {
    param(
        [string]$ServerName,
        [string]$User,
        [string]$Pass
    )

    Write-Host "Intentando conectar a $ServerName..."
    if (Invoke-Migration -ServerName $ServerName -User $User -Pass $Pass) {
        Write-Host "Migración ejecutada exitosamente en $ServerName"
        $aplicadas.Add($ServerName)
        return $true
    }

    if ($ServerName -match '^\(localdb\)\\(?<instancia>.+)$') {
        $instancia = $Matches['instancia']
        Write-Host "  No respondió $ServerName; intentando arrancar LocalDB y usar su tubería..."
        # Resolve-LocalDbPipe ya explicó por qué no pudo; aquí solo se cierra el intento.
        $tuberia = Resolve-LocalDbPipe -Instance $instancia
        if (-not $tuberia) {
            $fallidas.Add($ServerName)
            return $false
        }

        $porTuberia = "np:$tuberia"
        Write-Host "  Reintentando por $porTuberia"
        if (Invoke-Migration -ServerName $porTuberia -User $User -Pass $Pass) {
            Write-Host "Migración ejecutada exitosamente en $ServerName (vía tubería)"
            $aplicadas.Add("$ServerName (vía tubería)")
            return $true
        }
    }

    Write-Host "No se pudo aplicar en $ServerName"
    $fallidas.Add($ServerName)
    return $false
}

if ($Server) {
    Write-Host "Aplicando $Script en $Server..."
    if (-not (Invoke-Target -ServerName $Server)) {
        throw "Falló la migración en $Server."
    }
    Write-Host "Listo."
    # exit explícito: si no, $LASTEXITCODE se queda con el del último sqlcmd que falló.
    exit 0
}

Write-Host "Aplicando $Script. Verificando conexiones a base de datos..."

# Contenedor Docker
$dockerOut = & docker ps --filter "name=dailytime-db" --format "{{.Names}}" 2>&1
if ($LASTEXITCODE -eq 0 -and $dockerOut -match "dailytime-db") {
    Write-Host "Contenedor Docker dailytime-db detectado. Ejecutando migración..."
    & docker exec -i dailytime-db /opt/mssql-tools18/bin/sqlcmd `
        -S localhost -U sa -P "DailyTime_Str0ng!" -C -b -I -d $Database -Q "$sqlScript"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Migración ejecutada en Docker."
        $aplicadas.Add("Docker dailytime-db")
    } else {
        Write-Host "Falló la migración en Docker (exit=$LASTEXITCODE)."
        $fallidas.Add("Docker dailytime-db")
    }
} else {
    Write-Host "Docker no disponible o sin contenedor dailytime-db; se omite."
}

# Instancias locales
$instances = @(
    @{ Server = "LENOVO-PROJECT\SQLEXPRESS04" },
    @{ Server = "(localdb)\MSSQLLocalDB" },
    @{ Server = "localhost,1433"; User = "sa"; Pass = "DailyTime_Str0ng!" }
)

foreach ($inst in $instances) {
    Invoke-Target -ServerName $inst.Server -User $inst.User -Pass $inst.Pass | Out-Null
}

Write-Host ""
Write-Host "--- Resumen ---"
if ($aplicadas.Count -gt 0) {
    Write-Host "Aplicada en: $($aplicadas -join ', ')"
}
if ($fallidas.Count -gt 0) {
    Write-Host "Sin aplicar en: $($fallidas -join ', ')"
}

if ($aplicadas.Count -eq 0) {
    # Que no pase por buena una migración que no llegó a ninguna base.
    Write-Host "La migración no se aplicó en ninguna base de datos."
    exit 1
}

# exit explícito: sin él $LASTEXITCODE conserva el del último sqlcmd, que casi siempre es el
# de un destino inexistente, y quien llame al script creerá que falló.
exit 0
