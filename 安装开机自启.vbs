Option Explicit
' Add DeepSeekPet to the current user's Startup folder (ASCII-only on purpose).
Dim sh, fso, dir, startup, lnk
Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
startup = sh.SpecialFolders("Startup")
Set lnk = sh.CreateShortcut(startup & "\DeepSeekPet.lnk")
lnk.TargetPath = dir & "\DeepSeekPet.exe"
lnk.WorkingDirectory = dir
lnk.IconLocation = dir & "\assets\icon.ico"
lnk.Description = "DeepSeek Pet - pool balance monitor"
lnk.Save
MsgBox "Added to startup:" & vbCrLf & startup & "\DeepSeekPet.lnk", 64, "DeepSeek Pet"