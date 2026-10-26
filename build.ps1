# 重新编译 DeepSeekPet.exe（需要 .NET Framework 4.x，Windows 自带）
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $csc)) { throw '未找到 .NET Framework 4.x 的 csc.exe' }

Get-Process DeepSeekPet -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 700

$files = Get-ChildItem (Join-Path $root 'src') -Filter *.cs | ForEach-Object { $_.FullName }
& $csc /nologo /target:winexe /codepage:65001 /optimize+ /platform:anycpu `
    "/out:$root\DeepSeekPet.exe" "/win32icon:$root\assets\icon.ico" `
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Net.Http.dll `
    $files
if ($LASTEXITCODE -ne 0) { throw "编译失败，退出码 $LASTEXITCODE" }

Write-Host ("编译完成: " + (Join-Path $root 'DeepSeekPet.exe'))
