<#
.SYNOPSIS
    Compiles NŌR into a standalone single-file Windows executable (NOR.exe).
.DESCRIPTION
    Requires .NET 8 SDK. Generates self-contained win-x64 binary in the release/ folder.
#>

$ErrorActionPreference = "Stop"

Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "       NŌR Live Wallpaper - Windows Build Tool         " -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Host "[ERROR] .NET 8 SDK is not installed or not in PATH." -ForegroundColor Red
    Write-Host "Download .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

$outputDir = Join-Path (Split-Path -Parent $scriptDir) "release\win-x64"

Write-Host "[*] Compiling self-contained single-file binary..." -ForegroundColor Green
Write-Host "[*] Target: Windows x64 (Release)" -ForegroundColor Gray
Write-Host "[*] Output directory: $outputDir" -ForegroundColor Gray
Write-Host ""

dotnet publish NOR.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $outputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=======================================================" -ForegroundColor Green
    Write-Host "[SUCCESS] NŌR for Windows compiled successfully!" -ForegroundColor Green
    $exePath = Join-Path $outputDir "NOR.exe"
    if (Test-Path $exePath) {
        $fileSize = (Get-Item $exePath).Length / 1MB
        Write-Host ("Executable: {0} ({1:N2} MB)" -f $exePath, $fileSize) -ForegroundColor Yellow
    }
    Write-Host "=======================================================" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[ERROR] Compilation failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

