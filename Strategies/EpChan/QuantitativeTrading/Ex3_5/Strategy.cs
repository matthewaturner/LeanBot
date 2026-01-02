/*
 * Example 3.5 - Buy and Hold IGE + Short SPY Strategy
 * From "Quantitative Trading" by Ernest P. Chan
 * 
 * This strategy demonstrates:
 * 1. Loading custom data from CSV files (IGE and SPY)
 * 2. Buying and holding IGE from November 26, 2001 to November 14, 2007
 * 3. Shorting an equal amount of SPY on day 1
 * 4. Using custom data with the Lean engine
 */

using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Slippage;
using System;
using System.Globalization;
using Bot.Data;

namespace Bot.Strategies;

/// <summary>
/// Buy and hold strategy for IGE with SPY short hedge using custom data from epchan dataset.
/// </summary>
public class Ex3_5 : QCAlgorithm
{
    private Symbol _igeSymbol;
    private Symbol _spySymbol;

    /// <summary>
    /// Initialize the algorithm with date range, cash, and security selection
    /// </summary>
    public override void Initialize()
    {
        // Set the backtest date range from November 26, 2001 to November 14, 2007
        SetStartDate(2001, 11, 26);
        SetEndDate(2007, 11, 14);
        
        // Set starting cash to $100,000
        SetCash(100000);

        // Add custom data for IGE and SPY
        _igeSymbol = AddData<IGEData>("IGE").Symbol;
        _spySymbol = AddData<SPYData>("SPY").Symbol;
        
        // Log initialization
        Debug("Algorithm initialized: Example 3.5 - Buy and Hold IGE + Short SPY");
        Debug("Configuration: No slippage, No fees, Risk-free rate = 0.04");
    }

    /// <summary>
    /// OnData event is triggered whenever new data arrives
    /// This is where the main algorithm logic goes
    /// </summary>
    /// <param name="data">Slice object containing the stock data</param>
    public override void OnData(Slice data)
    {
        // If we don't already hold positions, enter both positions on day 1
        if (!Portfolio.Invested)
        {
            // Check if we have data for both symbols
            if (data.ContainsKey(_igeSymbol) && data.ContainsKey(_spySymbol))
            {
                // Invest 50% of the portfolio in IGE (long)
                SetHoldings(_igeSymbol, 0.5);
                Debug($"Purchased {_igeSymbol} at {data[_igeSymbol].Price:C} on {Time}");
                
                // Short 50% in SPY (equal dollar amount)
                SetHoldings(_spySymbol, -0.5);
                Debug($"Shorted {_spySymbol} at {data[_spySymbol].Price:C} on {Time}");
                
                Debug($"Cash: {Portfolio.Cash:C}, Total Portfolio Value: {Portfolio.TotalPortfolioValue:C}");
            }
        }
    }

    /// <summary>
    /// End of algorithm run - log final portfolio value
    /// </summary>
    public override void OnEndOfAlgorithm()
    {
        Debug($"Algorithm completed. Final portfolio value: {Portfolio.TotalPortfolioValue:C}");
    }
}
