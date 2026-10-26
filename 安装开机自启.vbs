Option Explicit
' 把桌宠加入开机自启（写入当前用户启动文件夹）
Dim sh, fso, dir, startup, lnk
Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
startup = sh.SpecialFolders("Startup")
Set lnk = sh.CreateShortcut(startup & "\DeepSeekPet.lnk")
lnk.TargetPath = dir & "\DeepSeekPet.exe"
lnk.WorkingDirectory = dir
lnk.IconLocation = dir & "\assets\icon.ico"
lnk.Description = "DeepSeek 池余额桌宠"
lnk.Save
MsgBox "已加入开机自启：" & vbCrLf & startup & "\DeepSeekPet.lnk", 64, "DeepSeek 桌宠"
