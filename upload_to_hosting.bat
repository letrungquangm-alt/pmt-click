@echo off
chcp 65001 >nul
cd /d "%~dp0"
title Đồng Bộ File Lên Hosting quiniumthu.qd.je
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\sync_ftp.ps1"
echo.
pause
