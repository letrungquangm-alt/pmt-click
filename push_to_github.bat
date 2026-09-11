@echo off
chcp 65001 >nul
cd /d "%~dp0"
title Đẩy Dự Án PMT Click Lên GitHub
echo ================================================================================
echo       ĐANG ĐẨY FILE APK VÀ DỰ ÁN LÊN GITHUB: letrungquangm-alt/pmt-click
echo ================================================================================
echo.
echo Khi cửa sổ GitHub hiện lên, bạn chỉ cần chọn "Sign in with your browser"!
"E:\Git\cmd\git.exe" add -A
"E:\Git\cmd\git.exe" commit -m "Update QuinGM Menu: Khoa Dinh Menh, Installer & Portable features"
"E:\Git\cmd\git.exe" push origin main
echo.
if %errorlevel% equ 0 (
    echo ================================================================================
    echo [THÀNH CÔNG] ĐÃ ĐẨY FILE PMT_Click.apk VÀ TOÀN BỘ DỰ ÁN LÊN GITHUB!
    echo.
    echo Link tải trực tiếp APK của bạn là:
    echo https://raw.githubusercontent.com/letrungquangm-alt/pmt-click/main/PMT_Click.apk
    echo ================================================================================
) else (
    echo [!] Có lỗi xảy ra, bạn hãy kiểm tra lại đăng nhập.
)
echo.
pause
