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
No hay suite de tests automatizada en ninguno de los dos proyectos .NET actualmente.

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

## Arquitectura de `dailyTimeApi`

Capas estrictas **Controller → Service → Repository → `AppDbContext` (EF Core)**, todas registradas por interfaz en [ServiceCollectionExtensions.cs](dailyTimeApi/Extensions/ServiceCollectionExtensions.cs) con `AddScoped`. Al añadir una entidad nueva se replica el patrón: `Models/Entities` + `Repository/Interfaces` + `Repository` + `Services/Interfaces` + `Services` + `Controllers`, y se registra el par repo/service en esa extensión.

Dominios principales expuestos por los controllers:
- **Productividad**: `TaskItems`, `Notes`, `TimeEntries`, `Projects`, `WorkItemStatuses`, `WorkItemCategories` (catálogos compartidos entre tareas y notas, diferenciados por `ItemType` = `task`/`note`).
- **Bóveda de credenciales**: `VaultAccounts`, `VaultPasswords`, `VaultServices`.
- **Carrera laboral**: `CareerProfile`, `CareerCompanies/Positions/Locations/Fields/Technologies`, `CareerApplicationStatuses`, `WorkExperiences`, `JobApplications`, `FitScore` (match candidato-oferta).
- **Scraping de empleo**: `JobPortals`, `JobOffers`, `Companies`, `People` — alimentados por `dailyTimeWorker`.

Config vía `IConfiguration`, no hardcodeada: `ConnectionStrings:DefaultConnection`, `Cors:Origins`, `Database:EnsureCreated`, `Database:SeedMinimal`, `DisableHttpsRedirection`. En Docker estos se pasan como variables de entorno (`Database__EnsureCreated`, etc.); en local van en `appsettings.Development.json` (gitignored en su mayor parte — no subir credenciales).

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
