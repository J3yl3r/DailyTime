@echo off
setlocal
cd /d "%~dp0"

echo DailyTime — Docker sin BD (usa tu SQL local)
docker compose -f docker-compose.local.yml up --build -d
if errorlevel 1 (
  echo Error. ¿Docker Desktop esta en marcha?
  pause
  exit /b 1
)

timeout /t 5 /nobreak >nul
start "" "http://localhost:4010"
echo Listo. Contenedores en segundo plano (icono Docker en la bandeja).
endlocal
