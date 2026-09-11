$ErrorActionPreference = "Stop"
$workspace = "e:\code\phone_pc_keyboard"
$toolsDir = Join-Path $workspace ".android_tools"

if (-not (Test-Path $toolsDir)) {
    New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null
}

$zipUrl = "https://dl.google.com/android/repository/commandlinetools-win-11076708_latest.zip"
$zipPath = Join-Path $toolsDir "cmdline-tools.zip"

if (-not (Test-Path $zipPath)) {
    Write-Host "Downloading Google Android Command Line Tools (~145MB)..."
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    (New-Object System.Net.WebClient).DownloadFile($zipUrl, $zipPath)
    Write-Host "Download complete!"
}

$latestDir = Join-Path $toolsDir "cmdline-tools\latest"
if (-not (Test-Path (Join-Path $latestDir "bin\sdkmanager.bat"))) {
    Write-Host "Extracting Command Line Tools..."
    Expand-Archive -Path $zipPath -DestinationPath $toolsDir -Force
    $cmdlineDir = Join-Path $toolsDir "cmdline-tools"
    $tempLatest = Join-Path $toolsDir "cmdline-tools_latest"
    New-Item -ItemType Directory -Path $tempLatest -Force | Out-Null
    
    Get-ChildItem -Path $cmdlineDir | Move-Item -Destination $tempLatest -Force
    Rename-Item -Path $tempLatest -NewName "latest" -Force
    Move-Item -Path (Join-Path $toolsDir "latest") -Destination $cmdlineDir -Force
    Write-Host "SDK Manager extracted to $latestDir"
}

Write-Host "Android Tools Ready!"
