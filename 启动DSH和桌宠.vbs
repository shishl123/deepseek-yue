Option Explicit
' Start DeepSeekRuntime together with the pet, and close the pet when DSH exits.
' ASCII-only on purpose: VBScript reads .vbs with the system ANSI code page.
Dim sh, fso, dir, pet, dsh, dsh2
Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
pet = dir & "\DeepSeekPet.exe"
dsh = "D:\Users\shish\AppData\Local\Programs\DeepSeekRuntime\DeepSeekRuntime.exe"
dsh2 = sh.ExpandEnvironmentStrings("%LOCALAPPDATA%") & "\Programs\DeepSeekRuntime\DeepSeekRuntime.exe"

If Not fso.FileExists(dsh) Then dsh = dsh2
If Not fso.FileExists(dsh) Then
    MsgBox "DeepSeekRuntime.exe was not found." & vbCrLf & dsh, 48, "DeepSeek Pet"
    WScript.Quit 1
End If
If Not fso.FileExists(pet) Then
    MsgBox "DeepSeekPet.exe was not found next to this script.", 48, "DeepSeek Pet"
    WScript.Quit 1
End If

sh.CurrentDirectory = dir

' 1) start the pet
sh.Run """" & pet & """", 0, False

' 2) start DSH and wait until it exits
sh.Run """" & dsh & """", 1, True

' 3) DSH is gone: close the pet too
sh.Run "taskkill /IM DeepSeekPet.exe /F", 0, True