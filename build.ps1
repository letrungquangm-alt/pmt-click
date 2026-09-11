$ErrorActionPreference = "Stop"
$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $PSScriptRoot

$CSC = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $CSC)) {
    Write-Error "Khong tim thay csc.exe tai: $CSC"
    exit 1
}

# Kill running processes
Get-Process | Where-Object { $_.ProcessName -like "*Quin*" -or $_.ProcessName -like "*Cai Dat*" -or $_.ProcessName -like "*Installer*" -or $_.ProcessName -like "*GMMenu*" -or $_.ProcessName -like "*pmt_click*" } | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 800

if (-not (Test-Path "bin")) { New-Item -ItemType Directory -Path "bin" | Out-Null }

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "1. Bien dich QuinGM luv Mthu Menu..." -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan

$menuArgs = @(
    "/target:winexe",
    "/out:bin\GMMenu.exe",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/win32icon:app.ico",
    "/res:app.ico,app.ico",
    "/res:static\logo.png,logo.png",
    "/res:static\favicon.ico,favicon.ico",
    "/res:static\index.html,index.html",
    "/res:static\style.css,style.css",
    "/res:static\script.js,script.js",
    "/res:static\manifest.json,manifest.json",
    "/res:static\apk_download.html,apk_download.html",
    "/res:PMT_Click.apk,PMT_Click.apk",
    "/r:System.dll,System.Windows.Forms.dll,System.Drawing.dll,System.Core.dll,System.Web.Extensions.dll",
    "src\Native\NativeApp.cs",
    "src\Native\PhonePcKeyboard.cs"
)

& $CSC $menuArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Bien dich Menu Game that bai!"
    exit 1
}

Copy-Item "bin\GMMenu.exe" "QuinGM luv Mthu Menu.exe" -Force
Copy-Item "bin\GMMenu.exe" "E:\QuinGM luv Mthu Menu.exe" -Force
Write-Host "[OK] Bien dich Menu Game thanh cong!" -ForegroundColor Green

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "2. Bien dich Trinh Cai Dat (Installer)..." -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan

$installerArgs = @(
    "/target:winexe",
    "/out:bin\Installer.exe",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/win32icon:app.ico",
    "/res:app.ico,app.ico",
    "/res:bin\GMMenu.exe,app_payload.bin",
    "/r:System.dll,System.Windows.Forms.dll,System.Drawing.dll,System.Core.dll",
    "src\Native\InstallerApp.cs"
)

& $CSC $installerArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Bien dich Trinh Cai Dat that bai!"
    exit 1
}

$vnBytes = [byte[]]@(0x43, 0xC3, 0xA0, 0x69, 0x20, 0xC4, 0x90, 0xE1, 0xBA, 0xB7, 0x74, 0x20, 0x51, 0x75, 0x69, 0x6E, 0x47, 0x4D, 0x20, 0x4D, 0x65, 0x6E, 0x75, 0x2E, 0x65, 0x78, 0x65)
$vnName = [System.Text.Encoding]::UTF8.GetString($vnBytes)

Copy-Item "bin\Installer.exe" $vnName -Force
Copy-Item "bin\Installer.exe" (Join-Path "E:\" $vnName) -Force

# Don dep sach se tat ca file trung lap hoac loi font
Get-ChildItem -Path "E:\" | Where-Object { ($_.Name -like "*Cai Dat QuinGM*" -or $_.Name -like "*C*i*t*QuinGM*") -and $_.Name -ne $vnName } | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path $PSScriptRoot | Where-Object { ($_.Name -like "*Cai Dat QuinGM*" -or $_.Name -like "*C*i*t*QuinGM*") -and $_.Name -ne $vnName } | Remove-Item -Force -ErrorAction SilentlyContinue

Write-Host "[OK] Bien dich Trinh Cai Dat thanh cong!" -ForegroundColor Green

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "[SUCCESS] HOAN TAT BIEN DICH THANH CONG CA 2 FILE:" -ForegroundColor Green
Write-Host " -> E:\QuinGM luv Mthu Menu.exe" -ForegroundColor Magenta
Write-Host " -> E:\Cài Đặt QuinGM Menu.exe" -ForegroundColor Magenta
Write-Host "========================================================" -ForegroundColor Cyan
