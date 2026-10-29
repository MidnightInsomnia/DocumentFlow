@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Run-DocumentFlow.ps1"
set "exitCode=%ERRORLEVEL%"
if not "%exitCode%"=="0" (
  echo.
  echo DocumentFlow failed to start.
  pause
)
exit /b %exitCode%
