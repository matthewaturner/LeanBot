param(
    [string]$StrategyName,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

# Paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$globalConfigPath = Join-Path $scriptDir "config.json"
$strategiesBaseDir = Join-Path $scriptDir "Strategies"
$leanLauncherPath = Join-Path $scriptDir "..\Lean\Launcher\bin\Release\QuantConnect.Lean.Launcher.exe"

# Function to find strategy folder (supports nested paths)
function Find-StrategyFolder {
    param($BasePath, $StrategyName)
    
    # First try direct path (handles nested paths like "EpChan/QuantitativeTrading/Ex3_4")
    $directPath = Join-Path $BasePath $StrategyName.Replace('/', '\')
    if ((Test-Path $directPath) -and (Test-Path (Join-Path $directPath "Strategy.cs"))) {
        return $directPath
    }
    
    # Search recursively for matching folder name
    Get-ChildItem -Path $BasePath -Directory -Recurse -ErrorAction SilentlyContinue | Where-Object {
        $_.Name -eq $StrategyName -and (Test-Path (Join-Path $_.FullName "Strategy.cs"))
    } | Select-Object -First 1 -ExpandProperty FullName
}

$strategyDir = Find-StrategyFolder -BasePath $strategiesBaseDir -StrategyName $StrategyName
$strategyConfigPath = if ($strategyDir) { Join-Path $strategyDir "config.json" } else { $null }

# Validate strategy folder exists
if (-not $strategyDir) {
    Write-Host ""
    Write-Host "Error: Strategy not found: $StrategyName" -ForegroundColor Red
    Write-Host "Available strategies:" -ForegroundColor Yellow
    
    function List-Strategies {
        param($BasePath, $Prefix = "")
        Get-ChildItem -Path $BasePath -Directory -ErrorAction SilentlyContinue | ForEach-Object {
            $strategyFile = Join-Path $_.FullName "Strategy.cs"
            if (Test-Path $strategyFile) {
                Write-Host "  - $Prefix$($_.Name)" -ForegroundColor Cyan
            }
            List-Strategies -BasePath $_.FullName -Prefix "$Prefix$($_.Name)/"
        }
    }
    
    List-Strategies -BasePath $strategiesBaseDir
    Write-Host ""
    exit 1
}

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
    Write-Host "[1/4] Building Bot project..." -ForegroundColor Green
    Push-Location $scriptDir
    try {
        # Use incremental build with no-restore for faster compilation
        # --no-restore: Skip restoring packages (do manually if needed with 'dotnet restore')
        # -p:BuildInParallel=true: Build projects in parallel
        $buildOutput = dotnet build -c Release --nologo --verbosity quiet --no-restore -p:BuildInParallel=true 2>&1
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
    Write-Host "[1/4] Skipping build (using existing DLL)" -ForegroundColor Yellow
}

# Step 2: Create timestamped results folder and merge configuration
Write-Host "[2/4] Merging configuration and creating results folder..." -ForegroundColor Green

# Load global config
$globalConfig = Get-Content $globalConfigPath -Raw | ConvertFrom-Json

# Load strategy-specific config if it exists
$strategyConfig = $null
if (Test-Path $strategyConfigPath) {
    $strategyConfig = Get-Content $strategyConfigPath -Raw | ConvertFrom-Json
    Write-Host "      [OK] Found strategy-specific config" -ForegroundColor DarkGreen
}

# Merge configs: strategy config overrides global config
function Merge-Config {
    param($base, $override)
    
    if ($null -eq $override) {
        return $base
    }
    
    # Convert to hashtables for easier merging
    $baseHash = @{}
    $base.PSObject.Properties | ForEach-Object { $baseHash[$_.Name] = $_.Value }
    
    # Override properties from strategy config
    $override.PSObject.Properties | ForEach-Object {
        if ($_.Name -ne "_comment") {  # Skip comments
            $baseHash[$_.Name] = $_.Value
        }
    }
    
    # Convert back to PSCustomObject
    return [PSCustomObject]$baseHash
}

$mergedConfig = Merge-Config -base $globalConfig -override $strategyConfig

# Create timestamped folder name
# Use only the base strategy name (last part of path) for the folder name
# This ensures consistency with how LEAN names output files (using algorithm-type-name)
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$baseStrategyName = Split-Path $StrategyName -Leaf
$resultsFolderName = "$baseStrategyName-$timestamp"
$resultsPath = Join-Path (Join-Path $scriptDir "Results") $resultsFolderName

# Create the directory
$null = New-Item -ItemType Directory -Path $resultsPath -Force

# Set runtime-specific values
$mergedConfig.'results-destination-folder' = $resultsPath

# Write merged config to a temporary file
$tempConfigPath = Join-Path $scriptDir "config.temp.json"
$mergedConfig | ConvertTo-Json -Depth 10 | Set-Content $tempConfigPath

Write-Host "      [OK] Config merged and results folder created" -ForegroundColor DarkGreen

# Step 3: Run the backtest
Write-Host "[3/4] Running backtest..." -ForegroundColor Green
Write-Host ""
Write-Host "================================================" -ForegroundColor DarkGray

try {
    & $leanLauncherPath --config $tempConfigPath
    $exitCode = $LASTEXITCODE
}
finally {
    # Clean up temporary config file
    if (Test-Path $tempConfigPath) {
        Remove-Item $tempConfigPath -Force
    }
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

Write-Host "[4/4] Running visualization..." -ForegroundColor Green
if ($exitCode -eq 0) {
    & "$scriptDir\vis.ps1" -FolderName $resultsFolderName
}

exit $exitCode
