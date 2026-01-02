# Buy and Hold XOM Algorithm - Demo

This demo folder contains a simple buy and hold algorithm for ExxonMobil (XOM) stock.

## What's Included

1. **BuyAndHoldXOM.cs** - The main algorithm that:
   - Buys XOM stock at the start of the backtest
   - Holds it from 2020-01-01 to 2026-01-01
   - Reports the final portfolio value

2. **Demo.csproj** - Project file that references the Lean engine libraries

3. **config.json** - Configuration file for running the algorithm with Lean

## How to Get Market Data

The Lean engine requires historical market data to run backtests. You have several options:

### Option 1: Download Free Sample Data
QuantConnect provides some free sample data. Visit:
https://www.quantconnect.com/datasets

### Option 2: Use QuantConnect Cloud
The easiest way to run this is on QuantConnect's cloud platform at https://www.quantconnect.com

### Option 3: Get Data from Your Broker
If you have a brokerage account (Interactive Brokers, TD Ameritrade, etc.), you can download historical data using the Lean ToolBox.

### Option 4: Use Alternative Data Sources
You can download daily data from sources like:
- Yahoo Finance (using the Lean ToolBox)
- Alpha Vantage
- Quandl

## Running the Algorithm Locally

If you have XOM data available, here's how to run:

1. Ensure XOM daily data exists at: `../Lean/Data/equity/usa/daily/xom.zip`

2. From the Demo directory, run:
   ```powershell
   cd c:\Users\matur\source\sandbox\Lean\Launcher\bin\Release
   .\QuantConnect.Lean.Launcher.exe --config "c:\Users\matur\source\sandbox\Demo\config.json"
   ```

## Algorithm Explanation

The algorithm is extremely simple:
- **Initialize()**: Sets the date range, starting cash ($100,000), and adds XOM as a security
- **OnData()**: On the first data point, invests 100% of the portfolio in XOM
- **OnEndOfAlgorithm()**: Logs the final portfolio value and holdings

## Next Steps

1. Get historical XOM data (2020-2026)
2. Place it in the correct directory structure
3. Run the algorithm
4. Modify the algorithm to add:
   - Stop losses
   - Take profit targets
   - Multiple securities
   - Risk management
   - Trading signals

## Alternative: Run a Different Symbol

You can modify the algorithm to use SPY (which has sample data included) by changing line 31 in BuyAndHoldXOM.cs:

```csharp
// Change this:
_xom = AddEquity("XOM", Resolution.Daily).Symbol;

// To this:
_xom = AddEquity("SPY", Resolution.Daily).Symbol;
```

Then rebuild and run!

## Building the Algorithm

```powershell
cd c:\Users\matur\source\sandbox\Demo
dotnet build Demo.csproj -c Release
```

The DLL will be output to: `bin\Release\net10.0\Demo.dll`

## Resources

- QuantConnect Documentation: https://www.quantconnect.com/docs
- Lean GitHub: https://github.com/QuantConnect/Lean
- QuantConnect Community: https://www.quantconnect.com/forum
