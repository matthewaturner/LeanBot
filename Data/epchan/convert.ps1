param(
    [Parameter(Mandatory=$true)]
    [string]$FileName
)

# Get the full path of the input file
$inputPath = Join-Path $PSScriptRoot $FileName

# Check if file exists
if (-not (Test-Path $inputPath)) {
    Write-Error "File not found: $inputPath"
    exit 1
}

# Create output filename
$baseName = [System.IO.Path]::GetFileNameWithoutExtension($FileName)
$outputPath = Join-Path $PSScriptRoot "$baseName.lean.csv"

Write-Host "Converting $FileName to Lean format..."
Write-Host "Input: $inputPath"
Write-Host "Output: $outputPath"

# Read the CSV file
$csv = Import-Csv $inputPath

# Reverse the order (oldest first)
[array]::Reverse($csv)

# Process each row to reformat the date
$processedData = $csv | ForEach-Object {
    # Parse the date in M/D/YYYY format
    $date = [DateTime]::ParseExact($_.Date, 'M/d/yyyy', $null)
    
    # Format as YYYYMMDD HH:mm:ss (midnight time for daily data)
    $leanDate = $date.ToString('yyyyMMdd 00:00:00')
    
    # Create new object with reformatted date
    [PSCustomObject]@{
        Date = $leanDate
        Open = $_.Open
        High = $_.High
        Low = $_.Low
        Close = $_.Close
        Volume = $_.Volume
        'Adj Close' = $_.'Adj Close'
    }
}

# Export to CSV
$processedData | Export-Csv -Path $outputPath -NoTypeInformation

Write-Host "Conversion complete! Output saved to: $outputPath"
Write-Host "Total rows: $($processedData.Count)"
