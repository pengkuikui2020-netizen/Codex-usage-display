$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$proj = Join-Path $root "src\CodexUsageWidget\CodexUsageWidget.csproj"
$out = Join-Path $root "publish"

Write-Host "[1/3] Checking .NET SDK..." -ForegroundColor Cyan
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: dotnet was not found." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

dotnet --version
Write-Host "[2/3] Restoring and publishing..." -ForegroundColor Cyan
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
dotnet restore $proj
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with code $LASTEXITCODE" }
dotnet publish $proj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $out
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with code $LASTEXITCODE" }
Write-Host "[3/3] BUILD SUCCESSFUL" -ForegroundColor Green
Write-Host "EXE: $out\CodexUsageWidget.exe"
Start-Process explorer.exe $out
Read-Host "Press Enter to exit"
