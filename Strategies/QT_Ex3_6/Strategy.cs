/*
 * Example 3.6 - Pairs Trading of GLD and GDX
 * From "Quantitative Trading" by Ernest P. Chan
 * 
 * This strategy demonstrates:
 * 1. Pairs trading using mean reversion
 * 2. OLS regression to determine hedge ratio
 * 3. Z-score based entry/exit signals
 * 4. Training period for parameter estimation
 */

using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Bot.Data;

namespace Bot.Strategies;

/// <summary>
/// Pairs trading strategy for GLD and GDX using OLS regression and z-score signals.
/// Uses first 252 days as training period to calculate hedge ratio, spread mean, and spread std dev.
/// </summary>
public class QT_Ex3_6 : QCAlgorithm
{
    private Symbol _gldSymbol;
    private Symbol _gdxSymbol;

    // Training period parameters
    private const int TrainingPeriod = 252;
    private List<decimal> _gldPrices = new List<decimal>();
    private List<decimal> _gdxPrices = new List<decimal>();
    private bool _isTrainingComplete = false;
    private int _dataPointCount = 0;

    // Calculated parameters from training period
    private double _hedgeRatio;
    private double _spreadMean;
    private double _spreadStdDev;

    // Current position tracking
    private bool _longSpreadPosition = false;  // Long GLD, Short GDX
    private bool _shortSpreadPosition = false; // Short GLD, Long GDX

    // Track daily PnL for Sharpe calculation (matching Python implementation exactly)
    private List<double> _dailyPnL = new List<double>();
    private decimal _previousGldPrice = 0;
    private decimal _previousGdxPrice = 0;
    private bool _hasPreviousPrices = false;

    /// <summary>
    /// Initialize the algorithm with date range, cash, and security selection
    /// </summary>
    public override void Initialize()
    {
        // Set the backtest date range - GDX starts on May 23, 2006
        // Note: epchan data ends on Nov 30, 2007
        SetStartDate(2006, 5, 23);
        SetEndDate(2007, 11, 30);
        
        // Set starting cash to $100,000
        SetCash(100000);

        // Set warm-up period to exclude training period from performance statistics
        // 252 trading days is approximately 360 calendar days
        SetWarmUp(TimeSpan.FromDays(360));

        // Set benchmark to zero (no benchmark) to calculate standalone Sharpe ratio
        SetBenchmark(time => 0m);

        // Add custom data for GLD and GDX
        _gldSymbol = AddData<GLDData>("GLD").Symbol;
        _gdxSymbol = AddData<GDXData>("GDX").Symbol;
        
        // Log initialization
        Debug("Algorithm initialized: Example 3.6 - Pairs Trading GLD and GDX");
        Debug($"Training period: {TrainingPeriod} days");
        Debug("Entry signals: zscore >= 2 (short spread), zscore <= -2 (long spread)");
        Debug("Exit signals: zscore <= 1 (exit short), zscore >= -1 (exit long)");
    }

