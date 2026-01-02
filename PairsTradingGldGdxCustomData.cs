/*
 * Pairs Trading Strategy: GLD vs GDX (Custom CSV Data)
 * 
 * Implements a mean-reverting pairs trading strategy between:
 * - GLD (SPDR Gold Shares ETF)
 * - GDX (VanEck Gold Miners ETF)
 * 
 * Strategy Overview:
 * 1. Calibration Phase (252 trading days): Calculate hedge ratio via OLS regression
 * 2. Trading Phase: Enter/exit positions based on z-score thresholds
 * 3. Entry: |z-score| >= 2, Exit: |z-score| <= 1
 * 
 * Data Source: Custom CSV files
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Algorithm;

namespace Demo
{
    /// <summary>
    /// Custom data class for loading CSV price data
    /// </summary>
    public class CustomCsvData : BaseData
    {
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }

        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            // Point to the CSV file location
            var source = $@"c:\Users\matur\source\sandbox\Demo\data\{config.Symbol.Value}.csv";
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile);
        }

        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            // Skip header row
            if (line.StartsWith("Date") || string.IsNullOrWhiteSpace(line))
                return null;

            try
            {
                var csv = line.Split(',');
                
                if (csv.Length < 7)
                    return null;
                
                // Parse CSV: Date,Open,High,Low,Close,Volume,Adj Close
                var dataTime = DateTime.ParseExact(csv[0].Trim(), "M/d/yyyy", CultureInfo.InvariantCulture);
                
                var data = new CustomCsvData
                {
                    Symbol = config.Symbol,
                    Time = dataTime,
                    Open = Convert.ToDecimal(csv[1].Trim(), CultureInfo.InvariantCulture),
                    High = Convert.ToDecimal(csv[2].Trim(), CultureInfo.InvariantCulture),
                    Low = Convert.ToDecimal(csv[3].Trim(), CultureInfo.InvariantCulture),
                    Close = Convert.ToDecimal(csv[4].Trim(), CultureInfo.InvariantCulture),
                    Volume = Convert.ToDecimal(csv[5].Trim(), CultureInfo.InvariantCulture),
                    Value = Convert.ToDecimal(csv[6].Trim(), CultureInfo.InvariantCulture) // Adj Close
                };
                
                data.EndTime = data.Time.AddDays(1);

                return data;
            }
            catch (Exception ex)
            {
                // Log parse errors for debugging
                return null;
            }
        }
    }

    /// <summary>
    /// Mean-reverting pairs trading strategy for GLD and GDX using custom CSV data
    /// </summary>
    public class PairsTradingGldGdxCustomData : QCAlgorithm
    {
        // Symbols
        private Symbol _gld;
        private Symbol _gdx;

        // Strategy parameters
        private const int TrainingPeriod = 252;
        private const decimal EntryZScore = 2.0m;
        private const decimal ExitZScore = 1.0m;

        // Calibration variables
        private bool _isCalibrated = false;
        private decimal _hedgeRatio;
        private decimal _spreadMean;
        private decimal _spreadStd;

        // Price history for calibration
        private List<decimal> _gldPrices = new List<decimal>();
        private List<decimal> _gdxPrices = new List<decimal>();

        // Position tracking
        private decimal _currentPosition = 0; // 1 = long spread, -1 = short spread, 0 = flat

        // Performance tracking
        private List<decimal> _trainingPnL = new List<decimal>();
        private List<decimal> _testPnL = new List<decimal>();
        private decimal _previousPortfolioValue;
        private int _tradingDaysCount = 0;

        // Store last prices
        private decimal _lastGldPrice = 0;
        private decimal _lastGdxPrice = 0;

        /// <summary>
        /// Initialize the algorithm
        /// </summary>
        public override void Initialize()
        {
            // Set backtest period to cover the CSV data range
            // GLD data: 11/30/2007 onwards (766 rows)
            // GDX data: 11/30/2007 onwards (387 rows)
            SetStartDate(2007, 11, 30);
            SetEndDate(2009, 5, 1); // Approximately matches the GDX data length
            
            // Set starting cash
            SetCash(100000);

            // Add custom data for GLD and GDX
            _gld = AddData<CustomCsvData>("GLD", Resolution.Daily).Symbol;
            _gdx = AddData<CustomCsvData>("GDX", Resolution.Daily).Symbol;

            // Set warmup period to ensure data is loaded
            SetWarmUp(TimeSpan.FromDays(5));

            _previousPortfolioValue = Portfolio.TotalPortfolioValue;

            Debug("Pairs Trading Algorithm Initialized (Custom CSV Data)");
            Debug($"Training Period: {TrainingPeriod} days");
            Debug($"Entry Z-Score Threshold: ±{EntryZScore}");
            Debug($"Exit Z-Score Threshold: ±{ExitZScore}");
        }

        /// <summary>
        /// OnData event - main trading logic
        /// </summary>
        public override void OnData(Slice data)
        {
            // Skip during warmup
            if (IsWarmingUp)
                return;

            // Ensure we have data for both symbols
            if (!data.ContainsKey(_gld) || !data.ContainsKey(_gdx))
                return;

            var gldData = data.Get<CustomCsvData>(_gld);
            var gdxData = data.Get<CustomCsvData>(_gdx);

            if (gldData == null || gdxData == null)
                return;

            decimal gldPrice = gldData.Close;
            decimal gdxPrice = gdxData.Close;

            // Store prices for position sizing
            _lastGldPrice = gldPrice;
            _lastGdxPrice = gdxPrice;

            _tradingDaysCount++;

            // Phase 1: Collect data for training period
            if (_tradingDaysCount <= TrainingPeriod)
            {
                _gldPrices.Add(gldPrice);
                _gdxPrices.Add(gdxPrice);

                // Calibrate at the end of training period
                if (_tradingDaysCount == TrainingPeriod)
                {
                    CalibrateStrategy();
                }
                
                // Calculate training P&L
                CalculateDailyPnL(true);
                return;
            }

            // Phase 2: Trading phase (after calibration)
            if (!_isCalibrated)
                return;

            // Calculate current spread and z-score
            decimal spread = gldPrice - _hedgeRatio * gdxPrice;
            decimal zScore = (spread - _spreadMean) / _spreadStd;

            // Log key metrics periodically
            if (_tradingDaysCount % 21 == 0) // Roughly monthly
            {
                Debug($"Date: {Time:yyyy-MM-dd} | GLD: ${gldPrice:F2} | GDX: ${gdxPrice:F2} | " +
                      $"Spread: {spread:F4} | Z-Score: {zScore:F4} | Position: {_currentPosition}");
            }

            // Trading logic
            ExecuteTradingLogic(gldPrice, gdxPrice, zScore);
            
            // Calculate test P&L
            CalculateDailyPnL(false);
        }

        /// <summary>
        /// Calibrate strategy using OLS regression
        /// </summary>
        private void CalibrateStrategy()
        {
            Debug($"\n{'=',-60}");
            Debug("CALIBRATION PHASE COMPLETE");
            Debug($"{'=',-60}");

            // Calculate hedge ratio using OLS regression: GLD = hedgeRatio × GDX
            (_hedgeRatio, decimal intercept) = CalculateOLS(_gdxPrices, _gldPrices);

            // Calculate spreads for training period
            List<decimal> spreads = new List<decimal>();
            for (int i = 0; i < TrainingPeriod; i++)
            {
                decimal spread = _gldPrices[i] - _hedgeRatio * _gdxPrices[i];
                spreads.Add(spread);
            }

            // Calculate spread statistics
            _spreadMean = spreads.Average();
            _spreadStd = CalculateStandardDeviation(spreads);

            _isCalibrated = true;

            Debug($"Hedge Ratio: {_hedgeRatio:F6}");
            Debug($"Regression Intercept: {intercept:F6}");
            Debug($"Spread Mean: {_spreadMean:F6}");
            Debug($"Spread Std Dev: {_spreadStd:F6}");
            
            if (_trainingPnL.Count > 0)
            {
                Debug($"Training Period Sharpe Ratio: {CalculateSharpeRatio(_trainingPnL):F4}");
            }
            
            Debug($"{'=',-60}\n");

            Plot("Calibration", "Hedge Ratio", (double)_hedgeRatio);
            Plot("Calibration", "Spread Mean", (double)_spreadMean);
            Plot("Calibration", "Spread Std", (double)_spreadStd);
        }

        /// <summary>
        /// Execute trading logic based on z-score
        /// </summary>
        private void ExecuteTradingLogic(decimal gldPrice, decimal gdxPrice, decimal zScore)
        {
            // ENTRY SIGNALS
            if (_currentPosition == 0)
            {
                if (zScore >= EntryZScore)
                {
                    // SHORT the spread: Short GLD, Long GDX
                    EnterShortSpread();
                    Debug($">>> ENTRY: SHORT SPREAD | Date: {Time:yyyy-MM-dd} | Z-Score: {zScore:F4}");
                }
                else if (zScore <= -EntryZScore)
                {
                    // LONG the spread: Long GLD, Short GDX
                    EnterLongSpread();
                    Debug($">>> ENTRY: LONG SPREAD | Date: {Time:yyyy-MM-dd} | Z-Score: {zScore:F4}");
                }
            }
            // EXIT SIGNALS
            else if (_currentPosition == -1 && zScore <= ExitZScore)
            {
                // Exit short spread position
                ExitPosition();
                Debug($"<<< EXIT: SHORT SPREAD | Date: {Time:yyyy-MM-dd} | Z-Score: {zScore:F4}");
            }
            else if (_currentPosition == 1 && zScore >= -ExitZScore)
            {
                // Exit long spread position
                ExitPosition();
                Debug($"<<< EXIT: LONG SPREAD | Date: {Time:yyyy-MM-dd} | Z-Score: {zScore:F4}");
            }

            // Plot z-score and position
            Plot("Z-Score", "Z-Score", (double)zScore);
            Plot("Z-Score", "Entry Threshold", (double)EntryZScore);
            Plot("Z-Score", "Exit Threshold", (double)ExitZScore);
            Plot("Position", "Current Position", (double)_currentPosition);
        }

        /// <summary>
        /// Enter long spread position: Long GLD, Short GDX
        /// </summary>
        private void EnterLongSpread()
        {
            // Allocate 50% of portfolio to each leg
            decimal gldTarget = 0.5m;
            decimal gdxTarget = -0.5m;

            SetHoldings(_gld, gldTarget);
            SetHoldings(_gdx, gdxTarget);

            _currentPosition = 1;
        }

        /// <summary>
        /// Enter short spread position: Short GLD, Long GDX
        /// </summary>
        private void EnterShortSpread()
        {
            // Allocate 50% of portfolio to each leg
            decimal gldTarget = -0.5m;
            decimal gdxTarget = 0.5m;

            SetHoldings(_gld, gldTarget);
            SetHoldings(_gdx, gdxTarget);

            _currentPosition = -1;
        }

        /// <summary>
        /// Exit all positions
        /// </summary>
        private void ExitPosition()
        {
            Liquidate();
            _currentPosition = 0;
        }

        /// <summary>
        /// Calculate daily P&L
        /// </summary>
        private void CalculateDailyPnL(bool isTraining)
        {
            decimal currentValue = Portfolio.TotalPortfolioValue;
            decimal dailyPnL = currentValue - _previousPortfolioValue;
            _previousPortfolioValue = currentValue;

            // Track P&L for training vs test period
            if (isTraining)
            {
                _trainingPnL.Add(dailyPnL);
            }
            else
            {
                _testPnL.Add(dailyPnL);
                
                // Plot cumulative P&L for test period
                if (_testPnL.Count > 0)
                {
                    decimal cumulativePnL = _testPnL.Sum();
                    Plot("Performance", "Cumulative P&L (Test)", (double)cumulativePnL);
                    Plot("Performance", "Portfolio Value", (double)currentValue);
                }
            }
        }

        /// <summary>
        /// Calculate OLS regression: y = beta * x + alpha
        /// Returns (beta, alpha)
        /// </summary>
        private (decimal, decimal) CalculateOLS(List<decimal> x, List<decimal> y)
        {
            int n = x.Count;
            decimal sumX = x.Sum();
            decimal sumY = y.Sum();
            decimal sumXY = x.Zip(y, (xi, yi) => xi * yi).Sum();
            decimal sumX2 = x.Sum(xi => xi * xi);

            decimal beta = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            decimal alpha = (sumY - beta * sumX) / n;

            return (beta, alpha);
        }

        /// <summary>
        /// Calculate standard deviation
        /// </summary>
        private decimal CalculateStandardDeviation(List<decimal> values)
        {
            if (values.Count == 0)
                return 0;
                
            decimal mean = values.Average();
            decimal sumSquaredDiff = values.Sum(v => (v - mean) * (v - mean));
            return (decimal)Math.Sqrt((double)(sumSquaredDiff / values.Count));
        }

        /// <summary>
        /// Calculate annualized Sharpe ratio
        /// </summary>
        private decimal CalculateSharpeRatio(List<decimal> pnlSeries)
        {
            if (pnlSeries.Count < 2)
                return 0;

            decimal meanPnL = pnlSeries.Average();
            decimal stdPnL = CalculateStandardDeviation(pnlSeries);

            if (stdPnL == 0)
                return 0;

            // Annualized Sharpe ratio: sqrt(252) * mean / std
            decimal sharpeRatio = (decimal)Math.Sqrt(252) * (meanPnL / stdPnL);
            return sharpeRatio;
        }

        /// <summary>
        /// End of algorithm - report final statistics
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            Debug($"\n{'=',-60}");
            Debug("BACKTEST COMPLETE - FINAL RESULTS");
            Debug($"{'=',-60}");

            Debug($"Initial Portfolio Value: $100,000");
            Debug($"Final Portfolio Value: {Portfolio.TotalPortfolioValue:C}");
            Debug($"Total Return: {(Portfolio.TotalPortfolioValue / 100000m - 1) * 100:F2}%");
            Debug($"");

            if (_trainingPnL.Count > 0)
            {
                decimal trainingSharpe = CalculateSharpeRatio(_trainingPnL);
                Debug($"Training Period Sharpe Ratio: {trainingSharpe:F4}");
                Debug($"Training Period Days: {_trainingPnL.Count}");
            }

            if (_testPnL.Count > 0)
            {
                decimal testSharpe = CalculateSharpeRatio(_testPnL);
                decimal cumulativeTestPnL = _testPnL.Sum();
                Debug($"Test Period Sharpe Ratio: {testSharpe:F4}");
                Debug($"Test Period Cumulative P&L: {cumulativeTestPnL:C}");
                Debug($"Test Period Trading Days: {_testPnL.Count}");
            }

            Debug($"");
            Debug($"Total Trading Days: {_tradingDaysCount}");
            Debug($"Training Days: {TrainingPeriod}");
            Debug($"Test Days: {_tradingDaysCount - TrainingPeriod}");
            
            if (_isCalibrated)
            {
                Debug($"");
                Debug($"Strategy Parameters:");
                Debug($"  Hedge Ratio: {_hedgeRatio:F6}");
                Debug($"  Spread Mean: {_spreadMean:F6}");
                Debug($"  Spread Std: {_spreadStd:F6}");
            }

            Debug($"{'=',-60}\n");
        }
    }
}
