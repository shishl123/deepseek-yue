Option Explicit
' Keep the pet in sync with DeepSeekRuntime:
'   DSH running  -> pet visible
'   DSH closed   -> pet closed
' Run this once (or put it in Startup). ASCII-only on purpose.
Dim sh, fso, dir, pet, wmi, d, p
Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
pet = dir & "\DeepSeekPet.exe"
Set wmi = GetObject("winmgmts:\\.\root\cimv2")

If Not fso.FileExists(pet) Then
    MsgBox "DeepSeekPet.exe was not found next to this script.", 48, "DeepSeek Pet"
    WScript.Quit 1
End If

Dim dPrev
dPrev = -1
Do
    d = Q("DeepSeekRuntime.exe")
    p = Q("DeepSeekPet.exe")
    If d = -1 Then d = dPrev

    If d > 0 Then
        If p = 0 Then
            sh.CurrentDirectory = dir
            sh.Run """" & pet & """", 0, False
            WScript.Sleep 2500
            p = 1
        End If
    ElseIf d = 0 And dPrev > 0 Then
        ' DSH was running before and just disappeared
        If p > 0 Then sh.Run "taskkill /IM DeepSeekPet.exe /F", 0, True
        WScript.Sleep 1000
        ' give it a moment, then let the next round restart if DSH comes back
    End If

    dPrev = d
    WScript.Sleep 2000
Loop

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