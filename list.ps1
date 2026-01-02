#!/usr/bin/env pwsh
<#
.SYNOPSIS
    List all available algorithms in the Demo project.

.DESCRIPTION
    Scans the Demo project for algorithm classes that inherit from QCAlgorithm.
#>

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host ""
Write-Host "Available Strategies:" -ForegroundColor Cyan
Write-Host ""

$strategiesDir = Join-Path $scriptDir "Strategies"
$strategyFolders = Get-ChildItem -Path $strategiesDir -Directory -ErrorAction SilentlyContinue

if ($strategyFolders.Count -eq 0) {
    Write-Host "No strategies found." -ForegroundColor Yellow
} else {
    $strategyFolders | Sort-Object -Property Name | ForEach-Object {
        $strategyName = $_.Name
        $strategyFile = Join-Path $_.FullName "Strategy.cs"
        $configFile = Join-Path $_.FullName "config.json"
        
        # Check if Strategy.cs exists
        $hasStrategy = Test-Path $strategyFile
        $hasConfig = Test-Path $configFile
        
        Write-Host "  - " -NoNewline -ForegroundColor Green
        Write-Host $strategyName -NoNewline -ForegroundColor White
        
        if ($hasConfig) {
            Write-Host " (custom config)" -NoNewline -ForegroundColor DarkCyan
        }
        
        if (-not $hasStrategy) {
            Write-Host " [WARNING: Missing Strategy.cs]" -NoNewline -ForegroundColor Yellow
        }
        
        Write-Host ""
    }
}

Write-Host ""