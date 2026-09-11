$ErrorActionPreference = "Stop"
$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $PSScriptRoot

$CSC = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $CSC)) {
    Write-Error "Khong tim thay csc.exe tai: $CSC"
    exit 1
}

# Kill running processes
Get-Process | Where-Object { $_.ProcessName -like "*Quin*" -or $_.ProcessName -like "*Cai Dat*" -or $_.ProcessName -like "*Installer*" -or $_.ProcessName -like "*GMMenu*" -or $_.ProcessName -like "*Uninstall*" -or $_.ProcessName -like "*pmt_click*" } | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 800

if (-not (Test-Path "bin")) { New-Item -ItemType Directory -Path "bin" | Out-Null }

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "1. Bien dich QuinGM luv Mthu Menu (App Chinh)..." -ForegroundColor Yellow
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
Write-Host "2. Bien dich Trinh Go Cai Dat (Uninstaller)..." -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan

$uninstallerArgs = @(
    "/target:winexe",
    "/out:bin\Uninstall.exe",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/win32icon:app.ico",
    "/res:app.ico,app.ico",
    "/r:System.dll,System.Windows.Forms.dll,System.Drawing.dll,System.Core.dll",
    "src\Native\UninstallerApp.cs"
)

& $CSC $uninstallerArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Bien dich Trinh Go Cai Dat that bai!"
    exit 1
}

$uninstallerBytes = [byte[]]@(0x47, 0xE1, 0xBB, 0xA1, 0x20, 0x43, 0xC3, 0xA0, 0x69, 0x20, 0xC4, 0x90, 0xE1, 0xBA, 0xB7, 0x74, 0x20, 0x51, 0x75, 0x69, 0x6E, 0x47, 0x4D, 0x2E, 0x65, 0x78, 0x65)
$uninstallerName = [System.Text.Encoding]::UTF8.GetString($uninstallerBytes)

Copy-Item "bin\Uninstall.exe" $uninstallerName -Force
Write-Host "[OK] Bien dich Trinh Go Cai Dat thanh cong!" -ForegroundColor Green

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "3. Bien dich Trinh Cai Dat (Installer Chua Ca 2 Payload)..." -ForegroundColor Yellow
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
    "/res:bin\Uninstall.exe,uninstall_payload.bin",
    "/r:System.dll,System.Windows.Forms.dll,System.Drawing.dll,System.Core.dll",
    "src\Native\InstallerApp.cs"
)

& $CSC $installerArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Bien dich Trinh Cai Dat that bai!"
    exit 1
}

$installerBytes = [byte[]]@(0x43, 0xC3, 0xA0, 0x69, 0x20, 0xC4, 0x90, 0xE1, 0xBA, 0xB7, 0x74, 0x20, 0x51, 0x75, 0x69, 0x6E, 0x47, 0x4D, 0x20, 0x4D, 0x65, 0x6E, 0x75, 0x2E, 0x65, 0x78, 0x65)
$installerName = [System.Text.Encoding]::UTF8.GetString($installerBytes)

Copy-Item "bin\Installer.exe" $installerName -Force
Copy-Item "bin\Installer.exe" (Join-Path "E:\" $installerName) -Force

# Don dep sach se tat ca file trung lap hoac loi font
Get-ChildItem -Path "E:\" | Where-Object { ($_.Name -like "*Cai Dat QuinGM*" -or $_.Name -like "*C*i*t*QuinGM*") -and $_.Name -ne $installerName } | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path $PSScriptRoot | Where-Object { ($_.Name -like "*Cai Dat QuinGM*" -or $_.Name -like "*C*i*t*QuinGM*") -and $_.Name -ne $installerName -and $_.Name -ne $uninstallerName } | Remove-Item -Force -ErrorAction SilentlyContinue

# Don dep file test neu co
if (Test-Path "test_install.ps1") { Remove-Item "test_install.ps1" -Force -ErrorAction SilentlyContinue }

# Don dep folder QuinGM cu tren E:\ nếu có file lỗi tên
if (Test-Path "E:\QuinGM") {
    Get-ChildItem -Path "E:\QuinGM" | Where-Object { $_.Name -like "*G*C*i*t*QuinGM*" -and $_.Name -ne $uninstallerName } | Remove-Item -Force -ErrorAction SilentlyContinue
    Copy-Item "bin\Uninstall.exe" (Join-Path "E:\QuinGM" $uninstallerName) -Force
}

Write-Host "[OK] Bien dich Trinh Cai Dat thanh cong!" -ForegroundColor Green

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "[SUCCESS] HOAN TAT BIEN DICH THANH CONG TOAN BO HE THONG:" -ForegroundColor Green
Write-Host " -> E:\Cài Đặt QuinGM Menu.exe (Trinh cai dat tu dong quet o dia, giai nen app & uninstaller & tag an)" -ForegroundColor Magenta
Write-Host " -> E:\QuinGM luv Mthu Menu.exe" -ForegroundColor Magenta
Write-Host "========================================================" -ForegroundColor Cyan
