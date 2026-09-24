@echo off
setlocal
cd /d "%~dp0"
if not exist "publish\CodexUsageWidget.exe" (
  echo CodexUsageWidget.exe was not found.
  echo Run BUILD.cmd first.
  pause
  exit /b 1
)
start "" "%CD%\publish\CodexUsageWidget.exe"
