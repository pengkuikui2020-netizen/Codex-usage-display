$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$exe = Join-Path $root "publish\CodexUsageWidget.exe"
if (-not (Test-Path $exe)) {
    Write-Host "CodexUsageWidget.exe was not found. Run BUILD.cmd first." -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}
Start-Process $exe
