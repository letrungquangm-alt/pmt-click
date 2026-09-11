$ErrorActionPreference = 'Stop'
$ftpHost = 'ftpupload.net'
$ftpUser = 'alalr_42888185'
$baseDir = 'e:\code\code'

$configFile = Join-Path $baseDir 'ftp_config.txt'
$sourceDir = Join-Path $baseDir 'File Manager Domain'

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "   DONG BO FILE TU DONG LEN HOSTING quiniumthu.qd.je " -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Thu muc nguon : $sourceDir" -ForegroundColor Gray
Write-Host "May chu dich  : ftp://$ftpHost/htdocs/" -ForegroundColor Gray
Write-Host ""

# Dong bo file PMT_Click.apk tu thu muc goc neu co ban moi hon
$rootApk = Join-Path $baseDir 'PMT_Click.apk'
$destApk = Join-Path $sourceDir 'PMT_Click.apk'
if (Test-Path $rootApk) {
    if (-not (Test-Path $destApk) -or ((Get-Item $rootApk).LastWriteTime -gt (Get-Item $destApk).LastWriteTime)) {
        Copy-Item $rootApk $destApk -Force
        Write-Host "   [i] Da dong bo file PMT_Click.apk moi nhat vao File Manager Domain" -ForegroundColor Cyan
    }
}

# Doc mat khau tu ftp_config.txt
$ftpPass = ''
if (Test-Path $configFile) {
    $ftpPass = (Get-Content $configFile -Raw).Trim()
}

if ([string]::IsNullOrWhiteSpace($ftpPass)) {
    Write-Host "Vui long nhap mat khau hosting ($ftpUser):" -ForegroundColor Yellow
    $ftpPass = Read-Host -Prompt "Mat khau FTP"
    if ([string]::IsNullOrWhiteSpace($ftpPass)) {
        Write-Host "[!] Chua nhap mat khau, huy thao tac." -ForegroundColor Red
        exit 1
    }
    $ftpPass | Out-File -FilePath $configFile -Encoding utf8
    Write-Host "[OK] Da luu mat khau vao ftp_config.txt (chi nhap 1 lan duy nhat)!" -ForegroundColor Green
}

if (-not (Test-Path $sourceDir)) {
    New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null
}

$files = Get-ChildItem -Path $sourceDir -File

if ($files.Count -eq 0) {
    Write-Host "[!] Thu muc File Manager Domain dang trong." -ForegroundColor Yellow
    exit 0
}

Write-Host "Dang tai len $($files.Count) file tu [File Manager Domain]..." -ForegroundColor White
Write-Host ""
$successCount = 0

foreach ($f in $files) {
    $remoteUri = "ftp://$ftpHost/htdocs/$($f.Name)"
    $sizeKb = [math]::Round($f.Length / 1KB, 1)
    Write-Host "-> Dang tai: $($f.Name) ($sizeKb KB)... " -NoNewline -ForegroundColor Gray

    try {
        $req = [System.Net.FtpWebRequest]::Create($remoteUri)
        $req.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $req.UseBinary = $true
        $req.KeepAlive = $false
        $req.Timeout = 120000
        $req.ReadWriteTimeout = 120000

        $fileBytes = [System.IO.File]::ReadAllBytes($f.FullName)
        $req.ContentLength = $fileBytes.Length

        $stream = $req.GetRequestStream()
        $stream.Write($fileBytes, 0, $fileBytes.Length)
        $stream.Flush()
        $stream.Close()
        $resp = $req.GetResponse()
        $resp.Close()

        Write-Host "[XONG]" -ForegroundColor Green
        $successCount++
    }
    catch {
        Write-Host "[LOI: $($_.Exception.Message)]" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
if ($successCount -eq $files.Count) {
    Write-Host "[THANH CONG] DA DONG BO TOAN BO $successCount FILE LEN WEBSITE https://quiniumthu.qd.je !" -ForegroundColor Green
} else {
    Write-Host "[HOAN TAT] Da tai thanh cong $successCount/$($files.Count) file." -ForegroundColor Yellow
}
Write-Host "================================================================================" -ForegroundColor Cyan
