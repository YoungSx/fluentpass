@echo off
setlocal
cd /d "%~dp0"

echo ========================================================
echo   KeePass Fluent UI - One-Click Build ^& Deploy (Windows)
echo ========================================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" -Install -Run

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Build or deployment failed with exit code %ERRORLEVEL%.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [DONE] Build, deploy, and launch completed successfully!
pause
