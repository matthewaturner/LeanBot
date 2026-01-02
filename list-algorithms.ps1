#!/usr/bin/env pwsh
<#
.SYNOPSIS
    List all available algorithms in the Demo project.

.DESCRIPTION
    Scans the Demo project for algorithm classes that inherit from QCAlgorithm.
#>

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host ""
Write-Host "Available Algorithms:" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host ""

$strategiesDir = Join-Path $scriptDir "Strategies"
$csFiles = Get-ChildItem -Path $strategiesDir -Filter "*.cs" -File -ErrorAction SilentlyContinue

$algorithms = @()

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw
    
    # Look for class definitions that inherit from QCAlgorithm
    if ($content -match 'class\s+(\w+)\s*:\s*QCAlgorithm') {
        $className = $matches[1]
        $algorithms += @{
            Name = $className
            File = $file.Name
        }
    }
}

if ($algorithms.Count -eq 0) {
    Write-Host "No algorithms found." -ForegroundColor Yellow
} else {
    $algorithms | Sort-Object -Property Name | ForEach-Object {
        Write-Host "  • " -NoNewline -ForegroundColor Green
        Write-Host $_.Name -NoNewline -ForegroundColor White
        Write-Host " ($($_.File))" -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "Usage:" -ForegroundColor Cyan
Write-Host "  .\run-backtest.ps1 <AlgorithmName>" -ForegroundColor White
Write-Host ""
Write-Host "Example:" -ForegroundColor Cyan
Write-Host "  .\run-backtest.ps1 $($algorithms[0].Name)" -ForegroundColor White
Write-Host ""
