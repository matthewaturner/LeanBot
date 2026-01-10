# QT_Ex3_6 - Pairs Trading GLD and GDX

## Strategy Overview
Pairs trading strategy from Example 3.6 of "Quantitative Trading" by Ernest P. Chan.

## Data Limitation
**Important**: The epchan dataset only contains data until 2007-11-30, providing approximately 6 months of actual trading data after the 252-day training period. This short period limits the statistical significance of performance metrics.

## Key Parameters
- **Training Period**: 252 days (used to calculate hedge ratio, spread mean, and spread std dev)
- **Entry Signals**:
  - Z-score >= 2: Short spread (short GLD, long GDX)
  - Z-score <= -2: Long spread (long GLD, short GDX)
- **Exit Signals**:
  - Z-score <= 1: Exit short spread
  - Z-score >= -1: Exit long spread

## Implementation Details
1. Uses Lean's `SetWarmUp(360 days)` to exclude training period from performance statistics
2. Sets neutral benchmark (`SetBenchmark(time => 0m)`) for standalone Sharpe calculation
3. Training period: 2006-05-23 to 2007-05-23 (252 trading days)
4. Trading period: 2007-05-23 to 2007-11-30 (~6 months)

## Method
1. Collect first 252 trading days during warm-up as training data
2. Calculate OLS regression: GLD = hedgeRatio * GDX (no intercept)
3. Calculate spread = GLD - hedgeRatio * GDX
4. Calculate mean and standard deviation of spread on training set
5. Calculate z-score = (spread - mean) / stddev
6. Trade based on z-score thresholds

## Position Sizing
- Uses 50% portfolio allocation to each leg (-0.5 for short, +0.5 for long)
- Maintains dollar-neutral pairs position

## Expected Results (with available data)
- Training completes: 2007-05-23
- Hedge Ratio: ~1.631
- Net Profit: ~5.4% over 6 months
- Win Rate: 67% (2 wins, 1 loss out of 3 complete trades)
- Max Drawdown: 3.5%

**Note**: The original Python implementation Sharpe ratio of ~1.4 likely used more extensive data through 2012.
