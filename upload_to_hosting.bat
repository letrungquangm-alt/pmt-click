@echo off
chcp 65001 >nul
title Tự Động Đồng Bộ Toàn Bộ File Lên Hosting quiniumthu.qd.je
cd /d "%~dp0"

echo ================================================================================
echo    CÔNG CỤ TỰ ĐỘNG TẢI TOÀN BỘ FILE LÊN HOSTING (quiniumthu.qd.je)
echo ================================================================================
echo.
echo Thư mục nguồn : e:\code\code\File Manager Domain
echo Máy chủ đích  : ftp://ftpupload.net/htdocs/
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command "& {
    $ErrorActionPreference = 'Stop'
    $ftpHost = 'ftpupload.net'
    $ftpUser = 'alalr_42888185'
    $configFile = 'ftp_config.txt'
    $sourceDir = 'e:\code\code\File Manager Domain'

    # Tự động cập nhật PMT_Click.apk mới nhất từ thư mục gốc vào File Manager Domain
    $rootApk = 'e:\code\code\PMT_Click.apk'
    $destApk = Join-Path $sourceDir 'PMT_Click.apk'
    if (Test-Path $rootApk) {
        if (-not (Test-Path $destApk) -or ((Get-Item $rootApk).LastWriteTime -gt (Get-Item $destApk).LastWriteTime)) {
            Copy-Item $rootApk $destApk -Force
            Write-Host '   [i] Đã đồng bộ file PMT_Click.apk mới nhất vào File Manager Domain' -ForegroundColor Cyan
        }
    }

    # Đọc mật khẩu
    $ftpPass = ''
    if (Test-Path $configFile) {
        $ftpPass = (Get-Content $configFile -Raw).Trim()
    }

    if ([string]::IsNullOrWhiteSpace($ftpPass)) {
        Write-Host 'Vui lòng nhập mật khẩu tài khoản hosting (alalr_42888185) để lưu lại cho các lần sau:' -ForegroundColor Yellow
        $ftpPass = Read-Host -Prompt 'Mật khẩu FTP'
        if ([string]::IsNullOrWhiteSpace($ftpPass)) {
            Write-Host '[!] Chưa nhập mật khẩu, hủy thao tác.' -ForegroundColor Red
            exit 1
        }
        $ftpPass | Out-File -FilePath $configFile -Encoding utf8
        Write-Host '[OK] Đã lưu mật khẩu vào ftp_config.txt (chỉ cần nhập 1 lần duy nhất)!' -ForegroundColor Green
    }

    if (-not (Test-Path $sourceDir)) {
        New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null
    }

    $files = Get-ChildItem -Path $sourceDir -File

    if ($files.Count -eq 0) {
        Write-Host '[!] Thư mục File Manager Domain đang trống, không có file nào để tải lên.' -ForegroundColor Yellow
        exit 0
    }

    Write-Host ('`nBắt đầu tải lên ' + $files.Count + ' file từ thư mục [File Manager Domain]...') -ForegroundColor White
    $successCount = 0

    foreach ($f in $files) {
        $remoteUri = 'ftp://' + $ftpHost + '/htdocs/' + $f.Name
        Write-Host ('-> Đang tải: ' + $f.Name + ' (' + [math]::Round($f.Length / 1KB, 1) + ' KB)... ') -NoNewline

        try {
            $req = [System.Net.FtpWebRequest]::Create($remoteUri)
            $req.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPass)
            $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
            $req.UseBinary = $true
            $req.KeepAlive = $false
            $req.Timeout = 15000

            $fileBytes = [System.IO.File]::ReadAllBytes($f.FullName)
            $req.ContentLength = $fileBytes.Length

            $stream = $req.GetRequestStream()
            $stream.Write($fileBytes, 0, $fileBytes.Length)
            $stream.Close()
            $resp = $req.GetResponse()
            $resp.Close()

            Write-Host '[XONG]' -ForegroundColor Green
            $successCount++
        }
        catch {
            Write-Host ('[LỖI: ' + $_.Exception.Message + ']') -ForegroundColor Red
        }
    }

    Write-Host '`n================================================================================'
    if ($successCount -eq $files.Count) {
        Write-Host '[THÀNH CÔNG] ĐÃ ĐỒNG BỘ TOÀN BỘ FILE LÊN WEBSITE: https://quiniumthu.qd.je !' -ForegroundColor Green
    } else {
        Write-Host ('[HOÀN TẤT] Đã tải thành công ' + $successCount + '/' + $files.Count + ' file.') -ForegroundColor Yellow
        Write-Host 'Nếu báo lỗi mật khẩu, bạn chỉ cần mở file ftp_config.txt để sửa lại mật khẩu đúng.' -ForegroundColor Gray
    }
    Write-Host '================================================================================`n'
}"

echo.
pause
