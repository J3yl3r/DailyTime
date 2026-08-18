# DailyTime — arranque automático y datos

## ¿Docker borra mis datos al reiniciar el PC?

**No**, salvo que tú lo borres a propósito.

| Escenario | ¿Se pierden datos? |
|-----------|-------------------|
| Apagas el PC y vuelves a encender | **No** (volumen Docker o tu SQL local) |
| `docker compose down` | **No** (el volumen `dailytime_sqldata` sigue) |
| `docker compose down -v` | **Sí** (borra el volumen de SQL en Docker) |
| `scripts/wipe-database.sql` | **Sí** (vacía tablas en la BD que uses) |

### Dónde se guarda cada cosa

| Modo | Dónde viven tus tareas, notas, bóveda… |
|------|----------------------------------------|
| **SQL local** (`DESKTOP-...\SQLEXPRESS`) | En tu instancia Express, carpeta de datos de SQL Server |
| **SQL en Docker** (`docker-compose.yml` servicio `db`) | Volumen Docker `dailytime_sqldata` (disco del PC, no dentro del contenedor efímero) |

Tener **dos SQL** (Express + Docker) = **dos copias separadas** de DailyTime. Para tu caso conviene **una sola**: tu Express.

---

## Modo bandeja de Windows (recomendado sin Docker)

App ligera en .NET que pone un **icono junto al reloj** (como Discord/Docker):

| Archivo | Qué hace |
|---------|----------|
| **`DailyTime-Tray.vbs`** | Arranca el tray **sin consola** |
| **`start-tray.bat`** | Igual, desde terminal |

**Menú del icono (clic derecho):**
- Abrir DailyTime (ventana tipo app)
- Iniciar / Detener servicios (API + Web + Voz **ocultos**)
- Ver logs (`logs/tray/`)
- Salir (pregunta si también detiene servicios)

**Doble clic** en el icono = abrir la app.

Uso diario: acceso directo a `DailyTime-Tray.vbs` en el escritorio.  
Arranque al login: Programador de tareas → ese mismo `.vbs`.

Los logs de consola van a archivos (no a ventanas), salvo que abras la carpeta “Ver logs”.

---

## Modo “app de escritorio” (sin consolas visibles)

### Arranque con Docker


| Archivo | Qué hace |
|---------|----------|
| **`DailyTime-Start.vbs`** | Levanta Docker en segundo plano (`up -d`, sin ventana) y abre DailyTime como ventana de app |
| **`DailyTime-App.vbs`** | Solo abre la ventana de app (Docker ya debe estar corriendo) |
| **`start-docker.bat`** / **`start-docker-local.bat`** | También van en segundo plano (`-d`); no hace falta dejar la consola abierta |

**Uso diario:** doble clic en **`DailyTime-Start.vbs`**.  
Crea un acceso directo en el escritorio a ese `.vbs` y renómbralo a **DailyTime**.

Los servicios siguen activos aunque cierres la ventana de la app. Solo se detienen con `docker compose down`.

### Instalar como PWA (icono en Inicio / barra de tareas)

1. Arranca la web (`http://localhost:3000`)
2. En **Chrome** o **Edge**: menú (⋮) → **Instalar DailyTime** / **Aplicaciones → Instalar este sitio como aplicación**
3. Queda un icono propio, sin barra de pestañas del navegador

La PWA y la ventana `--app=` de Chrome/Edge se sienten igual; la PWA además se puede anclar al Inicio de Windows.

### Arrancar al encender Windows

1. `Win + R` → `taskschd.msc`
2. **Crear tarea básica**
3. Desencadenador: **Al iniciar sesión**
4. Acción: **Iniciar programa** → ruta a `DailyTime-Start.vbs` (o solo `start-docker-local.bat` si prefieres abrir la app manualmente)
5. En Propiedades → General: marcar **Ejecutar esté o no el usuario conectado** si quieres que arranque antes de iniciar sesión (opcional)

---

## Qué verás en la bandeja (como Docker Desktop)

| Enfoque | Bandeja del sistema | Datos |
|---------|---------------------|-------|
| **`DailyTime-Start.vbs` + Docker** | Solo icono **Docker Desktop**; web/api/voz en contenedores | Según compose (local o volumen Docker) |
| **`start-local.bat`** | 3 ventanas **minimizadas** en la barra de tareas | Tu SQL Express |

Docker **no** pone un icono por cada contenedor en la bandeja; eso es normal. Controlas todo desde Docker Desktop o con `docker ps`.

---

## Opción alternativa: local sin Docker

### Acceso directo

Clic derecho en `start-local.bat` → **Enviar a → Escritorio (crear acceso directo)**.

### Sin consolas visibles

Usa **`DailyTime-App.vbs`** después de que `start-local.bat` haya levantado los servicios, o instala la **PWA** como arriba.

---

## Docker sin base de datos duplicada

Si prefieres contenedores pero **tu SQL local**:

```bat
start-docker-local.bat
```

Requiere que la API dentro de Docker pueda conectar a Express (`host.docker.internal\SQLEXPRESS`).  
Si usas solo Windows Auth, es más fácil **`start-local.bat`** (API fuera de Docker).

---

## Comandos útiles

```bat
:: Local (3 procesos minimizados)
start-local.bat

:: Docker con SQL incluido (BD aparte de la tuya)
start-docker.bat

:: Docker sin SQL (apps en contenedor, BD = tu Express)
start-docker-local.bat

:: Parar Docker
docker compose down
docker compose -f docker-compose.local.yml down
```
