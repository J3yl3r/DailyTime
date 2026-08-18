# DailyTime

Monorepo de **DailyTime**: productividad personal, carrera laboral, bóveda de credenciales y scraping de ofertas.

## Servicios

| Carpeta | Stack | Descripción |
|---------|-------|-------------|
| `daily-time-web` | Next.js | Frontend principal |
| `dailyTimeApi` | .NET 8 | API REST |
| `daily-time-voice` | Python / FastAPI | Comandos de voz |
| `dailyTimeWorker` | .NET + Playwright | Scraping de portales |
| `daily-time-tray` | .NET (Windows) | Icono en bandeja del sistema |

## Arranque rápido

### Docker (todo incluido + SQL Server)

```bash
docker compose up --build -d
```

- Web: http://localhost:4010  
- API: http://localhost:5100/swagger  
- Voz: http://localhost:5400  

Ver [DOCKER.md](./DOCKER.md).

### Local sin Docker (Windows)

```bat
start-local.bat
```

Ver [ARRANQUE.md](./ARRANQUE.md) para bandeja, PWA y arranque automático.

## Configuración

1. Copia `.env.example` → `.env` en la raíz (solo Docker).
2. API: edita `dailyTimeApi/appsettings.Development.json` con tu SQL Server local.
3. Worker: opcional `dailyTimeWorker/appsettings.Local.json` para rutas de Chrome/perfil.
4. Voz: copia `daily-time-voice/.env.example` → `.env`.

**No subas** `.env`, contraseñas ni la carpeta `documentos/` (CVs).

## Scripts SQL

En `scripts/`:

- `migrate-career-profile.sql` — perfil de postulación
- `portal-configs/` — configuración de portales
- Ver también scripts de seed en la carpeta

## Estructura

```
DailyTime/
├── daily-time-web/
├── dailyTimeApi/
├── daily-time-voice/
├── dailyTimeWorker/
├── daily-time-tray/
├── scripts/
├── docker-compose.yml
├── docker-compose.local.yml
├── DOCKER.md
└── ARRANQUE.md
```

## Repos anteriores

Este monorepo reemplaza los repos separados (`daily-time-web`, `dailyTimeApi`, etc.).  
Usa **un solo clone** y **un solo push** para todo el proyecto.
