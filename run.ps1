param(
    [string]$StrategyName,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

# Paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$configPath = Join-Path $scriptDir "config.json"
$leanLauncherPath = Join-Path $scriptDir "..\Lean\Launcher\bin\Release\QuantConnect.Lean.Launcher.exe"

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  QuantConnect LEAN Backtest Runner" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Strategy: " -NoNewline
Write-Host $StrategyName -ForegroundColor Yellow
Write-Host ""

# Step 1: Build the project (unless -NoBuild is specified)
if (-not $NoBuild) {
    Write-Host "[1/3] Building Demo project..." -ForegroundColor Green
    Push-Location $scriptDir
    try {
        $buildOutput = dotnet build -c Release --nologo --verbosity quiet 2>&1
        $buildExitCode = $LASTEXITCODE
        
        if ($buildExitCode -ne 0) {
            Write-Host ""
            Write-Host "Build failed!" -ForegroundColor Red
            Write-Host $buildOutput
            exit $buildExitCode
        }
        
        Write-Host "      [OK] Build successful" -ForegroundColor DarkGreen
    }
    finally {
        Pop-Location
    }
} else {
    Write-Host "[1/3] Skipping build (using existing DLL)" -ForegroundColor Yellow
}

# Step 2: Create timestamped results folder and update config
Write-Host "[2/3] Creating results folder and updating configuration..." -ForegroundColor Green

# Create timestamped folder name
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$resultsFolderName = "$StrategyName-$timestamp"
$resultsPath = Join-Path (Join-Path $scriptDir "Results") $resultsFolderName

# Create the directory
$null = New-Item -ItemType Directory -Path $resultsPath -Force
# Write-Host "      [OK] Created results folder: $resultsFolderName" -ForegroundColor DarkGreen

# Update config with algorithm name and results path
$config = Get-Content $configPath -Raw | ConvertFrom-Json
$originalStrategy = $config.'algorithm-type-name'
$originalResultsPath = $config.'results-destination-folder'
$config.'algorithm-type-name' = $StrategyName
$config.'results-destination-folder' = $resultsPath

$config | ConvertTo-Json -Depth 10 | Set-Content $configPath

Write-Host "      [OK] Config updated: $originalStrategy -> $StrategyName" -ForegroundColor DarkGreen

# Step 3: Run the backtest
Write-Host "[3/3] Running backtest..." -ForegroundColor Green
Write-Host ""
Write-Host "================================================" -ForegroundColor DarkGray

try {
    & $leanLauncherPath --config $configPath
    $exitCode = $LASTEXITCODE
}
finally {
    # Restore original config
    $config.'algorithm-type-name' = $originalStrategy
    $config.'results-destination-folder' = $originalResultsPath
    $config | ConvertTo-Json -Depth 10 | Set-Content $configPath
}

Write-Host "================================================" -ForegroundColor DarkGray
Write-Host ""

if ($exitCode -eq 0) {
    Write-Host "[SUCCESS] Backtest completed successfully!" -ForegroundColor Green
} else {
    Write-Host "[FAILED] Backtest failed with exit code: $exitCode" -ForegroundColor Red
}

Write-Host ""
Write-Host "Results location: " -NoNewline
Write-Host $resultsPath -ForegroundColor Cyan
Write-Host ""

exit $exitCode
