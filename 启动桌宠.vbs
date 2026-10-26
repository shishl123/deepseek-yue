Option Explicit
' 静默启动桌宠（不弹控制台窗口）
Dim sh, fso, dir, exe
Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
exe = dir & "\DeepSeekPet.exe"
If Not fso.FileExists(exe) Then
    MsgBox "找不到 DeepSeekPet.exe，请确认它和本脚本在同一目录。", 48, "DeepSeek 桌宠"
    WScript.Quit 1
End If
sh.CurrentDirectory = dir
sh.Run """" & exe & """", 0, False
