# Running Backtests - Quick Guide

## Easy Method: Use the run-backtest Script

### Run a backtest:
```powershell
.\run-backtest.ps1 <AlgorithmName>
```

### Examples:
```powershell
# Run the pairs trading strategy
.\run-backtest.ps1 PairsTradingCsvData

# Run the buy-and-hold strategy
.\run-backtest.ps1 BuyAndHoldXOM

# Skip the build step (if code hasn't changed)
.\run-backtest.ps1 PairsTradingCsvData -NoBuild
```

### List available algorithms:
```powershell
.\list-algorithms.ps1
```

## What happens when you run the script:

1. **Builds the Demo project** (unless you use `-NoBuild`)
2. **Updates config.json** with the algorithm name
3. **Runs the backtest** using the LEAN engine

## No more manual steps needed!

✓ No copying DLLs  
✓ No editing config files  
✓ Just one simple command  

## Results Location

Backtest results are saved to:
```
.\Lean\Launcher\bin\Release\Results\
```

## Available Algorithms

- **PairsTradingCsvData** - GLD/GDX pairs trading strategy (CSV data)
- **BuyAndHoldXOM** - Simple buy-and-hold demo
- **PairsTradingGldGdx** - Pairs trading with standard data feed
- **PairsTradingGldGdxCustomData** - Pairs trading with custom data class

## Alternative: Using cmd

Windows users can also use:
```cmd
run-backtest.cmd PairsTradingCsvData
```
