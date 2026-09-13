# DailyTime Worker

API .NET de **jobs de fondo**: scraper de portales de empleo, notificaciones y correo.

| | |
|--|--|
| HTTP | `http://localhost:5500` |
| HTTPS / Swagger | `https://localhost:5510/swagger` |
| Habla con | DailyTime API (`https://localhost:5110`) |

## Arranque

```bat
cd dailyTimeWorker
dotnet run --launch-profile https
```

O usa `start-local.bat` / el tray (incluye el worker).

## Playwright (recomendado)

Tras el primer `dotnet build`:

```powershell
cd dailyTimeWorker
dotnet build
pwsh bin/Debug/net8.0/playwright.ps1 install chromium
```

Sin browsers, el worker hace un **smoke HTTP** (abre la URL y reporta status).

## Captura manual

1. Configura el portal con `ScrapeConfig` (selectores flexibles).
2. En la web pulsa **Capturar** → llama a `POST /api/jobs/scrape/{id}`.
3. El worker scrapea y hace upsert en `JobOffer`.
4. Revisa **Carrera → Ofertas**.

Si ya hay una captura en curso (manual o programada), `POST /api/jobs/scrape/{id}` responde `409`.

## Captura programada

- **Horario global** (tabla `ScrapeSchedule` de la API): las mismas horas `HH:mm` y días para todos los
  portales. Se edita en la web: **Portales → Ejecución automática**.
- **Portales incluidos**: los activos con el check **Automática** (`JobPortal.AutoScrapeEnabled`).
- **Sin sondeo**: `PortalScrapeBackgroundService` pide el horario a la API, calcula la próxima hora y espera
  con un temporizador de Windows de hora absoluta que además despierta el equipo de la suspensión.
  Al guardar el horario, la web llama a `POST /api/schedule/reload` y el worker recalcula.
- **Ejecución**: a cada hora corre los portales marcados uno tras otro y pide a Windows no suspender el
  equipo mientras tanto.
- **Horas perdidas** (equipo apagado, suspendido sin despertar, u otra captura en curso) se **omiten** y
  quedan registradas como `skipped`; tolerancia de 10 minutos de retraso.
- `Worker:EnableAutoScrape` es el interruptor general (con `false` solo atiende capturas manuales).

Para que arranque solo al iniciar sesión:

```powershell
.\scripts\apply-migration.ps1 -Script add-scrape-schedule.sql   # una vez, en BD existentes
.\scripts\register-scheduled-task.ps1                            # tarea de Windows (API + worker)
```

Además, habilita **Permitir temporizadores de reactivación** en las opciones de energía de Windows.

## ScrapeConfig flexible (por portal)

```json
{
  "listSelectors": [".card-a", ".card-b"],
  "titleSelectors": ["h2 a", "h3"],
  "linkSelectors": ["h2 a", "a"],
  "companySelectors": [".company"],
  "locationSelectors": [".location"],
  "waitForSelector": "h2 a",
  "maxItems": 30,
  "scrollTimes": 2
}
```

Si Indeed/LinkedIn/elempleo cambian el HTML, solo editas ese JSON del portal.

## Notificaciones / correo

Configura en `appsettings.json`:

```json
"Notifications": {
  "Smtp": {
    "Enabled": true,
    "Host": "smtp.tu-proveedor.com",
    "Port": 587,
    "UseSsl": true,
    "User": "...",
    "Password": "...",
    "From": "noreply@tudominio.com"
  }
}
```

Endpoint: `POST /api/notifications/email`

## Próximos pasos sugeridos

1. Ajustar selectores portal por portal.
2. Tabla `JobOffer` en la API principal para persistir ofertas.
3. Correo/alertas cuando aparezcan ofertas nuevas.
