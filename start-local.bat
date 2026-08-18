@echo off
setlocal
cd /d "%~dp0"

set "ROOT=%CD%"
set "API=%ROOT%\dailyTimeApi"
set "WORKER=%ROOT%\dailyTimeWorker"
set "WEB=%ROOT%\daily-time-web"
set "VOICE=%ROOT%\daily-time-voice"

echo DailyTime — arranque local (usa tu SQL Server Express)
echo.

where dotnet >nul 2>&1 || (echo Falta .NET SDK.& pause & exit /b 1)
where npm >nul 2>&1 || (echo Falta Node/npm.& pause & exit /b 1)

:: API (.NET) — ventana minimizada
start "DailyTime API" /MIN cmd /k "cd /d "%API%" && dotnet run --launch-profile https"

:: Worker (.NET) — scraper + notificaciones
start "DailyTime Worker" /MIN cmd /k "cd /d "%WORKER%" && dotnet run --launch-profile https"

:: Web (Next.js)
start "DailyTime Web" /MIN cmd /k "cd /d "%WEB%" && npm run dev"

:: Voz (Python) — usa venv si existe
if exist "%VOICE%\.venv\Scripts\python.exe" (
  start "DailyTime Voice" /MIN cmd /k "cd /d "%VOICE%" && .venv\Scripts\python.exe -m app"
) else (
  start "DailyTime Voice" /MIN cmd /k "cd /d "%VOICE%" && python -m app"
)

echo Esperando servicios...
timeout /t 10 /nobreak >nul

start "" "http://localhost:4010"
echo.
echo Listo. App en http://localhost:4010
echo Worker Swagger: https://localhost:5510/swagger
echo Ventanas: API / Worker / Web / Voice
echo Para cerrar: cierra esas ventanas.
endlocal
