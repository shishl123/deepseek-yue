Option Explicit
' Start DeepSeekRuntime (if needed) together with the pet,
' and close the pet when DSH exits. ASCII-only on purpose.
Dim sh, fso, dir, pet, dsh, wmi, n, i
Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
pet = dir & "\DeepSeekPet.exe"
dsh = "D:\Users\shish\AppData\Local\Programs\DeepSeekRuntime\DeepSeekRuntime.exe"
Set wmi = GetObject("winmgmts:\\.\root\cimv2")

If Not fso.FileExists(dsh) Then
    dsh = sh.ExpandEnvironmentStrings("%LOCALAPPDATA%") & "\Programs\DeepSeekRuntime\DeepSeekRuntime.exe"
End If
If Not fso.FileExists(dsh) Then
    MsgBox "DeepSeekRuntime.exe was not found.", 48, "DeepSeek Pet"
    WScript.Quit 1
End If
If Not fso.FileExists(pet) Then
    MsgBox "DeepSeekPet.exe was not found next to this script.", 48, "DeepSeek Pet"
    WScript.Quit 1
End If

sh.CurrentDirectory = dir

' 1) start DSH only when it is not running yet
If Q("DeepSeekRuntime.exe") = 0 Then
    sh.Run """" & dsh & """", 1, False
    For i = 1 To 120
        WScript.Sleep 500
        If Q("DeepSeekRuntime.exe") > 0 Then Exit For
    Next
End If

' 2) start the pet when it is not running
If Q("DeepSeekPet.exe") = 0 Then sh.Run """" & pet & """", 0, False

' 3) wait until DSH exits
Dim seen, ticks
seen = False
ticks = 0
Do
    WScript.Sleep 2000
    n = Q("DeepSeekRuntime.exe")
    ticks = ticks + 1
    If n > 0 Then seen = True
    If n = 0 And seen Then Exit Do
    If n = 0 And ticks > 30 Then Exit Do
Loop

' 4) close the pet only when we really saw DSH running
If seen Then sh.Run "taskkill /IM DeepSeekPet.exe /F", 0, True

Function Q(name)
    Dim col, c
    On Error Resume Next
    Set col = wmi.ExecQuery("SELECT ProcessId FROM Win32_Process WHERE Name='" & name & "'")
    c = col.Count
    If Err.Number <> 0 Then
        c = -1
        Err.Clear
    End If
    On Error Goto 0
    Q = c
End Function