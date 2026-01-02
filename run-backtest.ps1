#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run a QuantConnect LEAN backtest with the specified algorithm.

.DESCRIPTION
    This script builds the Demo project and runs a backtest with the specified algorithm name.
    No need to manually copy DLLs or edit config files!

.PARAMETER AlgorithmName
    The name of the algorithm class to run (e.g., "PairsTradingCsvData", "BuyAndHoldXOM")

.PARAMETER NoBuild
    Skip the build step (useful if you haven't changed the code)

.EXAMPLE
    .\run-backtest.ps1 PairsTradingCsvData
    
.EXAMPLE
    .\run-backtest.ps1 BuyAndHoldXOM -NoBuild
#>

param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$AlgorithmName,
    
    [Parameter(Mandatory=$false)]
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
Write-Host "Algorithm: " -NoNewline
Write-Host $AlgorithmName -ForegroundColor Yellow
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

# Step 2: Update config.json with the algorithm name
Write-Host "[2/3] Updating configuration..." -ForegroundColor Green

$config = Get-Content $configPath -Raw | ConvertFrom-Json
$originalAlgorithm = $config.'algorithm-type-name'
$config.'algorithm-type-name' = $AlgorithmName

$config | ConvertTo-Json -Depth 10 | Set-Content $configPath

Write-Host "      [OK] Config updated: $originalAlgorithm → $AlgorithmName" -ForegroundColor DarkGreen

# Step 3: Run the backtest
Write-Host "[3/3] Running backtest..." -ForegroundColor Green
Write-Host ""
Write-Host "================================================" -ForegroundColor DarkGray

try {
    & $leanLauncherPath --config $configPath
    $exitCode = $LASTEXITCODE
}
finally {
    # Restore original config (optional - comment out if you want to keep the change)
    # $config.'algorithm-type-name' = $originalAlgorithm
    # $config | ConvertTo-Json -Depth 10 | Set-Content $configPath
}

Write-Host "================================================" -ForegroundColor DarkGray
Write-Host ""

if ($exitCode -eq 0) {
    Write-Host "[SUCCESS] Backtest completed successfully!" -ForegroundColor Green
} else {
    Write-Host "[FAILED] Backtest failed with exit code: $exitCode" -ForegroundColor Red
}

Write-Host ""
Write-Host "Results location: .\Lean\Launcher\bin\Release\Results" -ForegroundColor Cyan
Write-Host ""

exit $exitCode
