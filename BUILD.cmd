@echo off
setlocal
cd /d "%~dp0"

echo [1/3] Checking .NET SDK...
where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: dotnet was not found.
  echo Install .NET 8 SDK, reopen this window, and try again.
  pause
  exit /b 1
)

dotnet --version

echo.
echo [2/3] Restoring and publishing...
if exist "publish" rmdir /s /q "publish"

dotnet restore "src\CodexUsageWidget\CodexUsageWidget.csproj"
if errorlevel 1 goto :build_error

dotnet publish "src\CodexUsageWidget\CodexUsageWidget.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "publish"
if errorlevel 1 goto :build_error

echo.
echo [3/3] BUILD SUCCESSFUL
echo EXE: %CD%\publish\CodexUsageWidget.exe
start "" explorer.exe "%CD%\publish"
echo.
pause
exit /b 0

:build_error
echo.
echo BUILD FAILED. Error code: %ERRORLEVEL%
echo Please take a screenshot of the error above and send it to ChatGPT.
echo.
pause
exit /b %ERRORLEVEL%
