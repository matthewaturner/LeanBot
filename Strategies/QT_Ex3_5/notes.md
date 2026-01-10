# Example 3.5 - Buy and Hold IGE + Short SPY

## Strategy Description
This strategy extends Example 3.4 by adding a short position in SPY to create a market-neutral hedge:
- Buys and holds IGE (iShares North American Natural Resources ETF)
- Shorts an equal dollar amount of SPY (S&P 500 ETF) on day 1
- Both positions are held from November 26, 2001 to November 14, 2007

## Key Features
- Long IGE position: 100% of portfolio
- Short SPY position: 100% of portfolio (equal dollar amount)
- This creates a market-neutral position where gains/losses are relative to the spread between IGE and SPY

## Data Source
- IGE data: Data/epchan/IGE.lean.csv
- SPY data: Data/epchan/SPY.lean.csv

## Configuration
- No slippage model
- No transaction fees
- Risk-free rate: 4%
- Date range: 2001-11-26 to 2007-11-14
- Starting capital: $100,000
