# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repositorio

Monorepo de **DailyTime**: productividad personal, carrera laboral, bóveda de credenciales y scraping de ofertas de empleo. Sustituye repos separados anteriores — **un solo clone y un solo push** para todo el proyecto.

## Servicios (5)

| Carpeta | Stack | Puerto local (dotnet run / npm dev) | Puerto Docker | Rol |
|---------|-------|---------------------------|---------------|-----|
| `daily-time-web` | Next.js 16 (React 19) | 4010 | 4010 | Frontend |
| `dailyTimeApi` | .NET 8 / EF Core / SQL Server | 5100 (http) / 5110 (https) | 5100→8080 | API REST principal |
| `daily-time-voice` | Python 3.11 / FastAPI | 8000 (local) / 5400 (docker) | 5400 | Comandos de voz en español → llama a la API .NET |
| `dailyTimeWorker` | .NET 8 / Playwright | 5500 (http) / 5510 (https) | — (no dockerizado) | Jobs de fondo: scraping de portales de empleo, notificaciones/correo |
| `daily-time-tray` | .NET 8 (WinForms) | — | — | Icono de bandeja de Windows que arranca/detiene los demás servicios |

Cadena de llamadas: `daily-time-web` → `dailyTimeApi` (datos) y → `daily-time-voice` (comandos de voz, que a su vez llama a `dailyTimeApi`). `dailyTimeWorker` es invocado por la API/web para lanzar scraping y hace upsert directo contra la BD de `dailyTimeApi`.

## Comandos

### Arranque completo
```bat
start-local.bat        :: 4 servicios sin Docker (usa SQL Server Express local), ventanas minimizadas
start-docker.bat        :: Docker con SQL Server incluido (docker-compose.yml)
start-docker-local.bat  :: Docker para apps, SQL local (docker-compose.local.yml)
start-tray.bat           :: solo el icono de bandeja (controla los demás desde ahí)
```
Los `.vbs` en la raíz (`DailyTime-Start.vbs`, `DailyTime-App.vbs`, `DailyTime-Tray.vbs`) son wrappers sin consola de los `.bat` para accesos directos de escritorio.

### daily-time-web (Next.js)
```bash
cd daily-time-web
npm run dev      # puerto 4010
npm run build
npm run lint      # eslint
```

### dailyTimeApi / dailyTimeWorker (.NET)
```bash
cd dailyTimeApi         # o dailyTimeWorker
dotnet run --launch-profile https
dotnet build
```
Pruebas: `dotnet test dailyTimeApi.Tests` (xUnit; hoy cubre la priorización de ofertas). Si la API está corriendo y bloquea sus DLL, compila a otra carpeta con `-o`. El worker no tiene pruebas.

Playwright del worker (una sola vez tras el primer build):
```powershell
cd dailyTimeWorker
dotnet build
pwsh bin/Debug/net8.0/playwright.ps1 install chromium
```
Sin navegadores instalados, el worker cae a un "smoke HTTP" (solo abre la URL y reporta status).

### daily-time-voice (Python/FastAPI)
```bash
cd daily-time-voice
python -m venv .venv
source .venv/Scripts/activate   # Git Bash; en PowerShell: .venv\Scripts\Activate.ps1
pip install -r requirements.txt
cp .env.example .env
python -m app
```

### Docker
```bash
docker compose up --build -d       # todo incluido + SQL Server
docker compose down                # detener (conserva datos)
docker compose down -v             # detener y borrar el volumen SQL (destructivo)
```
Contraseña SQL por defecto: `DailyTime_Str0ng!` (configurable vía `.env` en la raíz, ver `.env.example`).

### Base de datos
Scripts en `scripts/`: `wipe-database.sql` (vacía tablas), `seed-minimal.sql` (estados/categorías base), `migrate-career-profile.sql`, `portal-configs/` (config de portales de scraping).
La API usa `EnsureCreated`, que **no** actualiza una base ya creada: al añadir columnas hay que aplicar el script correspondiente con `./scripts/apply-migration.ps1 -Script <archivo>.sql`.

## Arquitectura de `dailyTimeApi`

Capas estrictas **Controller → Service → Repository → `AppDbContext` (EF Core)**, todas registradas por interfaz en [ServiceCollectionExtensions.cs](dailyTimeApi/Extensions/ServiceCollectionExtensions.cs) con `AddScoped`. Al añadir una entidad nueva se replica el patrón: `Models/Entities` + `Repository/Interfaces` + `Repository` + `Services/Interfaces` + `Services` + `Controllers`, y se registra el par repo/service en esa extensión.

Dominios principales expuestos por los controllers:
- **Productividad**: `TaskItems`, `Notes`, `TimeEntries`, `Projects`, `WorkItemStatuses`, `WorkItemCategories` (catálogos compartidos entre tareas y notas, diferenciados por `ItemType` = `task`/`note`).
- **Bóveda de credenciales**: `VaultAccounts`, `VaultPasswords`, `VaultServices`.
- **Carrera laboral**: `CareerProfile`, `CareerCompanies/Positions/Locations/Fields/Technologies`, `CareerApplicationStatuses`, `WorkExperiences`, `JobApplications`, `FitScore` (match candidato-oferta).
- **Scraping de empleo**: `JobPortals`, `JobOffers`, `Companies`, `People` — alimentados por `dailyTimeWorker`.
- **Priorización de ofertas**: `Services/Triage/OfferScorer` (función pura) puntúa 0–100 (tier A/B/C) y decide descartes con las reglas de `OfferTriageConfig` y el perfil de carrera. Se aplica en el upsert del worker y con `api/job-offers/triage/*` (vista previa, guardar y recalcular). Las reglas nunca cambian un estado con `StatusSource = user`.
- **Calendario de Google**: `Services/Google` sincroniza en los dos sentidos tareas y notas con
  el calendario de una cuenta conectada (OAuth con redirect de loopback). Cada pasada propaga
  borrados, trae cambios remotos y empuja los locales, dentro de una ventana de días; en un
  conflicto gana el cambio más reciente. Corre en `GoogleCalendarSyncBackgroundService` cada
  pocos minutos y al instante tras cada guardado. Las credenciales van en user secrets
  (`Google:ClientId`, `Google:ClientSecret`). Detalle y puesta en marcha en
  [GOOGLE-CALENDAR.md](GOOGLE-CALENDAR.md).
