/*
 * Example 3.4 - Buy and Hold Strategy for IGE
 * From "Quantitative Trading" by Ernest P. Chan
 * 
 * This strategy demonstrates:
 * 1. Loading custom data from a CSV file
 * 2. Buying and holding IGE from November 26, 2001 to November 14, 2007
 * 3. Using custom data with the Lean engine
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
/// Buy and hold strategy for IGE using custom data from epchan dataset.
/// </summary>
public class QT_Ex3_4 : QCAlgorithm
{
    private Symbol _igeSymbol;

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

        // Add custom data for IGE
        _igeSymbol = AddData<IGEData>("IGE").Symbol;
        
        // Log initialization
        Debug("Algorithm initialized: Example 3.4 - Buy and Hold IGE");
        Debug("Configuration: No slippage, No fees, Risk-free rate = 0.04");
    }

    /// <summary>
    /// OnData event is triggered whenever new data arrives
    /// This is where the main algorithm logic goes
    /// </summary>
    /// <param name="data">Slice object containing the stock data</param>
    public override void OnData(Slice data)
    {
        // If we don't already hold the stock, buy and hold
        if (!Portfolio.Invested)
        {
            // Check if we have data for our symbol
            if (data.ContainsKey(_igeSymbol))
            {
                // Invest 100% of the portfolio
                SetHoldings(_igeSymbol, 1.0);
                Debug($"Purchased {_igeSymbol} at {data[_igeSymbol].Price:C} on {Time}");
                Debug($"Cash: {Portfolio.Cash:C}, Holdings Value: {Portfolio[_igeSymbol].HoldingsValue:C}, Total: {Portfolio.TotalPortfolioValue:C}");
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
