# Quick Start Script for Buy and Hold Demo
# This script rebuilds the Demo algorithm and runs it with Lean

Write-Host "QuantConnect Lean - Buy and Hold Demo" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build the algorithm
Write-Host "[1/3] Building Demo algorithm..." -ForegroundColor Yellow
Set-Location "c:\Users\matur\source\sandbox\Demo"
$buildResult = dotnet build Demo.csproj -c Release 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}
Write-Host "✓ Build successful!" -ForegroundColor Green
Write-Host ""

# Step 2: Copy DLL to Lean Launcher
Write-Host "[2/3] Copying Demo.dll to Lean Launcher..." -ForegroundColor Yellow
Copy-Item "bin\Release\net10.0\Demo.dll" -Destination "..\Lean\Launcher\bin\Release\" -Force
Write-Host "✓ DLL copied!" -ForegroundColor Green
Write-Host ""

# Step 3: Run the algorithm
Write-Host "[3/3] Running the algorithm with Lean engine..." -ForegroundColor Yellow
Write-Host ""
Set-Location "..\Lean\Launcher\bin\Release"
.\QuantConnect.Lean.Launcher.exe --config "c:\Users\matur\source\sandbox\Demo\config.json"

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Done! Check the Results folder for detailed logs." -ForegroundColor Cyan
Write-Host "Log file: ./Results/BuyAndHoldXOM-log.txt" -ForegroundColor Cyan