- **Análisis con IA**: `Services/Ai` analiza con Gemini (`generateContent`, capa gratuita) todas las ofertas activas sin análisis, las que entraron más recientemente primero. Lo dispara `OfferAiBackgroundService` tras cada upsert o recálculo, sin sondeo y sin tope propio: el único límite es la cuota de Google (ante un 429 se pausa y sigue sola al renovarse). El resultado (`JobOffer.AiAnalysis`) ajusta el puntaje entre +10 y −20; nunca descarta ni cambia estados. Al modelo no se le envían datos personales del perfil. La clave va en user secrets (`dotnet user-secrets set "Gemini:ApiKey" "..." --project dailyTimeApi`), nunca en appsettings.

Config vía `IConfiguration`, no hardcodeada: `ConnectionStrings:DefaultConnection`, `Cors:Origins`, `Database:EnsureCreated`, `Database:SeedMinimal`, `DisableHttpsRedirection`. En Docker estos se pasan como variables de entorno (`Database__EnsureCreated`, etc.); en local van en `appsettings.Development.json`. Ese archivo **sí está versionado**: los secretos, como `Gemini:ApiKey`, van en user secrets.

**User secrets: nunca los escribas desde la app de escritorio de Claude.** Es un paquete MSIX y Windows redirige lo que sus procesos escriben en `%APPDATA%` a `%LOCALAPPDATA%\Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming\`. Desde Claude la clave parece guardada y funciona, pero los servicios que arranca la bandeja al iniciar sesión leen el `%APPDATA%` real, no la ven y arrancan sin ella, sin dar ningún error (`AddUserSecrets` es opcional). Por eso `dotnet user-secrets set` se ejecuta **siempre en una terminal normal de Windows**; si Claude lo necesita, le pide al usuario que lo haga. Por la misma razón, reiniciar la API desde Claude no sirve para probar nada que dependa de `%APPDATA%`: lo que se lanza desde aquí hereda esa redirección. Comprobación: `curl -sk https://localhost:5110/api/job-offers/ai/status` → `"configured": true`.

## Arquitectura de `dailyTimeWorker`

`ScrapeConfig` por portal (selectores CSS flexibles: `listSelectors`, `titleSelectors`, `linkSelectors`, `companySelectors`, `locationSelectors`, `waitForSelector`, `maxItems`, `scrollTimes`) permite adaptar el scraper a un portal nuevo (Indeed/LinkedIn/elempleo) editando solo JSON, sin tocar código, cuando cambie el HTML del portal. Captura manual: la web dispara `POST /api/jobs/scrape/{id}`, el worker scrapea con Playwright y hace upsert de `JobOffer` en la BD de la API principal. Captura programada: horario global en la API (`ScrapeSchedule`, mismas horas para todos los portales con `JobPortal.AutoScrapeEnabled`); `PortalScrapeBackgroundService` no sondea, espera con un temporizador de Windows de hora absoluta (despierta el equipo) y se recarga con `POST /api/schedule/reload`. Las horas perdidas se omiten (`skipped`). Detalle en [dailyTimeWorker/README.md](dailyTimeWorker/README.md).

## Arquitectura de `daily-time-web`

- Cliente API centralizado en [lib/api/](daily-time-web/lib/api/) — un archivo por dominio (`task-items.ts`, `career.ts`, `vault.ts`, `voice.ts`, `worker.ts`, etc.) sobre [lib/api/client.ts](daily-time-web/lib/api/client.ts).
- Validación con `zod` en [schemas/](daily-time-web/schemas/), un schema por dominio, usados junto con `react-hook-form` + `@hookform/resolvers`.
- Server state con `@tanstack/react-query`; claves centralizadas en [lib/query/keys.ts](daily-time-web/lib/query/keys.ts).
- URLs de los otros servicios en [config/env.ts](daily-time-web/config/env.ts) (`NEXT_PUBLIC_API_URL`, `NEXT_PUBLIC_VOICE_API_URL`, `NEXT_PUBLIC_WORKER_API_URL`), con defaults a `https://localhost:5110` / `http://localhost:5400` / `http://localhost:5500`.
- Alias de import `@/*` → raíz de `daily-time-web`.
- Rutas (App Router) reflejan los dominios de negocio: `board`, `calendar`, `day`, `tasks`, `notes`, `projects`, `career`, `applications`, `experiences`, `people`, `companies`, `vault`, `statuses`, `categories`, `workspace`.
- **Importante (AGENTS.md del proyecto)**: esta versión de Next.js puede tener cambios respecto a lo entrenado — antes de escribir código que dependa de convenciones de Next.js, revisar `node_modules/next/dist/docs/` si hay dudas sobre una API o convención.

## Notas de seguridad / datos

- Nunca subir `.env`, contraseñas ni la carpeta `documentos/` (CVs) — están en `.gitignore`.
- La bóveda de credenciales (`Vault*`) almacena secretos de usuario; no hay OAuth en `daily-time-voice`, asume que la API está abierta en local/escritorio (no exponer estos servicios fuera de localhost sin añadir auth).
