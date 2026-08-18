Option Explicit

' Lanza DailyTime.Tray sin ventana de consola.
' Icono aparece junto al reloj de Windows.

Dim shell, fso, root, cmd

Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
root = fso.GetParentFolderName(WScript.ScriptFullName)

cmd = "cmd /c cd /d """ & root & """ && dotnet run --project """ & root & "\daily-time-tray\DailyTime.Tray.csproj"" -c Release --verbosity quiet"
shell.Run cmd, 0, False
