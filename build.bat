@echo off
chcp 65001 >nul
cd /d "%~dp0"

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [ERROR] Khong tim thay trinh bien dich csc.exe tai: %CSC%
    exit /b 1
)

taskkill /f /im "QuinGM luv Mthu Menu.exe" /im "GM Menu.exe" /im GMMenu.exe /im MenuGame.exe /im pmt_click.exe 2>nul
ping 127.0.0.1 -n 2 >nul

if not exist "bin" mkdir bin

echo ========================================================
echo Bien dich QuinGM luv Mthu Menu...
echo ========================================================

"%CSC%" /target:winexe /out:"bin\GMMenu.exe" /platform:anycpu /optimize+ /codepage:65001 /win32icon:"app.ico" /res:app.ico,app.ico /res:static\logo.png,logo.png /res:static\favicon.ico,favicon.ico /res:static\index.html,index.html /res:static\style.css,style.css /res:static\script.js,script.js /res:static\manifest.json,manifest.json /res:static\apk_download.html,apk_download.html /res:PMT_Click.apk,PMT_Click.apk /r:System.dll,System.Windows.Forms.dll,System.Drawing.dll,System.Core.dll,System.Web.Extensions.dll src\Native\NativeApp.cs src\Native\PhonePcKeyboard.cs

if %errorLevel% neq 0 (
    echo [ERROR] Bien dich that bai!
    exit /b 1
)
echo [OK] Bien dich thanh cong!

copy /y "bin\GMMenu.exe" "QuinGM luv Mthu Menu.exe" >nul

echo.
echo ========================================================
echo [SUCCESS] HOAN TAT BIEN DICH THANH CONG!
echo ========================================================