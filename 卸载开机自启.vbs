Option Explicit
' Remove DeepSeekPet from the current user's Startup folder (ASCII-only on purpose).
Dim sh, fso, startup, lnk
Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
startup = sh.SpecialFolders("Startup")
lnk = startup & "\DeepSeekPet.lnk"
If fso.FileExists(lnk) Then
    fso.DeleteFile lnk, True
    MsgBox "Removed from startup.", 64, "DeepSeek Pet"
Else
    MsgBox "No startup shortcut was found.", 64, "DeepSeek Pet"
End If