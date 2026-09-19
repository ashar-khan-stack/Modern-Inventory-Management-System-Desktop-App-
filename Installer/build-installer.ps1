# Modern Inventory Management System - Windows Installer & Distribution Builder
# Requires: .NET 8 SDK
# Optional for Setup.exe: Inno Setup 6 (ISCC.exe)

[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipInstallerCompile = $false
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionDir = Split-Path -Parent $ScriptDir
$PublishDir = Join-Path $SolutionDir "publish\win-x64"
$OutputDir = Join-Path $ScriptDir "Output"
$IssFile = Join-Path $ScriptDir "ModernInventory.iss"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host " Modern Inventory - VIP Windows Distribution Builder" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "Solution Directory: $SolutionDir"
Write-Host "Publish Directory:  $PublishDir"
Write-Host "Output Directory:   $OutputDir"
Write-Host "Configuration:      $Configuration"
Write-Host "Runtime:            $Runtime (Self-Contained)"
Write-Host ""

# 1. Clean previous build artifacts
if (Test-Path $PublishDir) {
    Write-Host "[1/4] Cleaning previous publish directory..." -ForegroundColor Yellow
    Remove-Item -Path $PublishDir -Recurse -Force
}
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# 2. Publish self-contained .NET 8 WPF application
Write-Host "[2/4] Publishing self-contained Windows desktop application..." -ForegroundColor Green
$ProjectFile = Join-Path $SolutionDir "ModernInventory.Desktop\ModernInventory.Desktop.csproj"

$publishArgs = @(
    "publish",
    $ProjectFile,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-p:PublishSingleFile=false",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-o", $PublishDir
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Successfully published self-contained application to $PublishDir" -ForegroundColor Green

# 3. Create Portable Zip Package
Write-Host "[3/4] Creating portable distribution archive..." -ForegroundColor Green
$ZipFile = Join-Path $OutputDir "ModernInventory_v1.0.0_Portable.zip"
if (Test-Path $ZipFile) {
    Remove-Item $ZipFile -Force
}
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipFile -CompressionLevel Optimal
Write-Host "Portable package created: $ZipFile" -ForegroundColor Green

# 4. Locate Inno Setup Compiler (ISCC.exe) and compile installer
Write-Host "[4/4] Searching for Inno Setup Compiler (ISCC.exe)..." -ForegroundColor Green

$isccCandidates = @(
    "ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$isccPath = $null
foreach ($candidate in $isccCandidates) {
    if (Get-Command $candidate -ErrorAction SilentlyContinue) {
        $isccPath = (Get-Command $candidate).Source
        break
    }
    if (Test-Path $candidate) {
        $isccPath = $candidate
        break
    }
}

if ($isccPath -and (-not $SkipInstallerCompile)) {
    Write-Host "Found Inno Setup at: $isccPath" -ForegroundColor Cyan
    Write-Host "Compiling Windows Setup installer..." -ForegroundColor Green
    
    Push-Location $ScriptDir
    try {
        & "$isccPath" "$IssFile"
        if ($LASTEXITCODE -eq 0) {
            $installerExe = Join-Path $OutputDir "ModernInventory_Setup_v1.0.0.exe"
            Write-Host ""
            Write-Host "========================================================" -ForegroundColor Green
            Write-Host " BUILD COMPLETE: Windows Installer Generated!" -ForegroundColor Green
            Write-Host " Installer: $installerExe" -ForegroundColor Green
            if (Test-Path $installerExe) {
                $hash = (Get-FileHash -Path $installerExe -Algorithm SHA256).Hash
                Write-Host " SHA256:   $hash" -ForegroundColor Gray
            }
            Write-Host " Portable:  $ZipFile" -ForegroundColor Green
            Write-Host "========================================================" -ForegroundColor Green
        } else {
            Write-Warning "ISCC compilation returned code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
} else {
    Write-Host ""
    Write-Host "Note: Inno Setup (ISCC.exe) was not found on PATH or default directories." -ForegroundColor Yellow
    Write-Host "To produce the .exe Setup wizard, install Inno Setup 6 via:" -ForegroundColor Yellow
    Write-Host "  winget install JRSoftware.InnoSetup" -ForegroundColor White
    Write-Host "Then re-run this script, or open 'ModernInventory.iss' inside Inno Setup IDE." -ForegroundColor White
    Write-Host ""
    Write-Host "Your fully self-contained portable package is ready at:" -ForegroundColor Green
    Write-Host "  $ZipFile" -ForegroundColor Green
}
