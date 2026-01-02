param(
    [string]$FolderName
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$venvPath = Join-Path $scriptDir "python\venv\Scripts\Activate.ps1"
$pythonScript = Join-Path $scriptDir "python\visualize_backtest.py"

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Backtest Visualization" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Activate virtual environment
if (Test-Path $venvPath) {
    Write-Host "Activating virtual environment..." -ForegroundColor Green
    & $venvPath
} else {
    Write-Host "[WARNING] Virtual environment not found at: $venvPath" -ForegroundColor Yellow
}

# Build the python command with optional folder name argument
if ($FolderName) {
    Write-Host "Visualizing backtest: " -NoNewline
    Write-Host $FolderName -ForegroundColor Yellow
    Write-Host ""
    & python $pythonScript $FolderName
} else {
    Write-Host "Visualizing most recent backtest" -ForegroundColor Yellow
    Write-Host ""
    
    # Find the most recent result folder (recursively) and pass relative path to Python
    $resultsPath = Join-Path $scriptDir "Results"
    $mostRecentFolder = Get-ChildItem -Path $resultsPath -Directory -Recurse |
        Where-Object { 
            # Only consider folders that match the timestamp pattern (end with -YYYYMMDD-HHMMSS)
            $_.Name -match '-\d{8}-\d{6}$'
        } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    
    if ($mostRecentFolder) {
        # Get the relative path from Results directory
        $relativePath = $mostRecentFolder.FullName.Substring($resultsPath.Length + 1)
        & python $pythonScript $relativePath
    } else {
        & python $pythonScript
    }
}

$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    Write-Host ""
    Write-Host "[FAILED] Visualization failed with exit code: $exitCode" -ForegroundColor Red
    Write-Host ""
    exit $exitCode
}

# Find and open the generated HTML file
$resultsPath = Join-Path $scriptDir "Results"

if ($FolderName) {
    # Handle nested paths by converting slashes to backslashes
    $normalizedPath = $FolderName.Replace('/', '\')
    $htmlFile = Join-Path $resultsPath "$normalizedPath\report.html"
} else {
    # Find the most recent folder (recursively search all subdirectories)
    $mostRecentFolder = Get-ChildItem -Path $resultsPath -Directory -Recurse | 
        Where-Object { Test-Path (Join-Path $_.FullName "report.html") } |
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 1
    if ($mostRecentFolder) {
        $htmlFile = Join-Path $mostRecentFolder.FullName "report.html"
    }
}

if ($htmlFile -and (Test-Path $htmlFile)) {
    Write-Host ""
    Write-Host "[SUCCESS] Visualization complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Opening report: " -NoNewline
    Write-Host $htmlFile -ForegroundColor Cyan
    Write-Host ""
    
    # Open the HTML file in the default browser
    Start-Process $htmlFile
} else {
    Write-Host ""
    Write-Host "[WARNING] Could not find report.html file" -ForegroundColor Yellow
    Write-Host ""
}

exit 0
