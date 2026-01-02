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

# Function to recursively find all Strategy.cs files
function Get-StrategyFolders {
    param($BasePath, $RelativePath = "")
    
    Get-ChildItem -Path $BasePath -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        $currentRelPath = if ($RelativePath) { "$RelativePath/$($_.Name)" } else { $_.Name }
        $strategyFile = Join-Path $_.FullName "Strategy.cs"
        
        if (Test-Path $strategyFile) {
            # This folder contains a strategy
            [PSCustomObject]@{
                Name = $currentRelPath
                FullPath = $_.FullName
                StrategyFile = $strategyFile
                ConfigFile = Join-Path $_.FullName "config.json"
            }
        }
        
        # Recursively search subdirectories
        Get-StrategyFolders -BasePath $_.FullName -RelativePath $currentRelPath
    }
}

$strategies = @(Get-StrategyFolders -BasePath $strategiesDir)

if ($strategies.Count -eq 0) {
    Write-Host "No strategies found." -ForegroundColor Yellow
} else {
    $strategies | Sort-Object -Property Name | ForEach-Object {
        $hasConfig = Test-Path $_.ConfigFile
        
        Write-Host "  - " -NoNewline -ForegroundColor Green
        Write-Host $_.Name -NoNewline -ForegroundColor White
        
        if ($hasConfig) {
            Write-Host " (custom config)" -NoNewline -ForegroundColor DarkCyan
        }
        
        Write-Host ""
    }
}

Write-Host ""