@echo off
setlocal
cd /d "%~dp0"
set "PYTHON_EXE="
for /d %%D in ("%LOCALAPPDATA%\hermes\tools\python-*") do if exist "%%~D\python.exe" set "PYTHON_EXE=%%~D\python.exe"
if defined PYTHON_EXE (
  "%PYTHON_EXE%" "%~dp0tools\local_stack.py"
) else (
  python "%~dp0tools\local_stack.py"
)
if errorlevel 1 pause
