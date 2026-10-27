Option Explicit
' Silent launcher for DeepSeekPet (no console window).
' Kept ASCII-only on purpose: VBScript reads .vbs with the system ANSI code page,
' so non-ASCII text breaks on some machines (Error 800A0409 unterminated string).
Dim sh, fso, dir, exe
Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
exe = dir & "\DeepSeekPet.exe"
If Not fso.FileExists(exe) Then
    MsgBox "DeepSeekPet.exe was not found next to this script.", 48, "DeepSeek Pet"
    WScript.Quit 1
End If
sh.CurrentDirectory = dir
sh.Run """" & exe & """", 0, False