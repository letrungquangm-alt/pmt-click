@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
cd /d "%~dp0"
title Thiet Lap Domain quiniumthu.qd.je Cho PMT Click

:MENU
cls
echo ================================================================================
echo        THIẾT LẬP DOMAIN CHÍNH THỨC: quiniumthu.qd.je CHO PMT CLICK
echo ================================================================================
echo.
echo  Domain chính:        https://quiniumthu.qd.je
echo  Link tải APK:        https://quiniumthu.qd.je/apk
echo  Port máy chủ nội bộ: 127.0.0.1:5000
echo.
echo --------------------------------------------------------------------------------
if exist "cloudflare_token.txt" (
    echo  [Trạng thái Token]: Đã tìm thấy file cloudflare_token.txt
) else (
    echo  [Trạng thái Token]: Chưa có file cloudflare_token.txt
)
echo --------------------------------------------------------------------------------
echo.
echo  [1] Nhập / Dán Cloudflare Tunnel Token cho quiniumthu.qd.je
echo  [2] Mở file cloudflare_token.txt bằng Notepad để chỉnh sửa
echo  [3] Mở trang Cloudflare Zero Trust để tạo / lấy Token
echo  [4] Chạy thử nghiệm kết nối Cloudflare Tunnel với domain ngay
echo  [5] Thoát
echo.
set /p opt="Chọn thao tác (1-5): "

if "%opt%"=="1" goto SET_TOKEN
if "%opt%"=="2" goto OPEN_TXT
if "%opt%"=="3" goto OPEN_WEB
if "%opt%"=="4" goto TEST_TUNNEL
if "%opt%"=="5" exit /b 0
goto MENU

:SET_TOKEN
cls
echo ================================================================================
echo NHẬP CLOUDFLARE TUNNEL TOKEN CHO DOMAIN: quiniumthu.qd.je
echo ================================================================================
echo.
echo Hướng dẫn:
echo 1. Trên Cloudflare Zero Trust (one.dash.cloudflare.com) -> Networks -> Tunnels
echo 2. Chọn Tunnel trỏ về quiniumthu.qd.je -> copy chuỗi Token (eyJh...)
echo.
set /p userToken="Dán chuỗi Token vào đây rồi ấn Enter: "
if "%userToken%"=="" (
    echo [Cảnh báo] Token trống, quay lại menu...
    pause
    goto MENU
)
echo %userToken%> "cloudflare_token.txt"
echo.
echo [OK] Đã lưu Token thành công vào file cloudflare_token.txt!
echo Khi mở PMT Click trong app, dịch vụ sẽ tự động kích hoạt domain quiniumthu.qd.je.
pause
goto MENU

:OPEN_TXT
if not exist "cloudflare_token.txt" (
    echo # Dan token vao day> "cloudflare_token.txt"
)
start notepad.exe "cloudflare_token.txt"
goto MENU

:OPEN_WEB
set TARGET_URL=https://one.dash.cloudflare.com
if exist "%~d0\Browsers\Chrome\chơi_chrome.bat" (
    start "" /b "%~d0\Browsers\Chrome\chơi_chrome.bat" "%TARGET_URL%"
    goto MENU
)
if exist "%~d0\Browsers\Chrome\Chrome.bat" (
    start "" /b "%~d0\Browsers\Chrome\Chrome.bat" "%TARGET_URL%"
    goto MENU
)
if exist "%~d0\Browsers\CocCoc\chơi_coccoc.bat" (
    start "" /b "%~d0\Browsers\CocCoc\chơi_coccoc.bat" "%TARGET_URL%"
    goto MENU
)
if exist "%~d0\Browsers\CocCoc\CocCoc.bat" (
    start "" /b "%~d0\Browsers\CocCoc\CocCoc.bat" "%TARGET_URL%"
    goto MENU
)
if exist "%~d0\Browsers\Edge\chơi_edge.bat" (
    start "" /b "%~d0\Browsers\Edge\chơi_edge.bat" "%TARGET_URL%"
    goto MENU
)
start "" "%TARGET_URL%"
goto MENU

:TEST_TUNNEL
cls
echo ================================================================================
echo KIỂM TRA CHẠY THỬ NGHIỆM CLOUDFLARED VỚI quiniumthu.qd.je
echo ================================================================================
echo.
if not exist "cloudflared.exe" (
    echo [Lỗi] Không tìm thấy cloudflared.exe tại thư mục hiện tại!
    pause
    goto MENU
)

set TOKEN=
for /f "usebackq delims=" %%a in ("cloudflare_token.txt") do (
    set LINE=%%a
    if not "!LINE:~0,1!"=="#" (
        if not defined TOKEN set TOKEN=%%a
    )
)

if defined TOKEN (
    echo Đang chạy Cloudflare Tunnel với Token đã cấu hình...
    cloudflared.exe tunnel run --token %TOKEN%
) else (
    echo Chưa có Token. Đang chạy Quick Tunnel kết nối đến port 5000...
    cloudflared.exe tunnel --url http://127.0.0.1:5000 --no-autoupdate
)
pause
goto MENU