    /// <summary>
    /// OnData event is triggered whenever new data arrives
    /// </summary>
    public override void OnData(Slice data)
    {
        // Make sure we have data for both symbols
        if (!data.ContainsKey(_gldSymbol) || !data.ContainsKey(_gdxSymbol))
            return;

        var gldData = data[_gldSymbol] as GLDData;
        var gdxData = data[_gdxSymbol] as GDXData;

        if (gldData == null || gdxData == null)
            return;

        // Collect training data during warm-up period
        if (!_isTrainingComplete)
        {
            _gldPrices.Add(gldData.AdjClose);
            _gdxPrices.Add(gdxData.AdjClose);
            _dataPointCount++;

            if (_dataPointCount >= TrainingPeriod)
            {
                // Calculate OLS regression: GLD = hedgeRatio * GDX
                _hedgeRatio = CalculateOLS(_gdxPrices, _gldPrices);

                // Calculate spread for training period
                var trainSpread = new List<double>();
                for (int i = 0; i < TrainingPeriod; i++)
                {
                    double spread = (double)_gldPrices[i] - _hedgeRatio * (double)_gdxPrices[i];
                    trainSpread.Add(spread);
                }

                // Calculate mean and standard deviation of spread
                _spreadMean = trainSpread.Average();
                _spreadStdDev = CalculateStdDev(trainSpread);

                _isTrainingComplete = true;

                Debug($"Training complete on {Time:yyyy-MM-dd}");
                Debug($"Hedge ratio: {_hedgeRatio:F6}");
                Debug($"Spread mean: {_spreadMean:F6}");
                Debug($"Spread std dev: {_spreadStdDev:F6}");
            }
            return;
        }

        // Don't trade during warm-up period
        if (IsWarmingUp)
            return;

        // Calculate daily PnL exactly like Python implementation
        // PnL = (positions from yesterday) * (returns today)
        if (_hasPreviousPrices)
        {
            // Calculate daily returns for each symbol
            double gldReturn = (double)((gldData.AdjClose - _previousGldPrice) / _previousGldPrice);
            double gdxReturn = (double)((gdxData.AdjClose - _previousGdxPrice) / _previousGdxPrice);
            
            // Get yesterday's positions (Python uses shift())
            double gldPosition = 0;
            double gdxPosition = 0;
            
            if (_longSpreadPosition)
            {
                gldPosition = 1.0;   // Long GLD
                gdxPosition = -1.0;  // Short GDX
            }
            else if (_shortSpreadPosition)
            {
                gldPosition = -1.0;  // Short GLD
                gdxPosition = 1.0;   // Long GDX
            }
            
            // Calculate PnL: sum of (position * return) for each symbol
            double dailyPnL = gldPosition * gldReturn + gdxPosition * gdxReturn;
            _dailyPnL.Add(dailyPnL);
        }
        
        // Store prices for next day's return calculation
        _previousGldPrice = gldData.AdjClose;
        _previousGdxPrice = gdxData.AdjClose;
        _hasPreviousPrices = true;

        // Trading logic - only execute after training is complete and warm-up is done
        double currentSpread = (double)gldData.AdjClose - _hedgeRatio * (double)gdxData.AdjClose;
        double zscore = (currentSpread - _spreadMean) / _spreadStdDev;

        // Entry logic: zscore >= 2 means spread is too high, short it (short GLD, long GDX)
        if (zscore >= 2.0 && !_shortSpreadPosition)
        {
            // Exit any long spread position first
            if (_longSpreadPosition)
            {
                Liquidate(_gldSymbol);
                Liquidate(_gdxSymbol);
                _longSpreadPosition = false;
            }

            // Enter short spread position
            // In pairs trading, we want equal dollar amounts: short GLD and long GDX
            // To maintain equal positions: short 0.5 of capital in GLD, long 0.5 in GDX
            SetHoldings(_gldSymbol, -0.5);
            SetHoldings(_gdxSymbol, 0.5);
            _shortSpreadPosition = true;
            Debug($"{Time:yyyy-MM-dd}: SHORT SPREAD - zscore={zscore:F3}, spread={currentSpread:F3}");
        }
        // Entry logic: zscore <= -2 means spread is too low, buy it (long GLD, short GDX)
        else if (zscore <= -2.0 && !_longSpreadPosition)
        {
            // Exit any short spread position first
            if (_shortSpreadPosition)
            {
                Liquidate(_gldSymbol);
                Liquidate(_gdxSymbol);
                _shortSpreadPosition = false;
            }

            // Enter long spread position
            // Long 0.5 of capital in GLD, short 0.5 in GDX
            SetHoldings(_gldSymbol, 0.5);
            SetHoldings(_gdxSymbol, -0.5);
            _longSpreadPosition = true;
            Debug($"{Time:yyyy-MM-dd}: LONG SPREAD - zscore={zscore:F3}, spread={currentSpread:F3}");
        }
        // Exit logic: zscore <= 1 means exit short spread
        else if (zscore <= 1.0 && _shortSpreadPosition)
        {
            Liquidate(_gldSymbol);
            Liquidate(_gdxSymbol);
            _shortSpreadPosition = false;
            Debug($"{Time:yyyy-MM-dd}: EXIT SHORT SPREAD - zscore={zscore:F3}, spread={currentSpread:F3}");
        }
        // Exit logic: zscore >= -1 means exit long spread
        else if (zscore >= -1.0 && _longSpreadPosition)
        {
            Liquidate(_gldSymbol);
            Liquidate(_gdxSymbol);
            _longSpreadPosition = false;
            Debug($"{Time:yyyy-MM-dd}: EXIT LONG SPREAD - zscore={zscore:F3}, spread={currentSpread:F3}");
        }
    }

    /// <summary>
    /// Calculate OLS regression coefficient: y = beta * x (no intercept)
    /// </summary>
    private double CalculateOLS(List<decimal> x, List<decimal> y)
    {
        if (x.Count != y.Count || x.Count == 0)
            throw new ArgumentException("X and Y must have the same non-zero length");

        double sumXY = 0;
        double sumXX = 0;

        for (int i = 0; i < x.Count; i++)
        {
            double xi = (double)x[i];
            double yi = (double)y[i];
            sumXY += xi * yi;
            sumXX += xi * xi;
        }

        return sumXY / sumXX;
    }

    /// <summary>
    /// Calculate standard deviation of a list of values
    /// </summary>
    private double CalculateStdDev(List<double> values)
    {
        if (values.Count == 0)
            return 0;
    
        double mean = values.Average();
        double sumSquaredDiff = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumSquaredDiff / values.Count);
    }

    /// <summary>
    /// End of algorithm run - log final portfolio value and calculate Sharpe
    /// </summary>
    public override void OnEndOfAlgorithm()
    {
        Debug($"Algorithm completed. Final portfolio value: {Portfolio.TotalPortfolioValue:C}");
        
        // Calculate Sharpe ratio using the EXACT same method as Python implementation
        // Python: sharpeTestset = np.sqrt(252) * np.mean(pnl[testset]) / np.std(pnl[testset])
        if (_dailyPnL.Count > 1)
        {
            double meanPnL = _dailyPnL.Average();
            double stdPnL = CalculateStdDev(_dailyPnL);
            double sharpeRatio = Math.Sqrt(252) * meanPnL / stdPnL;
            
            Debug($"");
            Debug($"=== Python-Style Sharpe Calculation ===");
            Debug($"Sharpe Ratio: {sharpeRatio:F3}");
            Debug($"Mean daily PnL: {meanPnL:F6}");
            Debug($"Std daily PnL: {stdPnL:F6}");
            Debug($"Number of trading days: {_dailyPnL.Count}");
            Debug($"Total PnL: {_dailyPnL.Sum():F6}");
        }
        else
        {
            Debug($"Not enough data for Sharpe calculation. PnL count: {_dailyPnL.Count}");
        }
    }
}
