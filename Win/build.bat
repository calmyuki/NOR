@echo off
setlocal enabledelayedexpansion

echo =======================================================
echo           NŌR Live Wallpaper - Windows Build Tool
echo =======================================================
echo.

where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo [ERROR] .NET 8 SDK is not installed or not in PATH.
    echo Please download and install .NET 8 SDK from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo [*] Building NŌR Live Wallpaper single-file executable...
echo [*] Target Runtime: win-x64
echo [*] Configuration: Release
echo.

cd /d "%~dp0"
dotnet publish NOR.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "../release/win-x64"

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Build failed! Check the error log above.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo =======================================================
echo [SUCCESS] NŌR for Windows compiled successfully!
echo Executable: release\win-x64\NOR.exe
echo =======================================================
echo.
pause

