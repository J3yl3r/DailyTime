@echo off
setlocal
cd /d "%~dp0"

echo Arrancando DailyTime con Docker...
docker compose up --build -d
if errorlevel 1 (
  echo.
  echo Error al arrancar. Revisa que Docker Desktop este en marcha.
  pause
  exit /b 1
)

echo.
echo Listo. Abre http://localhost:4010
start "" "http://localhost:4010"
endlocal
