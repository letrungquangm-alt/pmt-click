@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo  PMT CLICK - MASTER BUILD ALL AUTOMATION
echo ============================================================
echo.
echo [1/2] Compiling Android Native APK...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_apk.ps1"
if errorlevel 1 (
    echo [ERROR] Failed to compile APK.
    exit /b 1
)

echo.
echo [2/2] Compiling Windows PC Executable (pmt_click.exe)...
taskkill /F /IM cloudflared.exe 2>nul
taskkill /F /IM pmt_click.exe 2>nul
ping -n 2 127.0.0.1 >nul

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /t:winexe /o+ /win32icon:app.ico /res:app.ico,app.ico /res:static\logo.png,logo.png /res:static\favicon.ico,favicon.ico /res:static\index.html,index.html /res:static\style.css,style.css /res:static\script.js,script.js /res:static\manifest.json,manifest.json /res:static\apk_download.html,apk_download.html /res:PMT_Click.apk,PMT_Click.apk /out:pmt_click.exe App.cs

if errorlevel 1 (
    echo [ERROR] Failed to compile pmt_click.exe.
    exit /b 1
)

echo.
echo ============================================================
echo  BUILD COMPLETED SUCCESSFULLY 100%!
echo  Output 1: pmt_click.exe
echo  Output 2: PMT_Click.apk
echo ============================================================
