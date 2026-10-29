@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Stop-DocumentFlow.ps1"
exit /b %ERRORLEVEL%
