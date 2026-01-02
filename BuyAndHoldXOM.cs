/*
 * Buy and Hold XOM Algorithm
 * 
 * This is a simple buy and hold algorithm that:
 * 1. Buys ExxonMobil (XOM) stock at the start
 * 2. Holds it throughout the backtest period
 * 3. Demonstrates the basic usage of QuantConnect's Lean engine
 */

using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;

namespace Demo
{
    /// <summary>
    /// Buy and hold algorithm for XOM (ExxonMobil) stock
    /// Modified to use SPY for demo purposes (sample data available)
    /// </summary>
    public class BuyAndHoldXOM : QCAlgorithm
    {
        private Symbol _symbol;

        /// <summary>
        /// Initialize the algorithm with date range, cash, and security selection
        /// </summary>
        public override void Initialize()
        {
            // Set the backtest date range from January 1, 2020 to January 1, 2026
            // Note: Using 2013 dates for demo with available SPY data
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            
            // Set starting cash to $100,000
            SetCash(100000);

            // Using SPY for demo (XOM data would need to be downloaded)
            // To use XOM: ensure XOM data is in ../Lean/Data/equity/usa/daily/xom.zip
            _symbol = AddEquity("SPY", Resolution.Daily).Symbol;

            // Log initialization
            Debug("Algorithm initialized: Buy and Hold Demo");
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
                // Invest 100% of the portfolio
                SetHoldings(_symbol, 1.0);
                Debug($"Purchased {_symbol} at {data[_symbol].Close:C} on {Time}");
            }
        }

        /// <summary>
        /// End of algorithm run - log final portfolio value
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            Debug($"Final Portfolio Value: {Portfolio.TotalPortfolioValue:C}");
            
            if (Portfolio[_symbol].Invested)
            {
                Debug($"{_symbol} Shares: {Portfolio[_symbol].Quantity}");
                Debug($"{_symbol} Market Value: {Portfolio[_symbol].HoldingsValue:C}");
            }
        }
    }
}
