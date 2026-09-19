@echo off
setlocal
cd /d "%~dp0"

echo ===================================================
echo  Modern Inventory - Windows Distribution Builder
echo ===================================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-installer.ps1"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build failed with exit code %ERRORLEVEL%.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Build completed successfully.
pause
