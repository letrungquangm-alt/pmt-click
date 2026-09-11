$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$jdkDir = Join-Path $root ".jdk"
$sdkDir = Join-Path $root ".android_sdk"

Write-Host "============================================================"
Write-Host " COMPILING NATIVE PMT CLICK APK (AUTOMATED)"
Write-Host "============================================================"

# Set up Java Environment
$javaBin = Join-Path $jdkDir "bin"
$env:JAVA_HOME = $jdkDir
$env:PATH = "$javaBin;$env:PATH"
Write-Host "Java ready at: $javaBin"

# Pre-accept All Android SDK Licenses
$licensesDir = Join-Path $sdkDir "licenses"
if (-not (Test-Path $licensesDir)) { New-Item -ItemType Directory -Path $licensesDir -Force | Out-Null }

$license1 = "24333f8a63b682569279e4470f1e9056f6713b79`n8933bad161af9b7b11a9ee66975c8f9322fb396d`nd56f5187479451eabf01fb78af6dfcb131a6481e"
Set-Content -Path (Join-Path $licensesDir "android-sdk-license") -Value $license1 -Force

$license2 = "84831b9409646d418e84ac9530999430d379c61e"
Set-Content -Path (Join-Path $licensesDir "android-sdk-preview-license") -Value $license2 -Force

# Install build-tools & platform
$cmdlineDir = Join-Path $sdkDir "cmdline-tools"
$latestDir = Join-Path $cmdlineDir "latest"
$sdkManager = Join-Path $latestDir "bin\sdkmanager.bat"

$androidJar = Join-Path $sdkDir "platforms\android-33\android.jar"
$buildToolsDir = Join-Path $sdkDir "build-tools\33.0.2"

if (-not (Test-Path $androidJar) -or -not (Test-Path $buildToolsDir)) {
    Write-Host "Downloading and installing Android Platform 33 & Build-Tools 33.0.2..."
    $env:ANDROID_HOME = $sdkDir
    $env:ANDROID_SDK_ROOT = $sdkDir
    
    & $sdkManager --sdk_root=$sdkDir "platforms;android-33" "build-tools;33.0.2"
}

$aapt2 = Join-Path $buildToolsDir "aapt2.exe"
$d8 = Join-Path $buildToolsDir "d8.bat"
$apksigner = Join-Path $buildToolsDir "apksigner.bat"
$zipalign = Join-Path $buildToolsDir "zipalign.exe"

Write-Host "AAPT2: $aapt2"
Write-Host "D8: $d8"
Write-Host "Android.jar: $androidJar"

# Direct Compilation of PMT Click APK
Write-Host "Compiling PMT Click Android project..."

$srcDir = Join-Path $root "pmt_click_android\app\src\main"
$manifest = Join-Path $srcDir "AndroidManifest.xml"
$resDir = Join-Path $srcDir "res"
$javaSrc = Join-Path $srcDir "java"
$assetsDir = Join-Path $srcDir "assets"

# Sync ALL latest static UI assets into Android assets root AND static subfolder
$staticSrc = Join-Path $root "static"
if (Test-Path $assetsDir) { Remove-Item $assetsDir -Recurse -Force }
New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null
Copy-Item -Path "$staticSrc\*" -Destination $assetsDir -Exclude "*.apk" -Recurse -Force

$targetStatic = Join-Path $assetsDir "static"
New-Item -ItemType Directory -Path $targetStatic -Force | Out-Null
Copy-Item -Path "$staticSrc\*" -Destination $targetStatic -Exclude "*.apk" -Recurse -Force

$buildDir = Join-Path $root ".build_apk"
if (Test-Path $buildDir) { Remove-Item $buildDir -Recurse -Force }
New-Item -ItemType Directory -Path $buildDir -Force | Out-Null

$genDir = Join-Path $buildDir "gen"
$binDir = Join-Path $buildDir "bin"
New-Item -ItemType Directory -Path $genDir -Force | Out-Null
New-Item -ItemType Directory -Path $binDir -Force | Out-Null

# Step 4a: Compile Resources with aapt2
Write-Host " - Step 1: Compiling resources..."
$compiledRes = Join-Path $buildDir "res.zip"
& $aapt2 compile --dir $resDir -o $compiledRes

# Step 4b: Link Resources, Assets & Generate R.java
Write-Host " - Step 2: Linking resources, assets and generating R.java..."
$unalignedApk = Join-Path $buildDir "unaligned.apk"
& $aapt2 link -I $androidJar $compiledRes -A $assetsDir --manifest $manifest -o $unalignedApk --java $genDir --auto-add-overlay --min-sdk-version 21 --target-sdk-version 33 --version-code 1 --version-name "2.0"

# Step 4c: Compile Java Source Code with javac (Target Java 8 for 100% Android runtime compatibility)
Write-Host " - Step 3: Compiling Java source files (Java 8 byte-code)..."
$allJavaFiles = Get-ChildItem -Path $genDir, $javaSrc -Filter "*.java" -Recurse | Select-Object -ExpandProperty FullName
$javacExe = Join-Path $javaBin "javac.exe"
& $javacExe -source 1.8 -target 1.8 -encoding UTF-8 -cp $androidJar -d $binDir $allJavaFiles

# Step 4d: Convert bytecode to Dalvik DEX (d8)
Write-Host " - Step 4: Converting classes to Dalvik DEX (min-api 21)..."
$allClassFiles = Get-ChildItem -Path $binDir -Filter "*.class" -Recurse | Select-Object -ExpandProperty FullName
$d8Jar = Join-Path $buildToolsDir "lib\d8.jar"
$javaExe = Join-Path $javaBin "java.exe"
& $javaExe -cp $d8Jar com.android.tools.r8.D8 --min-api 21 --output $buildDir --lib $androidJar $allClassFiles

# Step 4e: Add classes.dex into unaligned.apk
Write-Host " - Step 5: Packaging classes.dex..."
$jarExe = Join-Path $javaBin "jar.exe"
Push-Location $buildDir
& $jarExe -uf $unalignedApk "classes.dex"
Pop-Location

# Step 4f: Zipalign APK
Write-Host " - Step 6: Aligning APK..."
$alignedApk = Join-Path $buildDir "aligned.apk"
& $zipalign -v -p 4 $unalignedApk $alignedApk

# Step 4g: Generate Keystore & Sign APK (apksigner with dual v1 + v2 signatures)
Write-Host " - Step 7: Signing APK with official debug certificate (v1 + v2 dual scheme)..."
$keystore = Join-Path $buildDir "debug.keystore"
$keytool = Join-Path $javaBin "keytool.exe"
& $keytool -genkey -v -keystore $keystore -storepass android -alias androiddebugkey -keypass android -keyalg RSA -keysize 2048 -validity 10000 -dname "CN=PMT Click, OU=PMT, O=PMT, L=HCM, ST=HCM, C=VN"

$finalApk = Join-Path $root "PMT_Click.apk"
$apksignerJar = Join-Path $buildToolsDir "lib\apksigner.jar"
& $javaExe -jar $apksignerJar sign --ks $keystore --ks-pass "pass:android" --key-pass "pass:android" --min-sdk-version 21 --v1-signing-enabled true --v2-signing-enabled true --out $finalApk $alignedApk

Write-Host "============================================================"
Write-Host " PMT_CLICK.APK COMPILED AND SIGNED SUCCESSFULLY!"
Write-Host " Target APK: $finalApk"
Write-Host "============================================================"
