@echo off
chcp 65001 >nul
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
if %errorlevel% neq 0 (
    echo [ERROR] Build failed with code %errorlevel%
    pause
    exit /b %errorlevel%
)