# Clean up all contents of the Results folder

$resultsPath = Join-Path $PSScriptRoot "Results"

if (Test-Path $resultsPath) {
    Write-Host "Cleaning Results folder..." -ForegroundColor Yellow
    
    Get-ChildItem -Path $resultsPath -Recurse | Remove-Item -Force -Recurse
    
    Write-Host "Results folder cleaned successfully!" -ForegroundColor Green
} else {
    Write-Host "Results folder does not exist." -ForegroundColor Red
}
