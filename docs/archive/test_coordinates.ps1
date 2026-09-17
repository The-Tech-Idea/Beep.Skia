# Quick test script to examine coordinate behavior
Write-Host "Testing coordinate behavior..."

# Build and run a simple test
Set-Location "Beep.Skia.Sample.WinForms"
dotnet build -c Debug --no-restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful, starting application..."
    dotnet run &
    Start-Sleep 3
    
    # Check log file for coordinate assignments
    $logPath = Join-Path $env:TEMP "beepskia_render.log"
    if (Test-Path $logPath) {
        Write-Host "`n=== Coordinate Log Entries ==="
        Get-Content $logPath | Where-Object { $_ -match "PositionChange|CreateAndAdd|DragDrop" } | Select-Object -Last 10
    } else {
        Write-Host "No log file found at $logPath"
    }
} else {
    Write-Host "Build failed"
}
