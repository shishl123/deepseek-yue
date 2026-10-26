Option Explicit
' 移除开机自启
Dim sh, fso, startup, lnk
Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
startup = sh.SpecialFolders("Startup")
lnk = startup & "\DeepSeekPet.lnk"
If fso.FileExists(lnk) Then
    fso.DeleteFile lnk, True
    MsgBox "已取消开机自启。", 64, "DeepSeek 桌宠"
Else
    MsgBox "没有找到开机自启项，无需处理。", 64, "DeepSeek 桌宠"
End If
