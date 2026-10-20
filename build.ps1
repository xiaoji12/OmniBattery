$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    Write-Error "CSC compiler not found: $csc"
    exit 1
}

$srcFiles = Get-ChildItem -Path "$PSScriptRoot\src_native" -Filter "*.cs" -Recurse | Select-Object -ExpandProperty FullName
$outFile = "$PSScriptRoot\OmniBattery.exe"
$icoFile = "$PSScriptRoot\app.ico"
$manifestFile = "$PSScriptRoot\app.manifest"

$cscArgs = @(
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll",
    "/win32icon:$icoFile",
    "/win32manifest:$manifestFile",
    "/out:$outFile"
) + $srcFiles

& $csc $cscArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "[SUCCESS] OmniBattery.exe compiled successfully!" -ForegroundColor Green

# Sign with Code Signing certificate
try {
    $cert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert -ErrorAction SilentlyContinue | Select-Object -First 1

    if (-not $cert) {
        Write-Host "Creating local code signing certificate..." -ForegroundColor Cyan
        $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=OmniBattery Publisher" -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddYears(5)
    }

    if ($cert) {
        $sig1 = Set-AuthenticodeSignature -FilePath $outFile -Certificate $cert -HashAlgorithm SHA256
        Write-Host "Authenticode Signature Status: $($sig1.Status)" -ForegroundColor Green
    }
} catch {
    Write-Host "Warning: Code signing skipped: $_" -ForegroundColor Yellow
}

# Deploy to Desktop
$desktopPath = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Desktop)
if (Test-Path $desktopPath) {
    # 1. Direct executable on Desktop
    $desktopExe = Join-Path $desktopPath "OmniBattery.exe"
    Copy-Item -Path $outFile -Destination $desktopExe -Force
    Write-Host "[DEPLOY] Copied OmniBattery.exe to Desktop: $desktopExe" -ForegroundColor Cyan

    # 2. Complete release folder on Desktop
    $desktopFolder = Join-Path $desktopPath "OmniBattery"
    if (-not (Test-Path $desktopFolder)) {
        New-Item -ItemType Directory -Path $desktopFolder -Force | Out-Null
    }
    Copy-Item -Path $outFile -Destination (Join-Path $desktopFolder "OmniBattery.exe") -Force
    Write-Host "[DEPLOY] Created standalone release pack: $desktopFolder" -ForegroundColor Cyan
}
