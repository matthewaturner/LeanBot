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

namespace Bot.Strategies;

/// <summary>
/// Buy and hold strategy for IGE using custom data from epchan dataset.
/// </summary>
public class Ex3_4 : QCAlgorithm
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

        // Set risk-free rate to 0.04 (4% annual) for Sharpe ratio calculation
        SetRiskFreeInterestRateModel(new ConstantRiskFreeRateInterestRateModel(0.04m));

        // Add custom data for IGE
        _igeSymbol = AddData<IGEData>("IGE").Symbol;

        // Set zero slippage model (no slippage)
        Securities[_igeSymbol].SetSlippageModel(new ConstantSlippageModel(0));
        
        // Set zero fee model (no transaction fees)
        Securities[_igeSymbol].SetFeeModel(new ConstantFeeModel(0));

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
        var stats = Statistics;
        Debug($"Algorithm completed. Final portfolio value: {Portfolio.TotalPortfolioValue:C}");
        Debug($"Total Return: {stats.TotalPerformance.PortfolioStatistics.TotalReturn:P2}");
        Debug($"Annual Return: {stats.TotalPerformance.PortfolioStatistics.CompoundingAnnualReturn:P2}");
        Debug($"Annual StdDev: {stats.TotalPerformance.PortfolioStatistics.AnnualStandardDeviation:P2}");
        Debug($"Sharpe Ratio: {stats.TotalPerformance.PortfolioStatistics.SharpeRatio:F3}");
        Debug($"Risk-Free Rate Used: 0.04");
    }
}

/// <summary>
/// Custom data type for IGE data from epchan dataset
/// </summary>
public class IGEData : BaseData
{
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public decimal AdjClose { get; set; }

    /// <summary>
    /// Return the URL string source of the file. This will be converted to a stream
    /// </summary>
    public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
    {
        // Point to the local CSV file
        var source = "Data/epchan/IGE.lean.csv";
        return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile);
    }

    /// <summary>
    /// Reader converts each line of the data source into BaseData objects
    /// </summary>
    public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
    {
        // Skip header line or empty lines
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Date"))
        {
            return null;
        }

        try
        {
            // Parse CSV line
            // Format: "Date","Open","High","Low","Close","Volume","Adj Close"
            // Example: "20011126 00:00:00","91.01","91.01","91.01","91.01","0","42.09"
            var csv = line.Split(',');
            
            // Remove quotes from date string and parse
            var dateString = csv[0].Trim('"');
            var parsedDate = DateTime.ParseExact(dateString, "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);

            var data = new IGEData
            {
                Symbol = config.Symbol,
                Time = parsedDate,
                Open = decimal.Parse(csv[1].Trim('"')),
                High = decimal.Parse(csv[2].Trim('"')),
                Low = decimal.Parse(csv[3].Trim('"')),
                Close = decimal.Parse(csv[4].Trim('"')),
                Volume = decimal.Parse(csv[5].Trim('"')),
                AdjClose = decimal.Parse(csv[6].Trim('"'))
            };

            // Use adjusted close as the value
            data.Value = data.AdjClose;

            return data;
        }
        catch (Exception ex)
        {
            // Return null if parsing fails
            return null;
        }
    }
}
