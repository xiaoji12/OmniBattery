@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
if %errorlevel% equ 0 (
    echo [OK] Build Success: OmniBattery.exe
) else (
    echo [ERROR] Build Failed!
)
pause
