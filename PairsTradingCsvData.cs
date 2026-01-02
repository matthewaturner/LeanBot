/*
 * Pairs Trading Strategy: GLD vs GDX (CSV Data Version)
 * 
 * Loads historical data from CSV files and implements mean-reverting pairs trading
 * between GLD (SPDR Gold Shares ETF) and GDX (VanEck Gold Miners ETF).
 * 
 * This version loads all data upfront and processes it sequentially.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;

namespace Demo
{
    /// <summary>
    /// Price data point
    /// </summary>
    public class PricePoint
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public decimal AdjClose { get; set; }
    }

    /// <summary>
    /// Pairs trading strategy using CSV data loaded directly
    /// </summary>
    public class PairsTradingCsvData : QCAlgorithm
    {
        // Strategy parameters
        private const int TrainingPeriod = 252;
        private const decimal EntryZScore = 2.0m;
        private const decimal ExitZScore = 1.0m;

        // Historical data
        private List<PricePoint> _gldData = new List<PricePoint>();
        private List<PricePoint> _gdxData = new List<PricePoint>();
        private List<DateTime> _commonDates = new List<DateTime>();

        // Calibration variables
        private decimal _hedgeRatio;
        private decimal _spreadMean;
        private decimal _spreadStd;

        // Performance tracking
        private decimal _portfolioValue = 100000m;
        private decimal _currentPosition = 0; // 1 = long spread, -1 = short spread, 0 = flat
        private List<decimal> _trainingPnL = new List<decimal>();
        private List<decimal> _testPnL = new List<decimal>();
        private List<decimal> _cumulativePnL = new List<decimal>();

        /// <summary>
        /// Initialize and run the backtest
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2007, 11, 30);
            SetEndDate(2009, 5, 1);
            SetCash(100000);

            Debug("=== Pairs Trading Strategy: GLD vs GDX ===");
            Debug($"Training Period: {TrainingPeriod} days");
            Debug($"Entry Z-Score: ±{EntryZScore}");
            Debug($"Exit Z-Score: ±{ExitZScore}\n");

            // Load CSV data
            LoadCsvData();

            if (_commonDates.Count < TrainingPeriod + 10)
            {
                Debug($"ERROR: Insufficient data. Need at least {TrainingPeriod + 10} days, got {_commonDates.Count}");
                return;
            }

            Debug($"Loaded {_commonDates.Count} days of data");
            Debug($"Date range: {_commonDates.First():yyyy-MM-dd} to {_commonDates.Last():yyyy-MM-dd}\n");

            // Run the backtest
            RunBacktest();
        }

        /// <summary>
        /// Load and parse CSV files
        /// </summary>
        private void LoadCsvData()
        {
            string gldPath = @"c:\Users\matur\source\sandbox\Demo\data\GLD.csv";
            string gdxPath = @"c:\Users\matur\source\sandbox\Demo\data\GDX.csv";

            var gldDict = LoadCsvFile(gldPath);
            var gdxDict = LoadCsvFile(gdxPath);

            // Find common dates (intersection)
            var gldDates = gldDict.Keys.OrderBy(d => d).ToList();
            var gdxDates = gdxDict.Keys.OrderBy(d => d).ToList();
            _commonDates = gldDates.Intersect(gdxDates).OrderBy(d => d).ToList();

            // Build aligned price series
            foreach (var date in _commonDates)
            {
                _gldData.Add(gldDict[date]);
                _gdxData.Add(gdxDict[date]);
            }

            Debug($"GLD data points: {gldDict.Count}");
            Debug($"GDX data points: {gdxDict.Count}");
            Debug($"Common dates: {_commonDates.Count}");
        }

        /// <summary>
        /// Load a CSV file into a dictionary
        /// </summary>
        private Dictionary<DateTime, PricePoint> LoadCsvFile(string path)
        {
            var data = new Dictionary<DateTime, PricePoint>();
            var lines = File.ReadAllLines(path);

            for (int i = 1; i < lines.Length; i++) // Skip header
            {
                var parts = lines[i].Split(',');
                if (parts.Length < 7)
                    continue;

                try
                {
                    var point = new PricePoint
                    {
                        Date = DateTime.ParseExact(parts[0].Trim(), "M/d/yyyy", CultureInfo.InvariantCulture),
                        Open = decimal.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                        High = decimal.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                        Low = decimal.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                        Close = decimal.Parse(parts[4].Trim(), CultureInfo.InvariantCulture),
                        Volume = decimal.Parse(parts[5].Trim(), CultureInfo.InvariantCulture),
                        AdjClose = decimal.Parse(parts[6].Trim(), CultureInfo.InvariantCulture)
                    };

                    data[point.Date] = point;
                }
                catch
                {
                    // Skip malformed lines
                }
            }

            return data;
        }

        /// <summary>
        /// Run the backtest on loaded data
        /// </summary>
        private void RunBacktest()
        {
            Debug("=== CALIBRATION PHASE ===");

            // Phase 1: Calibration on training period
            var trainingGld = _gldData.Take(TrainingPeriod).Select(p => p.AdjClose).ToList();
            var trainingGdx = _gdxData.Take(TrainingPeriod).Select(p => p.AdjClose).ToList();

            (_hedgeRatio, decimal intercept) = CalculateOLS(trainingGdx, trainingGld);

            // Calculate spread statistics
            var trainingSpreads = new List<decimal>();
            for (int i = 0; i < TrainingPeriod; i++)
            {
                decimal spread = trainingGld[i] - _hedgeRatio * trainingGdx[i];
                trainingSpreads.Add(spread);
            }

            _spreadMean = trainingSpreads.Average();
            _spreadStd = CalculateStandardDeviation(trainingSpreads);

            Debug($"Hedge Ratio: {_hedgeRatio:F6}");
            Debug($"Intercept: {intercept:F6}");
            Debug($"Spread Mean: {_spreadMean:F6}");
            Debug($"Spread Std Dev: {_spreadStd:F6}\n");

            // Phase 2: Trading on test period
            Debug("=== TRADING PHASE ===\n");

            decimal previousValue = _portfolioValue;
            int tradesExecuted = 0;

            for (int i = 0; i < _commonDates.Count; i++)
            {
                var date = _commonDates[i];
                var gldPrice = _gldData[i].AdjClose;
                var gdxPrice = _gdxData[i].AdjClose;

                decimal dailyPnL = 0;

                if (i < TrainingPeriod)
                {
                    // Training period - just track P&L
                    dailyPnL = _portfolioValue - previousValue;
                    _trainingPnL.Add(dailyPnL);
                }
                else
                {
                    // Test period - execute trading strategy
                    decimal spread = gldPrice - _hedgeRatio * gdxPrice;
                    decimal zScore = (spread - _spreadMean) / _spreadStd;

                    // Calculate P&L from existing position
                    if (_currentPosition != 0 && i > 0)
                    {
                        decimal gldReturn = (_gldData[i].AdjClose - _gldData[i - 1].AdjClose) / _gldData[i - 1].AdjClose;
                        decimal gdxReturn = (_gdxData[i].AdjClose - _gdxData[i - 1].AdjClose) / _gdxData[i - 1].AdjClose;

                        if (_currentPosition == 1) // Long spread
                        {
                            dailyPnL = _portfolioValue * 0.5m * (gldReturn - gdxReturn);
                        }
                        else // Short spread
                        {
                            dailyPnL = _portfolioValue * 0.5m * (-gldReturn + gdxReturn);
                        }

                        _portfolioValue += dailyPnL;
                    }

                    // Trading logic
                    if (_currentPosition == 0)
                    {
                        if (zScore >= EntryZScore)
                        {
                            _currentPosition = -1; // Short spread
                            tradesExecuted++;
                            Debug($"{date:yyyy-MM-dd} | ENTER SHORT SPREAD | Z-Score: {zScore:F4} | Portfolio: ${_portfolioValue:F2}");
                        }
                        else if (zScore <= -EntryZScore)
                        {
                            _currentPosition = 1; // Long spread
                            tradesExecuted++;
                            Debug($"{date:yyyy-MM-dd} | ENTER LONG SPREAD | Z-Score: {zScore:F4} | Portfolio: ${_portfolioValue:F2}");
                        }
                    }
                    else if (_currentPosition == -1 && zScore <= ExitZScore)
                    {
                        _currentPosition = 0;
                        Debug($"{date:yyyy-MM-dd} | EXIT SHORT SPREAD | Z-Score: {zScore:F4} | Portfolio: ${_portfolioValue:F2}");
                    }
                    else if (_currentPosition == 1 && zScore >= -ExitZScore)
                    {
                        _currentPosition = 0;
                        Debug($"{date:yyyy-MM-dd} | EXIT LONG SPREAD | Z-Score: {zScore:F4} | Portfolio: ${_portfolioValue:F2}");
                    }

                    _testPnL.Add(dailyPnL);
                    _cumulativePnL.Add(_portfolioValue - 100000m);
                }

                previousValue = _portfolioValue;
            }

            // Final results
            Debug("\n=== FINAL RESULTS ===");
            Debug($"Initial Portfolio Value: $100,000");
            Debug($"Final Portfolio Value: ${_portfolioValue:F2}");
            Debug($"Total Return: {(_portfolioValue / 100000m - 1) * 100:F2}%");
            Debug($"Trades Executed: {tradesExecuted}\n");

            if (_trainingPnL.Count > 0)
            {
                decimal trainingSharpe = CalculateSharpeRatio(_trainingPnL);
                Debug($"Training Period Sharpe Ratio: {trainingSharpe:F4}");
                Debug($"Training Period Days: {_trainingPnL.Count}");
            }

            if (_testPnL.Count > 0)
            {
                decimal testSharpe = CalculateSharpeRatio(_testPnL);
                Debug($"Test Period Sharpe Ratio: {testSharpe:F4}");
                Debug($"Test Period Days: {_testPnL.Count}");
                Debug($"Test Period Total P&L: ${_testPnL.Sum():F2}");
            }

            Debug($"\n=== BACKTEST COMPLETE ===");
        }

        /// <summary>
        /// Calculate OLS regression
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

            return (decimal)Math.Sqrt(252) * (meanPnL / stdPnL);
        }

        public override void OnData(Slice data)
        {
            // Not used - all processing done in Initialize
        }
    }
}
