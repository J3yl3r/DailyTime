Option Explicit

' DailyTime — arranca Docker en segundo plano (sin consola) y abre ventana tipo app.
' Doble clic en este archivo = servicios ocultos + DailyTime como "escritorio".

Dim shell, fso, root, url, composeFile, cmd, browser, i, candidates

Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

root = fso.GetParentFolderName(WScript.ScriptFullName)
url = "http://localhost:4010/workspace"

' Usa docker-compose.local.yml si existe (SQL local); si no, el compose completo.
composeFile = "docker-compose.local.yml"
If Not fso.FileExists(root & "\" & composeFile) Then
  composeFile = "docker-compose.yml"
End If

cmd = "cmd /c cd /d """ & root & """ && docker compose -f " & composeFile & " up -d"
shell.Run cmd, 0, True

WScript.Sleep 6000

Call OpenAppWindow(url)

Sub OpenAppWindow(targetUrl)
  candidates = Array( _
    shell.ExpandEnvironmentStrings("%ProgramFiles%\Google\Chrome\Application\chrome.exe"), _
    shell.ExpandEnvironmentStrings("%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe"), _
    shell.ExpandEnvironmentStrings("%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe"), _
    shell.ExpandEnvironmentStrings("%ProgramFiles%\Microsoft\Edge\Application\msedge.exe") _
  )

  For i = 0 To UBound(candidates)
    If fso.FileExists(candidates(i)) Then
      shell.Run """" & candidates(i) & """ --app=" & targetUrl, 1, False
      Exit Sub
    End If
  Next

  shell.Run targetUrl, 1, False
End Sub
