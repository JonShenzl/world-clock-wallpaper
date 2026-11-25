# Copyright (c) 2026 Jon.Shen (Jon.Shen@Foxmail.com)
# 自由复制、修改；禁止商业使用；整合或再分发需注明出处。
# 编译世界时钟动态壁纸，输出 dist\WorldClockWallpaper.exe
param(
    [string]$OutputDir = (Join-Path $PSScriptRoot 'dist')
)

$ErrorActionPreference = 'Stop'
$fx = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $fx 'csc.exe'
$wpf = Join-Path $fx 'WPF'

if (-not (Test-Path $csc)) {
    throw '未找到 .NET Framework 4.x 编译器 csc.exe，请确认系统为 Windows 10/11'
}

New-Item -ItemType Directory -Force $OutputDir | Out-Null

& $csc /nologo /codepage:65001 /target:winexe /optimize+ `
    /out:"$OutputDir\WorldClockWallpaper.exe" `
    /win32icon:"$PSScriptRoot\src\clock.ico" `
    /r:"$wpf\PresentationFramework.dll" `
    /r:"$wpf\PresentationCore.dll" `
    /r:"$wpf\WindowsBase.dll" `
    /r:"$fx\System.Xaml.dll" `
    "$PSScriptRoot\src\WorldClock.cs"

if ($LASTEXITCODE -ne 0) {
    throw "编译失败（csc 退出码 $LASTEXITCODE）"
}

Write-Host "已生成：$OutputDir\WorldClockWallpaper.exe"
