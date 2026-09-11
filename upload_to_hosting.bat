@echo off
chcp 65001 >nul
title Tự Động Đồng Bộ Toàn Bộ File Lên Hosting quiniumthu.qd.je
cd /d "%~dp0"

echo ================================================================================
echo    CÔNG CỤ TỰ ĐỘNG TẢI TOÀN BỘ FILE LÊN HOSTING (quiniumthu.qd.je)
echo ================================================================================
echo.
echo Thư mục nguồn: e:\code\code\static
echo Máy chủ đích: ftp://ftpupload.net/htdocs/
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command "& {
    $ErrorActionPreference = 'Stop'
    $ftpHost = 'ftpupload.net'
    $ftpUser = 'alalr_42888185'
    $configFile = 'ftp_config.txt'

    # Auto sync newest PMT_Click.apk from root to static
    $rootApk = 'e:\code\code\PMT_Click.apk'
    $staticApk = 'e:\code\code\static\PMT_Click.apk'
    if (Test-Path $rootApk) {
        if (-not (Test-Path $staticApk) -or ((Get-Item $rootApk).LastWriteTime -gt (Get-Item $staticApk).LastWriteTime)) {
            Copy-Item $rootApk $staticApk -Force
            Write-Host '   [i] Đã cập nhật file PMT_Click.apk mới nhất vào thư mục static' -ForegroundColor Cyan
        }
    }

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

    $sourceDir = 'e:\code\code\static'
    $files = Get-ChildItem -Path $sourceDir -File

    Write-Host ('`nBắt đầu tải lên ' + $files.Count + ' file...') -ForegroundColor White
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
