Option Explicit

' Solo abre DailyTime como ventana de app (Docker ya debe estar corriendo).

Dim shell, fso, browser, i, candidates, url

Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

url = "http://localhost:4010/workspace"

candidates = Array( _
  shell.ExpandEnvironmentStrings("%ProgramFiles%\Google\Chrome\Application\chrome.exe"), _
  shell.ExpandEnvironmentStrings("%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe"), _
  shell.ExpandEnvironmentStrings("%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe"), _
  shell.ExpandEnvironmentStrings("%ProgramFiles%\Microsoft\Edge\Application\msedge.exe") _
)

For i = 0 To UBound(candidates)
  If fso.FileExists(candidates(i)) Then
    shell.Run """" & candidates(i) & """ --app=" & url, 1, False
    WScript.Quit 0
  End If
Next

shell.Run url, 1, False
