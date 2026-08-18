@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>&1 || (
  echo Falta .NET SDK.
  pause
  exit /b 1
)

echo Iniciando DailyTime en la bandeja del sistema...
start "" /B dotnet run --project "%~dp0daily-time-tray\DailyTime.Tray.csproj" -c Release --verbosity quiet
endlocal
